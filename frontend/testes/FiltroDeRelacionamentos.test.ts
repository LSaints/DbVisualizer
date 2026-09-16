import { describe, expect, it } from 'vitest';
import type { RelacionamentoDeBanco, TabelaDeBanco } from '../src/modelos/tipos';
import { idDeTabela } from '../src/modelos/tipos';
import { filtrarRelacionamentosDiretos } from '../src/servicos/FiltroDeRelacionamentos';

const tabelas: TabelaDeBanco[] = [
  { esquema: 'erp', nome: 'clientes', colunas: [] },
  { esquema: 'erp', nome: 'pedidos', colunas: [] },
  { esquema: 'erp', nome: 'pagamentos', colunas: [] },
  { esquema: 'fin', nome: 'lancamentos', colunas: [] }
];

const relacionamentos: RelacionamentoDeBanco[] = [
  {
    esquemaOrigem: 'erp',
    tabelaOrigem: 'pedidos',
    colunaOrigem: 'cliente_id',
    esquemaDestino: 'erp',
    tabelaDestino: 'clientes',
    colunaDestino: 'id',
    nomeDaRestricao: 'fk_pedidos_clientes'
  },
  {
    esquemaOrigem: 'erp',
    tabelaOrigem: 'pagamentos',
    colunaOrigem: 'pedido_id',
    esquemaDestino: 'erp',
    tabelaDestino: 'pedidos',
    colunaDestino: 'id'
  },
  {
    esquemaOrigem: 'fin',
    tabelaOrigem: 'lancamentos',
    colunaOrigem: 'cliente_id',
    esquemaDestino: 'erp',
    tabelaDestino: 'clientes',
    colunaDestino: 'id'
  }
];

describe('filtrarRelacionamentosDiretos', () => {
  it('mantém apenas a tabela selecionada e as diretamente relacionadas', () => {
    const foco = filtrarRelacionamentosDiretos(
      tabelas,
      relacionamentos,
      idDeTabela({ esquema: 'erp', nome: 'pedidos', colunas: [] })
    );

    expect(foco.idsDeTabelas).toEqual(
      new Set(['erp.pedidos', 'erp.clientes', 'erp.pagamentos'])
    );
    expect(foco.idsDeEdges.size).toBe(2);
  });

  it('não inclui tabelas indiretamente relacionadas', () => {
    const foco = filtrarRelacionamentosDiretos(
      tabelas,
      relacionamentos,
      'erp.clientes'
    );

    expect(foco.idsDeTabelas.has('fin.lancamentos')).toBe(true);
    expect(foco.idsDeTabelas).toEqual(
      new Set(['erp.clientes', 'erp.pedidos', 'fin.lancamentos'])
    );
  });

  it('tabela sem relacionamentos mantém apenas a própria tabela', () => {
    const foco = filtrarRelacionamentosDiretos(tabelas, relacionamentos, 'erp.solitaria');

    expect(foco.idsDeTabelas).toEqual(new Set(['erp.solitaria']));
    expect(foco.idsDeEdges.size).toBe(0);
  });
});