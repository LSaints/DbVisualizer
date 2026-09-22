import type {
  ConexaoDeBanco,
  EsquemaDeBanco,
  PaginaDeTabelas,
  RespostaTesteConexao
} from '../modelos/tipos';

/**
 * Cliente da API (contracts/api.md). Mensagens de erro em pt-BR; a senha nunca
 * é retornada pela API.
 */

const TEMPO_MAXIMO_EM_MILISSEGUNDOS = 60_000;

async function corpoDaResposta<T>(resposta: Response): Promise<T> {
  return (await resposta.json()) as T;
}

/** Anula a requisição após o timeout para não acumular chamadas pendentes. */
async function requisitar(caminho: string, corpo: unknown): Promise<Response> {
  const controlador = new AbortController();
  const temporizador = setTimeout(() => controlador.abort(), TEMPO_MAXIMO_EM_MILISSEGUNDOS);

  try {
    return await fetch(caminho, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(corpo),
      signal: controlador.signal
    });
  } finally {
    clearTimeout(temporizador);
  }
}

/** POST /api/conexoes/teste — testa a conexão sem armazená-la. */
export async function testarConexao(conexao: ConexaoDeBanco): Promise<RespostaTesteConexao> {
  const resposta = await requisitar('/api/conexoes/teste', conexao);

  return corpoDaResposta<RespostaTesteConexao>(resposta);
}

/** POST /api/esquema — realiza a introspecção (somente leitura) e retorna o schema. */
export async function obterEsquema(conexao: ConexaoDeBanco): Promise<EsquemaDeBanco> {
  const resposta = await requisitar('/api/esquema', conexao);

  const corpo = (await resposta.json()) as EsquemaDeBanco & { mensagem?: string };

  if (!resposta.ok) {
    throw new Error(corpo?.mensagem ?? 'Não foi possível carregar o schema.');
  }

  return corpo as EsquemaDeBanco;
}

/**
 * POST /api/esquema/mais-tabelas — busca o próximo lote de tabelas
 * ("carregar mais"), excluindo as já carregadas pelo cliente.
 */
export async function obterMaisTabelas(
  conexao: ConexaoDeBanco,
  tabelasCarregadas: string[]
): Promise<PaginaDeTabelas> {
  const resposta = await requisitar('/api/esquema/mais-tabelas', {
    ...conexao,
    tabelasCarregadas
  });

  const corpo = (await resposta.json()) as PaginaDeTabelas & { mensagem?: string };

  if (!resposta.ok) {
    throw new Error(corpo?.mensagem ?? 'Não foi possível carregar mais tabelas.');
  }

  return corpo as PaginaDeTabelas;
}