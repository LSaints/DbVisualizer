using System.Data;
using DatabaseDiagram.Api.Infraestrutura;
using DatabaseDiagram.Api.Modelos;
using MySqlConnector;

namespace DatabaseDiagram.Api.Provedores.MySql;

/// <summary>
/// Provedor de introspecção do MySQL via <c>information_schema</c>.
/// Executa somente consultas de leitura predefinidas (constituição III),
/// página a página/tabela a tabela para não sobrecarregar bancos grandes.
/// Valores técnicos dos metadados (ex.: <c>InnoDB</c>, <c>BASE TABLE</c>)
/// são preservados conforme vêm do banco.
/// </summary>
public sealed class MySqlProvedorDeSchema : InterfaceProvedorDeSchema
{
    /// <summary>Número máximo de tabelas introspeccionadas por requisição.</summary>
    internal const int LimiteDeTabelasParaIntrospeccao = 500;

    /// <summary>
    /// Uma linha a mais que o limite: detecta truncamento sem consulta extra.
    /// Mantenha em sincronia com o <c>LIMIT</c> da <see cref="ConsultaDeTabelas"/>.
    /// </summary>
    internal const int LimiteDeConsultaDeTabelas = LimiteDeTabelasParaIntrospeccao + 1;

    /// <summary>Timeout de cada comando para não segurar conexão/thread indefinidamente.</summary>
    private const int TimeoutDoComandoEmSegundos = 30;

    private readonly ConfiguradorDeConexao _configurador;

    public MySqlProvedorDeSchema(ConfiguradorDeConexao configurador) => _configurador = configurador;

    public string Tipo => TipoDeProvedor.MySql.ToString().ToLowerInvariant();

    internal const string ConsultaDoDatabase = """
        SELECT DEFAULT_CHARACTER_SET_NAME, DEFAULT_COLLATION_NAME
        FROM information_schema.SCHEMATA
        WHERE SCHEMA_NAME = @bancoDeDados
        """;

    internal const string ConsultaDeTabelas = """
        SELECT TABLE_NAME, TABLE_TYPE, ENGINE, TABLE_COMMENT
        FROM information_schema.TABLES
        WHERE TABLE_SCHEMA = @bancoDeDados
          AND TABLE_TYPE = 'BASE TABLE'
        ORDER BY TABLE_NAME
        LIMIT 501
        """;

    internal const string ConsultaDeColunas = """
        SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE, COLUMN_TYPE, COLUMN_DEFAULT,
               IS_NULLABLE, ORDINAL_POSITION, EXTRA, COLUMN_COMMENT
        FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = @bancoDeDados
          AND TABLE_NAME = @tabela
        ORDER BY ORDINAL_POSITION
        """;

    internal const string ConsultaDeChavesPrimarias = """
        SELECT kcu.TABLE_NAME, kcu.COLUMN_NAME
        FROM information_schema.TABLE_CONSTRAINTS tc
        JOIN information_schema.KEY_COLUMN_USAGE kcu
          ON kcu.CONSTRAINT_SCHEMA = tc.CONSTRAINT_SCHEMA
         AND kcu.CONSTRAINT_NAME = tc.CONSTRAINT_NAME
         AND kcu.TABLE_NAME = tc.TABLE_NAME
        WHERE tc.TABLE_SCHEMA = @bancoDeDados
          AND tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
          AND tc.TABLE_NAME = @tabela
        ORDER BY kcu.ORDINAL_POSITION
        """;

    internal const string ConsultaDeChavesEstrangeiras = """
        SELECT kcu.CONSTRAINT_NAME,
               kcu.TABLE_NAME,
               kcu.COLUMN_NAME,
               kcu.REFERENCED_TABLE_SCHEMA,
               kcu.REFERENCED_TABLE_NAME,
               kcu.REFERENCED_COLUMN_NAME,
               rc.UPDATE_RULE,
               rc.DELETE_RULE
        FROM information_schema.KEY_COLUMN_USAGE kcu
        JOIN information_schema.REFERENTIAL_CONSTRAINTS rc
          ON rc.CONSTRAINT_SCHEMA = kcu.CONSTRAINT_SCHEMA
         AND rc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME
         AND rc.TABLE_NAME = kcu.TABLE_NAME
        WHERE kcu.REFERENCED_TABLE_NAME IS NOT NULL
          AND kcu.TABLE_SCHEMA = @bancoDeDados
          AND kcu.TABLE_NAME = @tabela
        ORDER BY kcu.ORDINAL_POSITION
        """;

