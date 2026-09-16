using System.Text;
using DatabaseDiagram.Api.Modelos;

namespace DatabaseDiagram.Api.Servicos;

/// <summary>
/// Monta o prompt de sistema em pt-BR a partir do <see cref="EsquemaDeBanco"/>
/// enviado pelo frontend (decisão D2): papel do assistente, ambiente (provedor,
/// versão, nome do banco, tabelas com colunas e flags de PK/FK, relacionamentos),
/// o contrato estruturado esperado (JSON) e a restrição de gerar somente leitura
/// (SELECT), instruindo explicar a limitação no campo <c>explicacao</c> quando o
/// pedido implicar escrita (FR-017). Se o schema vier truncado (<c>Aviso</c>),
/// declara o limite e prioriza os relacionamentos.
/// </summary>
public sealed class ConstrutorDeContextoDeBanco
{
    public string Construir(EsquemaDeBanco esquema)
    {
        var construtor = new StringBuilder();

        construtor.AppendLine(
            "Você é um assistente especializado em gerar consultas SQL somente leitura (SELECT) "
            + "para o banco de dados do usuário. Responda sempre em pt-BR e estritamente no "
            + "formato JSON abaixo, sem texto fora do JSON.");

        var versao = string.IsNullOrWhiteSpace(esquema.Versao) ? "não informada" : esquema.Versao;
        construtor.AppendLine();
        construtor.AppendLine($"Ambiente: banco {esquema.NomeDoBanco} · provedor {esquema.Provedor} · versão {versao}.");

        construtor.AppendLine();
        construtor.AppendLine("Tabelas do banco:");
        foreach (var tabela in esquema.Tabelas)
        {
            var colunas = tabela.Colunas.Count == 0
                ? "(sem colunas)"
                : string.Join(", ", tabela.Colunas.Select(MontarColuna));
            construtor.AppendLine($"- {tabela.Nome}: {colunas}");
        }

        if (esquema.Relacionamentos.Count > 0)
        {
            construtor.AppendLine();
            construtor.AppendLine("Relacionamentos:");
            foreach (var relacionamento in esquema.Relacionamentos)
            {
                construtor.AppendLine(
                    $"- {relacionamento.TabelaOrigem}.{relacionamento.ColunaOrigem} → "
                    + $"{relacionamento.TabelaDestino}.{relacionamento.ColunaDestino}");
            }
        }

        construtor.AppendLine();
        construtor.AppendLine(
            "Responda em JSON com as chaves exatas: consulta, explicacao, objetivo, tabelas, "
            + "relacionamentos, filtros, campos_retorno, tipo_consulta, resultado_esperado, parametros. "
            + "Em tabelas use { nome, apelido, funcao }; em relacionamentos use "
            + "{ tabela_origem, campo_origem, tabela_destino, campo_destino, tipo, explicacao }; "
            + "em filtros use { campo, operador, valor, explicacao }; em campos_retorno cada "
            + "chave é uma tabela e cada valor é uma string com os campos retornados separados por "
            + "vírgula (ex.: \"clientes\": \"id, nome, email\"), com a chave reservada explicacao; "
            + "em parametros use { nome, valor, descricao }.");

        construtor.AppendLine();
        construtor.AppendLine(
            "Gere somente consultas de leitura (SELECT). Se o pedido do usuário implicar escrita "
            + "(INSERT, UPDATE, DELETE, DDL), não gere o comando: explique essa limitação no campo "
            + "explicacao do JSON. A consulta gerada é apenas exibida ao usuário, nunca executada.");

        if (!string.IsNullOrWhiteSpace(esquema.Aviso))
        {
            construtor.AppendLine();
            construtor.AppendLine($"Aviso do schema: {esquema.Aviso.Trim()}. "
                + "Prefira os relacionamentos informados acima às colunas de tabelas não relacionadas.");
        }

        return construtor.ToString();
    }

    private static string MontarColuna(ColunaDeBanco coluna)
    {
        var sufixo = coluna.ChavePrimaria ? " [PK]" : coluna.ChaveEstrangeira ? " [FK]" : string.Empty;
        return $"{coluna.Nome} ({coluna.TipoDoDado}){sufixo}";
    }
}