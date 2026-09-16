import type { RelacionamentoDeBanco, TabelaDeBanco } from '../modelos/tipos';
import { idDeRelacionamento, idDeTabelaPorChaves } from '../modelos/tipos';

/**
 * Foco em relacionamentos diretos (US3): dada uma tabela selecionada, retorna
 * os IDs da própria tabela, das tabelas diretamente relacionadas e dos edges
 * envolvidos. Nível de relacionamento único neste MVP.
 */

export interface FocoDeRelacionamento {
  idsDeTabelas: Set<string>;
  idsDeEdges: Set<string>;
}

export function filtrarRelacionamentosDiretos(
  _tabelas: TabelaDeBanco[],
  relacionamentos: RelacionamentoDeBanco[],
  tabelaSelecionada: string
): FocoDeRelacionamento {
  const idsDeTabelas = new Set<string>([tabelaSelecionada]);
  const idsDeEdges = new Set<string>();

  for (const relacionamento of relacionamentos) {
    const origem = idDeTabelaPorChaves(
      relacionamento.esquemaOrigem,
      relacionamento.tabelaOrigem
    );
    const destino = idDeTabelaPorChaves(
      relacionamento.esquemaDestino,
      relacionamento.tabelaDestino
    );

    const envolveAselecionada =
      origem === tabelaSelecionada || destino === tabelaSelecionada;

    if (!envolveAselecionada) {
      continue;
    }

    idsDeTabelas.add(origem);
    idsDeTabelas.add(destino);
    idsDeEdges.add(idDeRelacionamento(relacionamento));
  }

  return { idsDeTabelas, idsDeEdges };
}