    public async Task<EsquemaDeBanco> ObterSchemaAsync(
        ConexaoDeBanco conexao,
        CancellationToken cancellationToken)
    {
        var connectionString = _configurador.CriarConnectionString(conexao);
        await using var conexaoSql = new MySqlConnection(connectionString);
        await conexaoSql.OpenAsync(cancellationToken);

        var nomeDoBanco = conexao.BancoDeDados;

        var (charset, collation) = await ObterDatabaseAsync(conexaoSql, nomeDoBanco, cancellationToken);

        var tabelas = await ObterTabelasAsync(conexaoSql, nomeDoBanco, cancellationToken);

        // Consultas por tabela (filtro TABLE_SCHEMA + TABLE_NAME no
        // information_schema) limitam a carga e a contenção do dicionário.
        var introspeccaoTruncada = tabelas.Count >= LimiteDeConsultaDeTabelas;
        if (introspeccaoTruncada)
        {
            tabelas = tabelas.Take(LimiteDeTabelasParaIntrospeccao).ToList();
        }

        var colunasPorTabela = new Dictionary<string, IReadOnlyList<ColunaDeBanco>>();
        var chavesPrimarias = new List<(string Esquema, string Tabela, string Coluna)>();
        var relacionamentos = new List<RelacionamentoDeBanco>();

        foreach (var tabela in tabelas)
        {
            cancellationToken.ThrowIfCancellationRequested();

            colunasPorTabela[tabela.Nome] = await ObterColunasDeTabelaAsync(
                conexaoSql, nomeDoBanco, tabela.Nome, cancellationToken);

            chavesPrimarias.AddRange(await ObterChavesPrimariasDeTabelaAsync(
                conexaoSql, nomeDoBanco, tabela.Nome, cancellationToken));

            relacionamentos.AddRange(await ObterRelacionamentosDeTabelaAsync(
                conexaoSql, nomeDoBanco, tabela.Nome, cancellationToken));
        }

        var esquema = MontarEsquema(
            provedor: Tipo,
            nomeDoBanco,
            charset,
            collation,
            tabelas,
            colunasPorTabela,
            chavesPrimarias,
            relacionamentos);

        if (introspeccaoTruncada)
        {
            esquema.Aviso = CriarAvisoDeTruncamento();
        }

        return esquema;
    }

    private static async Task<(string? Charset, string? Collation)> ObterDatabaseAsync(
        MySqlConnection conexaoSql,
        string bancoDeDados,
        CancellationToken cancellationToken)
    {
        await using var comando = CriarComando(conexaoSql, ConsultaDoDatabase, bancoDeDados);
        await using var leitor = await comando.ExecuteReaderAsync(cancellationToken);

        if (!await leitor.ReadAsync(cancellationToken))
        {
            return (null, null);
        }

        return (ObterStringOpcional(leitor, 0), ObterStringOpcional(leitor, 1));
    }

    private static async Task<List<TabelaDeBanco>> ObterTabelasAsync(
        MySqlConnection conexaoSql,
        string bancoDeDados,
        CancellationToken cancellationToken)
    {
        var tabelas = new List<TabelaDeBanco>();

        await using var comando = CriarComando(conexaoSql, ConsultaDeTabelas, bancoDeDados);
        await using var leitor = await comando.ExecuteReaderAsync(cancellationToken);

        while (await leitor.ReadAsync(cancellationToken))
        {
            tabelas.Add(new TabelaDeBanco
            {
                Esquema = bancoDeDados,
                Nome = leitor.GetString(0),
                TipoDaTabela = ObterStringOpcional(leitor, 1),
                Motor = ObterStringOpcional(leitor, 2),
                Comentario = ObterStringOpcional(leitor, 3)
            });
        }

        return tabelas;
    }

    private static async Task<List<ColunaDeBanco>> ObterColunasDeTabelaAsync(
        MySqlConnection conexaoSql,
        string bancoDeDados,
        string nomeDaTabela,
        CancellationToken cancellationToken)
    {
        var colunas = new List<ColunaDeBanco>();

        await using var comando = CriarComando(
            conexaoSql, ConsultaDeColunas, bancoDeDados, nomeDaTabela);
        await using var leitor = await comando.ExecuteReaderAsync(cancellationToken);

        while (await leitor.ReadAsync(cancellationToken))
        {
            colunas.Add(new ColunaDeBanco
            {
                Nome = leitor.GetString(1),
                TipoDoDado = leitor.GetString(2),
                TipoCompletoDoDado = ObterStringOpcional(leitor, 3),
                ValorPadrao = ObterStringOpcional(leitor, 4),
                PodeSerNula = string.Equals(leitor.GetString(5), "YES", StringComparison.OrdinalIgnoreCase),
                PosicaoOrdinal = leitor.GetInt32(6),
                AutoIncremento = leitor.GetString(7).Contains("auto_increment", StringComparison.OrdinalIgnoreCase),
                Comentario = ObterStringOpcional(leitor, 8)
            });
        }

        colunas.Sort((a, b) => a.PosicaoOrdinal.CompareTo(b.PosicaoOrdinal));
        return colunas;
    }

    private static async Task<List<(string Esquema, string Tabela, string Coluna)>> ObterChavesPrimariasDeTabelaAsync(
        MySqlConnection conexaoSql,
        string bancoDeDados,
        string nomeDaTabela,
        CancellationToken cancellationToken)
    {
        var chaves = new List<(string, string, string)>();

        await using var comando = CriarComando(
            conexaoSql, ConsultaDeChavesPrimarias, bancoDeDados, nomeDaTabela);
        await using var leitor = await comando.ExecuteReaderAsync(cancellationToken);

        while (await leitor.ReadAsync(cancellationToken))
        {
            chaves.Add((bancoDeDados, nomeDaTabela, leitor.GetString(1)));
        }

        return chaves;
    }

