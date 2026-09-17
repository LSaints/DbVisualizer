import type {
  Conversa,
  ConversaDetalhada,
  ProvedorDeIaDisponivel,
  RequisicaoDeConsultaDoAssistente,
  RespostaDeConsultaDoAssistente
} from '../modelos/tiposDoAssistente';

/**
 * Cliente dos endpoints do assistente e das conversas (contracts/api.md).
 * Mensagens de erro em pt-BR, orientadas à ação e sem expor a chave de API.
 */

/** Retorno de `gerarConsulta`: o contrato estruturado e a flag de truncamento do contexto (FR-011). */
export interface RespostaDeGerarConsulta {
  resposta: RespostaDeConsultaDoAssistente;
  contextoTruncado: boolean;
}

/** POST /api/assistente/consultas — gera a consulta com o contexto do banco. */
export async function gerarConsulta(
  requisicao: RequisicaoDeConsultaDoAssistente,
  sinal?: AbortSignal
): Promise<RespostaDeGerarConsulta> {
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
    if (resposta.status === 404) {
      throw new Error('Conversa não encontrada.');
    }
    if (resposta.status === 409) {
      throw new Error(
        corpo && 'mensagem' in corpo && corpo.mensagem
          ? corpo.mensagem
          : 'O banco conectado difere do registrado nesta conversa.'
      );
    }
    throw new Error(
      corpo && 'mensagem' in corpo && corpo.mensagem
        ? corpo.mensagem
        : 'Não foi possível gerar a consulta.'
    );
  }

  const contextoTruncado = resposta.headers.get('X-Contexto-Truncado') === 'true';

  return { resposta: corpo as RespostaDeConsultaDoAssistente, contextoTruncado };
}

/** GET /api/assistente/provedores — somente os provedores implementados na fábrica. */
export async function listarProvedores(): Promise<ProvedorDeIaDisponivel[]> {
  const resposta = await fetch('/api/assistente/provedores');

  if (!resposta.ok) {
    throw new Error('Não foi possível carregar os provedores de IA.');
  }

  return (await resposta.json()) as ProvedorDeIaDisponivel[];
}

/** GET /api/conversas — lista as conversas persistidas (ordenadas por atualizadaEm desc). */
export async function listarConversas(): Promise<Conversa[]> {
  const resposta = await fetch('/api/conversas');

  if (!resposta.ok) {
    throw new Error('Não foi possível carregar as conversas.');
  }

  return (await resposta.json()) as Conversa[];
}

/** POST /api/conversas — cria uma conversa nova (vazia). */
export async function criarConversa(titulo?: string): Promise<Conversa> {
  const resposta = await fetch('/api/conversas', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(titulo ? { titulo } : {})
  });

  if (!resposta.ok) {
    throw new Error('Não foi possível criar a conversa.');
  }

  return (await resposta.json()) as Conversa;
}

/** GET /api/conversas/{id} — histórico completo da conversa. */
export async function obterConversa(id: string): Promise<ConversaDetalhada> {
  const resposta = await fetch(`/api/conversas/${id}`);

  if (!resposta.ok) {
    throw new Error('Conversa não encontrada.');
  }

  return (await resposta.json()) as ConversaDetalhada;
}

/** PATCH /api/conversas/{id}/titulo — renomeia a conversa (D6). */
export async function renomearConversa(id: string, titulo: string): Promise<void> {
  const resposta = await fetch(`/api/conversas/${id}/titulo`, {
    method: 'PATCH',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ titulo })
  });

  if (!resposta.ok) {
    const corpo = (await resposta.json().catch(() => null)) as { mensagem?: string } | null;
    throw new Error(corpo?.mensagem ?? 'Não foi possível renomear a conversa.');
  }
}

/** DELETE /api/conversas/{id} — exclui a conversa permanentemente. */
export async function excluirConversa(id: string): Promise<void> {
  const resposta = await fetch(`/api/conversas/${id}`, { method: 'DELETE' });

  if (!resposta.ok) {
    throw new Error('Não foi possível excluir a conversa.');
  }
}
