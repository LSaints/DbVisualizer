import type {
  ProvedorDeIaDisponivel,
  RequisicaoDeConsultaDoAssistente,
  RespostaDeConsultaDoAssistente
} from '../modelos/tiposDoAssistente';

/**
 * Cliente dos endpoints do assistente (contracts/api.md). Mensagens de erro em
 * pt-BR, orientadas à ação e sem expor a chave de API.
 */

/** POST /api/assistente/consultas — gera a consulta com o contexto do banco. */
export async function gerarConsulta(
  requisicao: RequisicaoDeConsultaDoAssistente,
  sinal?: AbortSignal
): Promise<RespostaDeConsultaDoAssistente> {
  const resposta = await fetch('/api/assistente/consultas', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(requisicao),
    signal: sinal
  });

  const corpo = (await resposta.json()) as
    | RespostaDeConsultaDoAssistente
    | { mensagem?: string };

  if (!resposta.ok) {
    throw new Error(
      corpo && 'mensagem' in corpo && corpo.mensagem
        ? corpo.mensagem
        : 'Não foi possível gerar a consulta.'
    );
  }

  return corpo as RespostaDeConsultaDoAssistente;
}

/** GET /api/assistente/provedores — somente os provedores implementados na fábrica. */
export async function listarProvedores(): Promise<ProvedorDeIaDisponivel[]> {
  const resposta = await fetch('/api/assistente/provedores');

  if (!resposta.ok) {
    throw new Error('Não foi possível carregar os provedores de IA.');
  }

  return (await resposta.json()) as ProvedorDeIaDisponivel[];
}