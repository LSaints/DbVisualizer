import { act, render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import type { EsquemaDeBanco } from '../src/modelos/tipos';
import { PaginaDoDiagrama } from '../src/paginas/PaginaDoDiagrama';

const esquema: EsquemaDeBanco = {
  provedor: 'mysql',
  nomeDoBanco: 'erp',
  tabelas: [
    {
      esquema: 'erp',
      nome: 'clientes',
      tipoDaTabela: 'BASE TABLE',
      motor: 'InnoDB',
      colunas: [
        { nome: 'id', tipoDoDado: 'int', posicaoOrdinal: 1, podeSerNula: false, chavePrimaria: true, chaveEstrangeira: false, autoIncremento: true },
        { nome: 'nome', tipoDoDado: 'varchar', posicaoOrdinal: 2, podeSerNula: false, chavePrimaria: false, chaveEstrangeira: false, autoIncremento: false }
      ]
    },
    {
      esquema: 'erp',
      nome: 'pedidos',
      tipoDaTabela: 'BASE TABLE',
      motor: 'InnoDB',
      colunas: [
        { nome: 'id', tipoDoDado: 'int', posicaoOrdinal: 1, podeSerNula: false, chavePrimaria: true, chaveEstrangeira: false, autoIncremento: true },
        { nome: 'cliente_id', tipoDoDado: 'int', posicaoOrdinal: 2, podeSerNula: false, chavePrimaria: false, chaveEstrangeira: true, autoIncremento: false }
      ]
    },
    {
      esquema: 'erp',
      nome: 'pagamentos',
      tipoDaTabela: 'BASE TABLE',
      motor: 'InnoDB',
      colunas: [
        { nome: 'pedido_id', tipoDoDado: 'int', posicaoOrdinal: 1, podeSerNula: false, chavePrimaria: false, chaveEstrangeira: true, autoIncremento: false },
        { nome: 'valor', tipoDoDado: 'decimal', posicaoOrdinal: 2, podeSerNula: false, chavePrimaria: false, chaveEstrangeira: false, autoIncremento: false }
      ]
    }
  ],
  relacionamentos: [
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
      colunaDestino: 'id',
      nomeDaRestricao: 'fk_pagamentos_pedidos'
    }
  ]
};

function renderizaPagina(propriedades?: {
  temMaisTabelas?: boolean;
  carregandoMaisTabelas?: boolean;
  aoCarregarMaisTabelas?: () => void;
}): void {
  render(
    <PaginaDoDiagrama
      esquema={esquema}
      aoDesconectar={() => undefined}
      temMaisTabelas={propriedades?.temMaisTabelas ?? false}
      carregandoMaisTabelas={propriedades?.carregandoMaisTabelas ?? false}
      aoCarregarMaisTabelas={propriedades?.aoCarregarMaisTabelas ?? (() => undefined)}
    />
  );
}

/**
 * Seleciona uma tabela no diagrama. Dispara apenas o evento `click` (com
 * `view` válido) para acionar o `onNodeClick` do React Flow sem passar pelo
 * mousedown do d3-drag, que em jsdom tem `event.view === null`.
 */
function clicarNumaTabela(nome: string): void {
  const nodo = screen.getByText(nome).closest('.tabela-no-diagrama');
  expect(nodo).not.toBeNull();
  act(() => {
    nodo?.dispatchEvent(
      new MouseEvent('click', { bubbles: true, cancelable: true })
    );
  });
}