    private static async Task<List<RelacionamentoDeBanco>> ObterRelacionamentosDeTabelaAsync(
        MySqlConnection conexaoSql,
        string bancoDeDados,
        string nomeDaTabela,
        CancellationToken cancellationToken)
    {
        var relacionamentos = new List<RelacionamentoDeBanco>();

        await using var comando = CriarComando(
            conexaoSql, ConsultaDeChavesEstrangeiras, bancoDeDados, nomeDaTabela);
        await using var leitor = await comando.ExecuteReaderAsync(cancellationToken);

        while (await leitor.ReadAsync(cancellationToken))
        {
            relacionamentos.Add(new RelacionamentoDeBanco
            {
                NomeDaRestricao = ObterStringOpcional(leitor, 0),
                EsquemaOrigem = bancoDeDados,
                TabelaOrigem = leitor.GetString(1),
                ColunaOrigem = leitor.GetString(2),
                EsquemaDestino = ObterStringOpcional(leitor, 3) ?? bancoDeDados,
                TabelaDestino = leitor.GetString(4),
                ColunaDestino = leitor.GetString(5),
                RegraDeAtualizacao = ObterStringOpcional(leitor, 6),
                RegraDeExclusao = ObterStringOpcional(leitor, 7)
            });
        }

        return relacionamentos;
    }

    private static MySqlCommand CriarComando(
        MySqlConnection conexaoSql,
        string consultaSql,
        string bancoDeDados,
        string? nomeDaTabela = null)
    {
        var comando = new MySqlCommand(consultaSql, conexaoSql)
        {
            CommandType = CommandType.Text,
            CommandTimeout = TimeoutDoComandoEmSegundos
        };
        comando.Parameters.AddWithValue("@bancoDeDados", bancoDeDados);
        if (nomeDaTabela is not null)
        {
            comando.Parameters.AddWithValue("@tabela", nomeDaTabela);
        }
        return comando;
    }

    /// <summary>Aviso quando o banco excede o limite de tabelas introspeccionadas.</summary>
    internal static string CriarAvisoDeTruncamento() =>
        $"O banco possui mais de {LimiteDeTabelasParaIntrospeccao} tabelas; "
        + $"o diagrama mostra apenas as {LimiteDeTabelasParaIntrospeccao} primeiras, em ordem alfabética.";

    /// <summary>Lê uma coluna string opcional (nula quando o banco não informou).</summary>
    private static string? ObterStringOpcional(MySqlDataReader leitor, int ordinal) =>
        leitor.IsDBNull(ordinal) ? null : leitor.GetString(ordinal);

    /// <summary>
    /// Monta o <see cref="EsquemaDeBanco"/> comum a partir dos metadados
    /// coletados. Pureza isolada para permitir teste unitário da montagem
    /// (declara flags de PK/FK, ordena e anexa relacionamentos).
    /// </summary>
    internal static EsquemaDeBanco MontarEsquema(
        string provedor,
        string nomeDoBanco,
        string? charset,
        string? collation,
        IReadOnlyCollection<TabelaDeBanco> tabelas,
        IReadOnlyDictionary<string, IReadOnlyList<ColunaDeBanco>> colunasPorTabela,
        IReadOnlyCollection<(string Esquema, string Tabela, string Coluna)> chavesPrimarias,
        IReadOnlyCollection<RelacionamentoDeBanco> relacionamentos)
    {
        var colunasPk = chavesPrimarias
            .Select(c => ChaveDaColuna(c.Esquema, c.Tabela, c.Coluna))
            .ToHashSet(StringComparer.Ordinal);

        var colunasOrigemDeFk = relacionamentos
            .Select(r => ChaveDaColuna(r.EsquemaOrigem, r.TabelaOrigem, r.ColunaOrigem))
            .ToHashSet(StringComparer.Ordinal);

        foreach (var tabela in tabelas)
        {
            if (!colunasPorTabela.TryGetValue(tabela.Nome, out var colunas))
            {
                continue;
            }

            foreach (var coluna in colunas)
            {
                if (colunasPk.Contains(ChaveDaColuna(tabela.Esquema, tabela.Nome, coluna.Nome)))
                {
                    coluna.ChavePrimaria = true;
                }

                if (colunasOrigemDeFk.Contains(ChaveDaColuna(tabela.Esquema, tabela.Nome, coluna.Nome)))
                {
                    coluna.ChaveEstrangeira = true;
                }
            }

            tabela.Colunas.AddRange(colunas.OrderBy(c => c.PosicaoOrdinal));
        }

        return new EsquemaDeBanco
        {
            Provedor = provedor,
            NomeDoBanco = nomeDoBanco,
            Charset = charset,
            Collation = collation,
            Tabelas = [.. tabelas],
            Relacionamentos = [.. relacionamentos]
        };
    }

    internal static string ChaveDaTabela(string esquema, string nome) => $"{esquema}.{nome}";

    internal static string ChaveDaColuna(string esquema, string tabela, string coluna) =>
        $"{ChaveDaTabela(esquema, tabela)}.{coluna}";
}