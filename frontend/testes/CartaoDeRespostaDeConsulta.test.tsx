import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { CartaoDeRespostaDeConsulta } from '../src/componentes/CartaoDeRespostaDeConsulta';
import type { RespostaDeConsultaDoAssistente } from '../src/modelos/tiposDoAssistente';

const respostaCompleta: RespostaDeConsultaDoAssistente = {
  consulta:
    'SELECT c.*, cl.* FROM contratos c INNER JOIN clientes cl ON cl.id = c.cliente_id WHERE c.status = \'X\';',
  explicacao: 'Lista os contratos com cliente e filtra pelo status X.',
  objetivo: 'Encontrar contratos com status X.',
  tabelas: [
    { nome: 'contratos', apelido: 'c', funcao: 'Tabela principal da consulta.' },
    { nome: 'clientes', apelido: 'cl', funcao: 'Dados dos clientes.' }
  ],
  relacionamentos: [
    {
      tabela_origem: 'contratos',
      campo_origem: 'cliente_id',
      tabela_destino: 'clientes',
      campo_destino: 'id',
      tipo: 'INNER JOIN',
      explicacao: 'Relaciona cada contrato ao seu cliente.'
    }
  ],
  filtros: [
    {
      campo: 'contratos.status',
      operador: '=',
      valor: 'X',
      explicacao: 'Somente contratos com status X.'
    }
  ],
  campos_retorno: { contratos: '*', clientes: '*', explicacao: 'Todos os campos.' },
  tipo_consulta: 'SELECT',
  resultado_esperado: 'Lista de contratos com clientes, com status X.',
  parametros: [
    { nome: 'status', valor: 'X', descricao: 'Status usado no filtro.' }
  ]
};

describe('CartaoDeRespostaDeConsulta', () => {
  beforeEach(() => {
    Object.defineProperty(navigator, 'clipboard', {
      value: { writeText: vi.fn().mockResolvedValue(undefined) },
      configurable: true
    });
  });

  it('destaca o SQL e exibe cada metadado na própria seção', () => {
    render(<CartaoDeRespostaDeConsulta resposta={respostaCompleta} />);

    expect(
      screen.getByRole('heading', { name: 'Consulta gerada' })
    ).toBeInTheDocument();
    expect(
      screen.getByText(/SELECT c\.\*, cl\.\* FROM contratos c/)
    ).toBeInTheDocument();
    expect(
      screen.getByRole('heading', { name: 'Explicação' })
    ).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Objetivo' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Tabelas' })).toBeInTheDocument();
    expect(
      screen.getByRole('heading', { name: 'Relacionamentos' })
    ).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Filtros' })).toBeInTheDocument();
    expect(
      screen.getByRole('heading', { name: 'Campos de retorno' })
    ).toBeInTheDocument();
    expect(
      screen.getByRole('heading', { name: 'Resultado esperado' })
    ).toBeInTheDocument();
    expect(
      screen.getByRole('heading', { name: 'Parâmetros' })
    ).toBeInTheDocument();
  });

  it('lista as tabelas em seção própria com nome, apelido e função', () => {
    render(<CartaoDeRespostaDeConsulta resposta={respostaCompleta} />);

    const tabela = obterTabelaDaSecao('Tabelas');
    expect(
      within(tabela).getByRole('columnheader', { name: 'Nome' })
    ).toBeInTheDocument();
    expect(
      within(tabela).getByRole('columnheader', { name: 'Apelido' })
    ).toBeInTheDocument();
    expect(
      within(tabela).getByRole('columnheader', { name: 'Função' })
    ).toBeInTheDocument();
    expect(within(tabela).getByText('contratos')).toBeInTheDocument();
    expect(within(tabela).getByText('c')).toBeInTheDocument();
    expect(
      within(tabela).getByText('Tabela principal da consulta.')
    ).toBeInTheDocument();
    expect(within(tabela).getByText('clientes')).toBeInTheDocument();
    expect(within(tabela).getByText('cl')).toBeInTheDocument();
  });

  it('exibe os relacionamentos com origem, destino e tipo de junção', () => {
    render(<CartaoDeRespostaDeConsulta resposta={respostaCompleta} />);

    const secao = screen.getByRole('region', { name: 'Relacionamentos' });
    const tabela = within(secao).getByRole('table');
    expect(within(tabela).getByText('contratos')).toBeInTheDocument();
    expect(within(tabela).getByText('cliente_id')).toBeInTheDocument();
    expect(within(tabela).getByText('clientes')).toBeInTheDocument();
    expect(within(tabela).getByText('id')).toBeInTheDocument();
    expect(within(tabela).getByText('INNER JOIN')).toBeInTheDocument();
    expect(
      within(secao).getByText('Relaciona cada contrato ao seu cliente.')
    ).toBeInTheDocument();
  });

  it('exibe filtros com campo, operador, valor e explicação', () => {
    render(<CartaoDeRespostaDeConsulta resposta={respostaCompleta} />);

    const tabela = obterTabelaDaSecao('Filtros');
    expect(within(tabela).getByText('contratos.status')).toBeInTheDocument();
    expect(within(tabela).getByText('=')).toBeInTheDocument();
    expect(within(tabela).getByText('X')).toBeInTheDocument();
    expect(
      within(tabela).getByText('Somente contratos com status X.')
    ).toBeInTheDocument();
  });

  it('exibe parâmetros com nome, valor e descrição', () => {
    render(<CartaoDeRespostaDeConsulta resposta={respostaCompleta} />);

    const tabela = obterTabelaDaSecao('Parâmetros');
    expect(within(tabela).getByText('status')).toBeInTheDocument();
    expect(within(tabela).getByText('X')).toBeInTheDocument();
    expect(
      within(tabela).getByText('Status usado no filtro.')
    ).toBeInTheDocument();
  });

  it('copia o SQL completo para a área de transferência com um clique', async () => {
    render(<CartaoDeRespostaDeConsulta resposta={respostaCompleta} />);

    await userEvent.click(screen.getByRole('button', { name: 'Copiar consulta' }));

    expect(navigator.clipboard.writeText).toHaveBeenCalledTimes(1);
    expect(navigator.clipboard.writeText).toHaveBeenCalledWith(
      respostaCompleta.consulta
    );
    await waitFor(() => {
      expect(screen.getByText('Copiado!')).toBeInTheDocument();
    });
  });
});

function obterTabelaDaSecao(rotuloDaSecao: string): HTMLElement {
  return within(screen.getByRole('region', { name: rotuloDaSecao })).getByRole(
    'table'
  );
}