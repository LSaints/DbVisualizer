using DatabaseDiagram.Api.Modelos;
using DatabaseDiagram.Api.Provedores;

namespace DatabaseDiagram.Api.Servicos;

/// <summary>
/// Resolve o provider na fábrica e retorna o <see cref="EsquemaDeBanco"/>
/// para a conexão informada. Independe do banco de origem (constituição IV).
/// Limita a concorrência de introspecções para não sobrecarregar o banco alvo.
/// </summary>
public sealed class ServicoDeSchema
{
    /// <summary>Quantas introspecções podem rodar simultaneamente.</summary>
    internal const int ConcorrenciaMaximaDeIntrospeccao = 2;

    private static readonly TimeSpan TempoMaximoDeEspera = TimeSpan.FromSeconds(15);

    private readonly FabricaDeProvedoresDeSchema _fabrica;
    private readonly SemaphoreSlim _portaoDeConcorrencia;
    private readonly TimeSpan _tempoMaximoDeEspera;

    public ServicoDeSchema(FabricaDeProvedoresDeSchema fabrica)
        : this(fabrica, ConcorrenciaMaximaDeIntrospeccao, TempoMaximoDeEspera)
    {
    }

    internal ServicoDeSchema(
        FabricaDeProvedoresDeSchema fabrica,
        int concorrenciaMaxima,
        TimeSpan tempoMaximoDeEspera)
    {
        _fabrica = fabrica;
        _portaoDeConcorrencia = new SemaphoreSlim(concorrenciaMaxima);
        _tempoMaximoDeEspera = tempoMaximoDeEspera;
    }

    public async Task<EsquemaDeBanco> ObterAsync(
        ConexaoDeBanco conexao,
        CancellationToken cancellationToken)
    {
        var provedor = _fabrica.ObterProvedor(conexao.Provedor)
            ?? throw new ProvedorNaoSuportadoException(conexao.Provedor);

        if (!await _portaoDeConcorrencia.WaitAsync(_tempoMaximoDeEspera, cancellationToken))
        {
            throw new IntrospeccaoOcupadaException(
                "Limite de introspecções simultâneas excedido. Aguarde e tente novamente.");
        }

        try
        {
            return await provedor.ObterEsquemaAsync(conexao, cancellationToken);
        }
        finally
        {
            _portaoDeConcorrencia.Release();
        }
    }
}