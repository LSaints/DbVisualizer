/**
 * Modelo de domínio comum consumido pelo frontend, espelhando data-model.md.
 * IDs e chaves JSON em pt-BR (camelCase).
 */

/** Parâmetros de conexão informados pelo usuário. Efêmeros (nunca persistidos). */
export interface ConexaoDeBanco {
  provedor: string;
  host: string;
  porta?: number;
  bancoDeDados: string;
  usuario: string;
  senha: string;
}

/** Raiz do schema descoberto em um database. */
export interface EsquemaDeBanco {
  provedor: string;
  nomeDoBanco: string;
  /** Versão do banco reportada pelo provider, ex.: `8.0.35`. */
  versao?: string | null;
  charset?: string | null;
  collation?: string | null;
  tabelas: TabelaDeBanco[];
  relacionamentos: RelacionamentoDeBanco[];
  /** Aviso em pt-BR sobre limitações da introspecção (ex.: schema truncado). */
  aviso?: string | null;
}

/** Tabela do banco (MVP prioriza BASE TABLE). */
export interface TabelaDeBanco {
  esquema: string;
  nome: string;
  tipoDaTabela?: string | null;
  motor?: string | null;
  comentario?: string | null;
  colunas: ColunaDeBanco[];
}

/** Coluna de uma tabela. */
export interface ColunaDeBanco {
  nome: string;
  tipoDoDado: string;
  tipoCompletoDoDado?: string | null;
  valorPadrao?: string | null;
  comentario?: string | null;
  posicaoOrdinal: number;
  podeSerNula: boolean;
  chavePrimaria: boolean;
  chaveEstrangeira: boolean;
  autoIncremento: boolean;
}

/** Relacionamento por chave estrangeira (origem → destino). */
export interface RelacionamentoDeBanco {
  esquemaOrigem: string;
  tabelaOrigem: string;
  colunaOrigem: string;
  esquemaDestino: string;
  tabelaDestino: string;
  colunaDestino: string;
  nomeDaRestricao?: string | null;
  regraDeAtualizacao?: string | null;
  regraDeExclusao?: string | null;
}

/** Resposta de POST /api/esquema/mais-tabelas ("carregar mais"). */
export interface PaginaDeTabelas {
  tabelas: TabelaDeBanco[];
  relacionamentos: RelacionamentoDeBanco[];
  temMaisTabelas: boolean;
}

/** Resposta de POST /api/conexoes/teste. */
export interface RespostaTesteConexao {
  sucesso: boolean;
  provedor: string;
  mensagem?: string | null;
}

/**
 * Identificador único de tabela: esquema + nome (evita conflito de tabelas
 * homônimas em schemas diferentes).
 */
export function idDeTabela(tabela: TabelaDeBanco): string {
  return `${tabela.esquema}.${tabela.nome}`;
}

export function idDeTabelaPorChaves(esquema: string, nome: string): string {
  return `${esquema}.${nome}`;
}

/** Identificador único de relacionamento (origem.coluna → destino). */
export function idDeRelacionamento(relacionamento: RelacionamentoDeBanco): string {
  return `${idDeTabelaPorChaves(relacionamento.esquemaOrigem, relacionamento.tabelaOrigem)}:${relacionamento.colunaOrigem}->${idDeTabelaPorChaves(relacionamento.esquemaDestino, relacionamento.tabelaDestino)}:${relacionamento.colunaDestino}`;
}