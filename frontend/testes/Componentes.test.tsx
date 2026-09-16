import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { BarraDePesquisa } from '../src/componentes/BarraDePesquisa';
import { FormularioDeConexao } from '../src/componentes/FormularioDeConexao';

describe('BarraDePesquisa', () => {
  it('chama aoAlterar ao digitar', async () => {
    const aoAlterar = vi.fn();
    render(<BarraDePesquisa valor="" aoAlterar={aoAlterar} />);

    await userEvent.type(screen.getByLabelText('Pesquisar tabela'), 'cliente');

    expect(aoAlterar).toHaveBeenCalledWith('c');
  });
});

describe('FormularioDeConexao', () => {
  it('submete a conexão com a porta padrão 3306', async () => {
    const aoSubmeter = vi.fn();
    render(<FormularioDeConexao aoSubmeter={aoSubmeter} />);

    await userEvent.type(screen.getByPlaceholderText('erp'), 'erp');
    const usuario = screen.getByPlaceholderText('readonly');
    await userEvent.clear(usuario);
    await userEvent.type(usuario, 'readonly');
    await userEvent.type(screen.getByPlaceholderText('••••••••'), 'segredo');
    await userEvent.click(screen.getByRole('button', { name: 'Testar conexão' }));

    expect(aoSubmeter).toHaveBeenCalledWith({
      provedor: 'mysql',
      host: 'localhost',
      porta: 3306,
      bancoDeDados: 'erp',
      usuario: 'readonly',
      senha: 'segredo'
    });
  });

  it('omite a porta quando o campo está vazio', async () => {
    const aoSubmeter = vi.fn();
    render(<FormularioDeConexao aoSubmeter={aoSubmeter} />);

    await userEvent.clear(screen.getByPlaceholderText('3306'));
    await userEvent.type(screen.getByPlaceholderText('erp'), 'erp');
    const usuario = screen.getByPlaceholderText('readonly');
    await userEvent.clear(usuario);
    await userEvent.type(usuario, 'readonly');
    await userEvent.type(screen.getByPlaceholderText('••••••••'), 'segredo');
    await userEvent.click(screen.getByRole('button', { name: 'Testar conexão' }));

    expect(aoSubmeter).toHaveBeenCalledWith(
      expect.objectContaining({ porta: undefined })
    );
  });
});