import dagre from 'dagre';
import type { RelacionamentoDeBanco, TabelaDeBanco } from '../modelos/tipos';
import { idDeRelacionamento, idDeTabela, idDeTabelaPorChaves } from '../modelos/tipos';

/**
 * Layout automático com Dagre (decisão D4 em research.md). Gera posições
 * (x, y) para cada tabela respeitando a direção dos relacionamentos e
 * mantendo tabelas isoladas distribuídas sem sobreposição excessiva.
 */

export const LARGURA_DA_TABELA = 240;
export const ALTURA_DO_CABECALHO = 34;
export const ALTURA_DA_LINHA = 26;
export const ESPACO_DO_RODAPE = 10;

export interface Posicao {
  x: number;
  y: number;
}

interface PontoDoGrafo {
  x: number;
  y: number;
  width: number;
  height: number;
}

export function calcularAlturaDaTabela(tabela: TabelaDeBanco): number {
  return (
    ALTURA_DO_CABECALHO +
    tabela.colunas.length * ALTURA_DA_LINHA +
    ESPACO_DO_RODAPE
  );
}

/**
 * Calcula as posições (canto superior esquerdo, como o React Flow espera)
 * para as tabelas informadas.
 */
export function aplicarLayout(
  tabelas: TabelaDeBanco[],
  relacionamentos: RelacionamentoDeBanco[]
): Record<string, Posicao> {
  const grafo = new dagre.graphlib.Graph();
  grafo.setGraph({
    rankdir: 'LR',
    nodesep: 60,
    ranksep: 100,
    marginx: 30,
    marginy: 30
  });
  grafo.setDefaultEdgeLabel(() => ({}));

  for (const tabela of tabelas) {
    grafo.setNode(idDeTabela(tabela), {
      width: LARGURA_DA_TABELA,
      height: calcularAlturaDaTabela(tabela)
    });
  }

  for (const relacionamento of relacionamentos) {
    const origem = idDeTabelaPorChaves(
      relacionamento.esquemaOrigem,
      relacionamento.tabelaOrigem
    );
    const destino = idDeTabelaPorChaves(
      relacionamento.esquemaDestino,
      relacionamento.tabelaDestino
    );

    if (grafo.hasNode(origem) && grafo.hasNode(destino)) {
      grafo.setEdge(origem, destino, {
        id: idDeRelacionamento(relacionamento)
      });
    }
  }

  dagre.layout(grafo);

  const posicoes: Record<string, Posicao> = {};
  for (const chave of grafo.nodes()) {
    const ponto = grafo.node(chave) as PontoDoGrafo;
    posicoes[chave] = {
      x: ponto.x - ponto.width / 2,
      y: ponto.y - ponto.height / 2
    };
  }

  return posicoes;
}