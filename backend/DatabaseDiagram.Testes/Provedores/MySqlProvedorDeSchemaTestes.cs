using System.Text.RegularExpressions;
using DatabaseDiagram.Api.Modelos;
using DatabaseDiagram.Api.Provedores.MySql;

namespace DatabaseDiagram.Testes.Provedores;

public class MySqlProvedorDeSchemaTestes
{
    [Fact]
    public void MontarEsquema_ComMetadados_ConstroiSchemaComFlagsDeChaves()
    {
        var clientes = new TabelaDeBanco { Esquema = "erp", Nome = "clientes" };
        var pedidos = new TabelaDeBanco { Esquema = "erp", Nome = "pedidos" };

        var colunasPorTabela = new Dictionary<string, IReadOnlyList<ColunaDeBanco>>
        {
            ["clientes"] =
            [
                new ColunaDeBanco { Nome = "id", TipoDoDado = "int", PosicaoOrdinal = 1, AutoIncremento = true }
            ],
            ["pedidos"] =
            [
                new ColunaDeBanco { Nome = "id", TipoDoDado = "int", PosicaoOrdinal = 1 },
                new ColunaDeBanco { Nome = "cliente_id", TipoDoDado = "int", PosicaoOrdinal = 2 }
            ]
        };

        var chavesPrimarias = new List<(string Esquema, string Tabela, string Coluna)>
        {
            ("erp", "clientes", "id"),
            ("erp", "pedidos", "id")
        };

        var relacionamentos = new List<RelacionamentoDeBanco>
        {
            new()
            {
                EsquemaOrigem = "erp",
                TabelaOrigem = "pedidos",
                ColunaOrigem = "cliente_id",
                EsquemaDestino = "erp",
                TabelaDestino = "clientes",
                ColunaDestino = "id",
                NomeDaRestricao = "fk_pedidos_clientes",
                RegraDeAtualizacao = "CASCADE",
                RegraDeExclusao = "RESTRICT"
            }
        };

        var esquema = MySqlProvedorDeSchema.MontarEsquema(
            provedor: "mysql",
            nomeDoBanco: "erp",
            charset: "utf8mb4",
            collation: "utf8mb4_general_ci",
            tabelas: [clientes, pedidos],
            colunasPorTabela,
            chavesPrimarias,
            relacionamentos);

        Assert.Equal("mysql", esquema.Provedor);
        Assert.Equal("erp", esquema.NomeDoBanco);
        Assert.Equal("utf8mb4", esquema.Charset);
        Assert.Equal("utf8mb4_general_ci", esquema.Collation);
        Assert.Equal(2, esquema.Tabelas.Count);

        var tabelaPedidos = esquema.Tabelas.Single(t => t.Nome == "pedidos");
        Assert.Equal(2, tabelaPedidos.Colunas.Count);
        Assert.True(tabelaPedidos.Colunas.Single(c => c.Nome == "id").ChavePrimaria);
        Assert.True(tabelaPedidos.Colunas.Single(c => c.Nome == "cliente_id").ChaveEstrangeira);
        Assert.False(tabelaPedidos.Colunas.Single(c => c.Nome == "cliente_id").ChavePrimaria);
        Assert.True(tabelaPedidos.Colunas.Single(c => c.Nome == "cliente_id").PodeSerNula == false);

        var tabelaClientes = esquema.Tabelas.Single(t => t.Nome == "clientes");
        Assert.True(tabelaClientes.Colunas.Single().ChavePrimaria);
        Assert.True(tabelaClientes.Colunas.Single().AutoIncremento);

        var relacionamento = Assert.Single(esquema.Relacionamentos);
        Assert.Equal("cliente_id", relacionamento.ColunaOrigem);
        Assert.Equal("CASCADE", relacionamento.RegraDeAtualizacao);
        Assert.Equal("RESTRICT", relacionamento.RegraDeExclusao);
    }

