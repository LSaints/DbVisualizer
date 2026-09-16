import { describe, expect, it } from 'vitest';
import type { TabelaDeBanco } from '../src/modelos/tipos';
import {
  aplicarLayout,
  calcularAlturaDaTabela
} from '../src/servicos/LayoutDoDiagrama';

const clientes: TabelaDeBanco = {
  esquema: 'erp',
  nome: 'clientes',
  colunas: [{ nome: 'id', tipoDoDado: 'int', posicaoOrdinal: 1, podeSerNula: false, chavePrimaria: true, chaveEstrangeira: false, autoIncremento: true }]
};

const pedidos: TabelaDeBanco = {
  esquema: 'erp',
  nome: 'pedidos',
  colunas: [{ nome: 'cliente_id', tipoDoDado: 'int', posicaoOrdinal: 1, podeSerNula: false, chavePrimaria: false, chaveEstrangeira: true, autoIncremento: false }]
};

describe('aplicarLayout', () => {
  it('gera posições finitas para todas as tabelas', () => {
    const posicoes = aplicarLayout(
      [clientes, pedidos],
      [
        {
          esquemaOrigem: 'erp',
          tabelaOrigem: 'pedidos',
          colunaOrigem: 'cliente_id',
          esquemaDestino: 'erp',
          tabelaDestino: 'clientes',
          colunaDestino: 'id'
        }
      ]
    );

    for (const chave of ['erp.clientes', 'erp.pedidos']) {
      expect(Number.isFinite(posicoes[chave].x)).toBe(true);
      expect(Number.isFinite(posicoes[chave].y)).toBe(true);
    }
  });

  it('tabelas isoladas também recebem posição', () => {
    const sozinha: TabelaDeBanco = {
      esquema: 'erp',
      nome: 'auditoria',
      colunas: []
    };

    const posicoes = aplicarLayout([sozinha], []);

    expect(posicoes['erp.auditoria']).toBeDefined();
  });

  it('tabelas relacionadas usam posições distintas (sem sobreposição exata)', () => {
    const posicoes = aplicarLayout(
      [clientes, pedidos],
      [
        {
          esquemaOrigem: 'erp',
          tabelaOrigem: 'pedidos',
          colunaOrigem: 'cliente_id',
          esquemaDestino: 'erp',
          tabelaDestino: 'clientes',
          colunaDestino: 'id'
        }
      ]
    );

    expect(posicoes['erp.clientes']).not.toEqual(posicoes['erp.pedidos']);
  });
});

describe('calcularAlturaDaTabela', () => {
  it('considera cabeçalho, linhas e rodapé', () => {
    expect(calcularAlturaDaTabela(clientes)).toBe(
      34 + 1 * 26 + 10
    );
  });
});