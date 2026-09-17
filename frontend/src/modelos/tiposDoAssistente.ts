import type { EsquemaDeBanco } from './tipos';

/**
 * Contratos do Assistente IA de Consultas SQL (contracts/api.md + data-model.md).
 * As chaves compostas seguem exatamente o exemplo informado pelo usuário
 * (decisão D1): snake_case em `campos_retorno`, `tipo_consulta`,
 * `resultado_esperado` e nos sub-objetos de `relacionamentos`.
 */

export interface TabelaDaResposta {
  nome: string;
  apelido: string;
  funcao: string;
}

export interface RelacionamentoDaResposta {
  tabela_origem: string;
  campo_origem: string;
  tabela_destino: string;
  campo_destino: string;
  tipo: string;
  explicacao: string;
}

export interface FiltroDaResposta {
  campo: string;
  operador: string;
  valor: string;
  explicacao: string;
}

/** Mapa `tabela → campos` com a chave reservada `explicacao`. */
export type CamposDeRetorno = Record<string, string>;

export interface ParametroDaResposta {
  nome: string;
  valor: string;
  descricao: string;
}

/** Contrato estruturado da resposta do assistente (chaves do exemplo do usuário). */
export interface RespostaDeConsultaDoAssistente {
  consulta: string;
  explicacao: string;
  objetivo: string;
  tabelas: TabelaDaResposta[];
  relacionamentos: RelacionamentoDaResposta[];
  filtros: FiltroDaResposta[];
  campos_retorno: CamposDeRetorno;
  tipo_consulta: string;
  resultado_esperado: string;
  parametros: ParametroDaResposta[];
}

/** Corpo de `POST /api/assistente/consultas`. A `chaveDeApi` é efêmera (em memória). */
export interface RequisicaoDeConsultaDoAssistente {
  provedorDeIa: string;
  chaveDeApi: string;
  mensagem: string;
  contextoDeBanco: EsquemaDeBanco;
  /** Id da conversa (US1); ausente = fluxo legado de troca única, sem persistência. */
  conversaId?: string;
}

/** Item de `GET /api/assistente/provedores`. */
export interface ProvedorDeIaDisponivel {
  provedor: string;
  rotulo: string;
}

/** Identidade do banco registrada em uma conversa (FR-013/D7). */
export interface IdentidadeDeBanco {
  provedor: string;
  nomeDoBanco: string;
  versao?: string;
}

/** Uma troca persistida na conversa (D3/data-model.md). */
export interface MensagemDaConversa {
  papel: 'usuario' | 'assistente';
  conteudo: string | RespostaDeConsultaDoAssistente;
  criadaEm: string;
}

/** Item de `GET /api/conversas` (contracts/api.md). */
export interface Conversa {
  id: string;
  titulo: string;
  criadaEm: string;
  atualizadaEm: string;
  resumo: string;
}

/** Resposta de `GET /api/conversas/{id}`. */
export interface ConversaDetalhada extends Conversa {
  contextoDeBanco?: IdentidadeDeBanco | null;
  mensagens: MensagemDaConversa[];
}