describe('PaginaDoDiagrama (cenários 4-8 do quickstart)', () => {
  it('cenário 4: renderiza as tabelas, suas colunas e indicadores de PK/FK', () => {
    renderizaPagina();

    const clientes = screen.getByText('clientes').closest('.tabela-no-diagrama');
    expect(clientes).not.toBeNull();
    expect(within(clientes as HTMLElement).getByText('id')).toBeInTheDocument();
    expect(
      within(clientes as HTMLElement).getByText((conteudo) => conteudo.includes('🔑'))
    ).toBeInTheDocument();
    expect(screen.getByText('pedidos')).toBeInTheDocument();
    expect(screen.getByText('pagamentos')).toBeInTheDocument();
  });

  it('cenário 5: busca por nome parcial filtra as tabelas em menos de 1s', async () => {
    renderizaPagina();

    const inicio = performance.now();
    await userEvent.type(screen.getByLabelText('Pesquisar tabela'), 'ped');
    const duracao = performance.now() - inicio;

    expect(duracao).toBeLessThan(1000);
    expect(screen.getByText('pedidos')).toBeInTheDocument();
    expect(screen.queryByText('clientes')).not.toBeInTheDocument();
  });

  it('cenário 6: oferece a reorganização automática (Dagre) sem quebrar relacionamentos', async () => {
    renderizaPagina();

    const organizar = screen.getByRole('button', { name: 'Organizar automaticamente' });
    await userEvent.click(organizar);

    expect(screen.getByText('clientes')).toBeInTheDocument();
    expect(screen.getByText('pedidos')).toBeInTheDocument();
    expect(screen.getByText('pagamentos')).toBeInTheDocument();
  });

  it('cenário 7: ocultar tabela some junto com seus edges e restaura pelo painel', async () => {
    renderizaPagina();

    clicarNumaTabela('pedidos');

    await userEvent.click(screen.getByRole('button', { name: 'Ocultar tabela' }));

    expect(screen.queryByText('pedidos')).not.toBeInTheDocument();

    await userEvent.click(screen.getByText('erp.pedidos'));

    expect(screen.getByText('pedidos')).toBeInTheDocument();
  });

  it('cenário 8: foco em relacionamentos mantém apenas a tabela e as vizinhas diretas; volta completa', async () => {
    renderizaPagina();

    clicarNumaTabela('pedidos');

    await userEvent.click(screen.getByRole('button', { name: 'Mostrar relacionamentos' }));

    expect(screen.getByText('pedidos')).toBeInTheDocument();
    expect(screen.getByText('clientes')).toBeInTheDocument();
    expect(screen.getByText('pagamentos')).toBeInTheDocument();

    await userEvent.click(screen.getByRole('button', { name: 'Voltar ao diagrama completo' }));

    expect(screen.getByText('clientes')).toBeInTheDocument();
    expect(screen.getByText('pagamentos')).toBeInTheDocument();
  });

  it('cenário 9: selecionar uma tabela destaca o node selecionado e atenua as demais; clique em área vazia limpa a seleção', () => {
    renderizaPagina();

    clicarNumaTabela('pedidos');

    const nodoPedidos = screen.getByText('pedidos').closest('.react-flow__node');
    const nodoPagamentos = screen.getByText('pagamentos').closest('.react-flow__node');

    expect(nodoPedidos).toHaveClass('tabela--selecionada');
    expect(nodoPagamentos).toHaveClass('tabela--atenuada');

    const painel = document.querySelector('.react-flow__pane');
    expect(painel).not.toBeNull();
    act(() => {
      painel?.dispatchEvent(
        new MouseEvent('click', { bubbles: true, cancelable: true })
      );
    });

    expect(screen.getByText('pedidos').closest('.react-flow__node')).not.toHaveClass(
      'tabela--selecionada'
    );
  });

  it('cenário 10: botão "carregar mais tabelas" só aparece quando há mais, e aciona o callback', async () => {
    renderizaPagina({ temMaisTabelas: false });
    expect(screen.queryByRole('button', { name: 'Carregar mais tabelas' })).not.toBeInTheDocument();

    const aoCarregarMaisTabelas = vi.fn();
    render(
      <PaginaDoDiagrama
        esquema={esquema}
        aoDesconectar={() => undefined}
        temMaisTabelas
        carregandoMaisTabelas={false}
        aoCarregarMaisTabelas={aoCarregarMaisTabelas}
      />
    );

    const botao = screen.getByRole('button', { name: 'Carregar mais tabelas' });
    await userEvent.click(botao);

    expect(aoCarregarMaisTabelas).toHaveBeenCalledTimes(1);
  });
});