    [Fact]
    public void MontarEsquema_SemMetadados_RetornaSchemaVazio()
    {
        var esquema = MySqlProvedorDeSchema.MontarEsquema(
            provedor: "mysql",
            nomeDoBanco: "erp",
            charset: null,
            collation: null,
            tabelas: [],
            new Dictionary<string, IReadOnlyList<ColunaDeBanco>>(),
            [],
            []);

        Assert.Equal("erp", esquema.NomeDoBanco);
        Assert.Null(esquema.Charset);
        Assert.Null(esquema.Collation);
        Assert.Empty(esquema.Tabelas);
        Assert.Empty(esquema.Relacionamentos);
    }

    [Fact]
    public void MontarEsquema_OrdenaColunasPorPosicaoOrdinal()
    {
        var tabela = new TabelaDeBanco { Esquema = "erp", Nome = "itens" };
        var colunas = new Dictionary<string, IReadOnlyList<ColunaDeBanco>>
        {
            ["itens"] =
            [
                new ColunaDeBanco { Nome = "b", TipoDoDado = "varchar", PosicaoOrdinal = 2 },
                new ColunaDeBanco { Nome = "a", TipoDoDado = "int", PosicaoOrdinal = 1 }
            ]
        };

        var esquema = MySqlProvedorDeSchema.MontarEsquema("mysql", "erp", null, null, [tabela], colunas, [], []);

        Assert.Equal(["a", "b"], esquema.Tabelas.Single().Colunas.Select(c => c.Nome));
    }

    [Theory]
    [InlineData(MySqlProvedorDeSchema.ConsultaDoDatabase)]
    [InlineData(MySqlProvedorDeSchema.ConsultaDeTabelas)]
    [InlineData(MySqlProvedorDeSchema.ConsultaDeColunas)]
    [InlineData(MySqlProvedorDeSchema.ConsultaDeChavesPrimarias)]
    [InlineData(MySqlProvedorDeSchema.ConsultaDeChavesEstrangeiras)]
    public void Consultas_SaoDeLeituraDeMetadados(string consulta)
    {
        Assert.Matches(@"^\s*SELECT\b", consulta);
        Assert.Contains("INFORMATION_SCHEMA", consulta.ToUpperInvariant());
        Assert.DoesNotMatch(
            new Regex(@"\b(INSERT|UPDATE|DELETE|DROP|CREATE|ALTER|TRUNCATE)\b", RegexOptions.IgnoreCase),
            consulta);
    }

    [Fact]
    public void ConsultaDeTabelas_FiltraApenasBaseTable()
    {
        Assert.Contains("TABLE_TYPE = 'BASE TABLE'", MySqlProvedorDeSchema.ConsultaDeTabelas);
    }

    [Fact]
    public void ConsultaDeTabelas_LimitaQuantidadeDeTabelas()
    {
        Assert.Contains("LIMIT", MySqlProvedorDeSchema.ConsultaDeTabelas);
    }

    [Fact]
    public void ConsultaDeColunas_FiltraPorTabela()
    {
        Assert.Contains("AND TABLE_NAME = @tabela", MySqlProvedorDeSchema.ConsultaDeColunas);
        Assert.DoesNotContain("ORDER BY TABLE_NAME", MySqlProvedorDeSchema.ConsultaDeColunas);
    }

    [Fact]
    public void ConsultaDeChavesPrimarias_FiltraPorTabela()
    {
        Assert.Contains("AND tc.TABLE_NAME = @tabela", MySqlProvedorDeSchema.ConsultaDeChavesPrimarias);
    }

    [Fact]
    public void ConsultaDeChavesEstrangeiras_FiltraPorTabela()
    {
        Assert.Contains("AND kcu.TABLE_NAME = @tabela", MySqlProvedorDeSchema.ConsultaDeChavesEstrangeiras);
    }

    [Fact]
    public void CriarAvisoDeTruncamento_DescreveOLimiteDeTabelas()
    {
        var aviso = MySqlProvedorDeSchema.CriarAvisoDeTruncamento();

        Assert.Contains("500", aviso);
        Assert.Contains("ordem alfabética", aviso);
    }

    [Fact]
    public void Chaves_SaoCompostasPorEsquemaENome()
    {
        Assert.Equal("erp.clientes", MySqlProvedorDeSchema.ChaveDaTabela("erp", "clientes"));
        Assert.Equal(
            "erp.clientes.id",
            MySqlProvedorDeSchema.ChaveDaColuna("erp", "clientes", "id"));
    }
}
