using DatabaseDiagram.Api.Infraestrutura;
using DatabaseDiagram.Api.Modelos;

namespace DatabaseDiagram.Testes.Infraestrutura;

public class ConfiguradorDeConexaoTestes
{
    [Fact]
    public void CriarStringDeConexao_MontaParametrosEmMemoria()
    {
        var conexao = new ConexaoDeBanco
        {
            Provedor = "mysql",
            Host = "localhost",
            Porta = 3306,
            BancoDeDados = "erp",
            Usuario = "readonly",
            Senha = "segredo"
        };

        var stringDeConexao = new ConfiguradorDeConexao().CriarStringDeConexao(conexao);

        Assert.Contains("Server=localhost", stringDeConexao);
        Assert.Contains("Port=3306", stringDeConexao);
        Assert.Contains("Database=erp", stringDeConexao);
        Assert.Contains("User ID=readonly", stringDeConexao);
        Assert.Contains("Password=segredo", stringDeConexao);
    }

    [Fact]
    public void CriarStringDeConexao_UsaPortaInformada()
    {
        var conexao = new ConexaoDeBanco
        {
            Provedor = "mysql",
            Host = "banco.example.com",
            Porta = 3307,
            BancoDeDados = "erp",
            Usuario = "readonly",
            Senha = "segredo"
        };

        var stringDeConexao = new ConfiguradorDeConexao().CriarStringDeConexao(conexao);

        Assert.Contains("Server=banco.example.com", stringDeConexao);
        Assert.Contains("Port=3307", stringDeConexao);
    }
}
