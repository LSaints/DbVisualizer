using DatabaseDiagram.Api.Modelos;
using DatabaseDiagram.Api.Servicos;

namespace DatabaseDiagram.Testes.Servicos;

public class ConstrutorDeContextoDeBancoTestes
{
    private static EsquemaDeBanco CriarEsquema(string? aviso = null) => new()
    {
        Provedor = "mysql",
        NomeDoBanco = "erp",
        Versao = "8.0.35",
        Aviso = aviso,
        Tabelas =
        [
            new TabelaDeBanco
            {
                Esquema = "erp",
                Nome = "contratos",
                Colunas =
                [
                    new ColunaDeBanco { Nome = "id", TipoDoDado = "int", PosicaoOrdinal = 1, ChavePrimaria = true },
                    new ColunaDeBanco { Nome = "cliente_id", TipoDoDado = "int", PosicaoOrdinal = 2, ChaveEstrangeira = true },
                    new ColunaDeBanco { Nome = "status", TipoDoDado = "varchar", PosicaoOrdinal = 3 }
                ]
            }
        ],
        Relacionamentos =
        [
            new RelacionamentoDeBanco
            {
                EsquemaOrigem = "erp",
                TabelaOrigem = "contratos",
                ColunaOrigem = "cliente_id",
                EsquemaDestino = "erp",
                TabelaDestino = "clientes",
                ColunaDestino = "id"
            }
        ]
    };

    [Fact]
    public void Construir_ComSchemaCompleto_DescreveAmbienteTabelasERelacionamentos()
    {
        var construtor = new ConstrutorDeContextoDeBanco();

        var prompt = construtor.Construir(CriarEsquema());

        Assert.Contains("mysql", prompt);
        Assert.Contains("8.0.35", prompt);
        Assert.Contains("erp", prompt);
        Assert.Contains("contratos", prompt);
        Assert.Contains("id", prompt);
        Assert.Contains("cliente_id", prompt);
        Assert.Contains("[PK]", prompt);
        Assert.Contains("[FK]", prompt);
        Assert.Contains("contratos.cliente_id", prompt);
        Assert.Contains("clientes.id", prompt);
    }

    [Fact]
    public void Construir_InstruiGerarSomenteLeitura()
    {
        var construtor = new ConstrutorDeContextoDeBanco();

        var prompt = construtor.Construir(CriarEsquema());

        Assert.Contains("SELECT", prompt);
        Assert.Contains("somente leitura", prompt);
    }

    [Fact]
    public void Construir_DeclaraAvisoDeTruncamentoQuandoPreenchido()
    {
        var construtor = new ConstrutorDeContextoDeBanco();

        var prompt = construtor.Construir(CriarEsquema(aviso: "O banco possui mais de 500 tabelas."));

        Assert.Contains("O banco possui mais de 500 tabelas.", prompt);
        Assert.Contains("relacionamentos", prompt);
    }

    [Fact]
    public void Construir_SemAviso_NaoMencionaLimite()
    {
        var construtor = new ConstrutorDeContextoDeBanco();

        var prompt = construtor.Construir(CriarEsquema());

        Assert.DoesNotContain("possui mais de", prompt);
    }
}