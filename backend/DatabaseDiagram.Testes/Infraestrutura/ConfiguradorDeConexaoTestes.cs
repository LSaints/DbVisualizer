using DatabaseDiagram.Api.Infraestrutura;
using DatabaseDiagram.Api.Modelos;

namespace DatabaseDiagram.Testes.Infraestrutura;

public class ConfiguradorDeConexaoTestes
{
    [Fact]
    public void CriarConnectionString_MontaParametrosEmMemoria()
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

        var connectionString = new ConfiguradorDeConexao().CriarConnectionString(conexao);

        Assert.Contains("Server=localhost", connectionString);
        Assert.Contains("Port=3306", connectionString);
        Assert.Contains("Database=erp", connectionString);
        Assert.Contains("User ID=readonly", connectionString);
        Assert.Contains("Password=segredo", connectionString);
    }

    [Fact]
    public void CriarConnectionString_UsaPortaInformada()
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

        var connectionString = new ConfiguradorDeConexao().CriarConnectionString(conexao);

        Assert.Contains("Server=banco.example.com", connectionString);
        Assert.Contains("Port=3307", connectionString);
    }
}
