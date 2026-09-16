using DatabaseDiagram.Api.Provedores;
using DatabaseDiagram.Api.Provedores.Ia.GoogleAiStudio;
using DatabaseDiagram.Api.Provedores.Ia.OpenAi;

namespace DatabaseDiagram.Testes.Provedores;

public class FabricaDeProvedoresDeIaTestes
{
    [Fact]
    public void Obter_ComTipoOpenai_RetornaProvedorDoOpenAi()
    {
        using var httpClient = new HttpClient();
        var fabrica = new FabricaDeProvedoresDeIa([
            new OpenAiProvedorDeIa(httpClient)
        ]);

        var provedor = fabrica.Obter("openai");

        Assert.IsType<OpenAiProvedorDeIa>(provedor);
        Assert.Equal("openai", provedor.Tipo);
    }

    [Fact]
    public void Obter_ComTipoGoogleAiStudio_RetornaProvedorDoGoogle()
    {
        using var httpClient = new HttpClient();
        var fabrica = new FabricaDeProvedoresDeIa([
            new GoogleAiStudioProvedorDeIa(httpClient)
        ]);

        var provedor = fabrica.Obter("google-ai-studio");

        Assert.IsType<GoogleAiStudioProvedorDeIa>(provedor);
        Assert.Equal("google-ai-studio", provedor.Tipo);
    }

    [Fact]
    public void Obter_ComTipoDesconhecido_LancaProvedorDeIaNaoSuportadoException()
    {
        var fabrica = new FabricaDeProvedoresDeIa([]);

        var excecao = Assert.Throws<ProvedorDeIaNaoSuportadoException>(() => fabrica.Obter("claude"));

        Assert.Equal("claude", excecao.Provedor);
    }

    [Fact]
    public void Suporta_ComTipoRegistrado_RetornaVerdadeiro()
    {
        using var httpClient = new HttpClient();
        var fabrica = new FabricaDeProvedoresDeIa([
            new OpenAiProvedorDeIa(httpClient)
        ]);

        Assert.True(fabrica.Suporta("openai"));
        Assert.False(fabrica.Suporta("gemini"));
        Assert.False(fabrica.Suporta(null));
    }

    [Fact]
    public void Listar_RetornaApenasOsProvedoresRegistrados()
    {
        using var httpClient = new HttpClient();
        var fabrica = new FabricaDeProvedoresDeIa([
            new OpenAiProvedorDeIa(httpClient)
        ]);

        var provedores = fabrica.Listar();

        Assert.Single(provedores);
        Assert.Equal("openai", provedores[0].Tipo);
    }
}