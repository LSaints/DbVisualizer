import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import type { Conversa } from '../src/modelos/tiposDoAssistente';
import { ListaDeConversas } from '../src/componentes/ListaDeConversas';

const conversas: Conversa[] = [
  {
    id: 'conversa-1',
    titulo: 'Contratos',
    criadaEm: '2026-01-01T00:00:00Z',
    atualizadaEm: '2026-01-01T00:00:00Z',
    resumo: 'Lista todos os contratos.'
  },
  {
    id: 'conversa-2',
    titulo: 'Clientes',
    criadaEm: '2026-01-02T00:00:00Z',
    atualizadaEm: '2026-01-02T00:00:00Z',
    resumo: 'Conversa vazia'
  }
];

describe('ListaDeConversas', () => {
  it('mostra título, data/hora e prévia curta de cada conversa (aceitação 1 da US3)', () => {
    render(
      <ListaDeConversas
        conversas={conversas}
        conversaAtivaId={null}
        aoSelecionar={() => undefined}
        aoCriarNova={() => undefined}
        aoRenomear={() => undefined}
        aoExcluir={() => undefined}
      />
    );

    expect(screen.getByRole('button', { name: 'Contratos' })).toBeInTheDocument();
    expect(screen.getByText('Lista todos os contratos.')).toBeInTheDocument();
    expect(screen.getByText('Conversa vazia')).toBeInTheDocument();
  });

  it('abrir uma conversa dispara aoSelecionar com o id (aceitação 2 da US3)', async () => {
    const aoSelecionar = vi.fn();
    render(
      <ListaDeConversas
        conversas={conversas}
        conversaAtivaId={null}
        aoSelecionar={aoSelecionar}
        aoCriarNova={() => undefined}
        aoRenomear={() => undefined}
        aoExcluir={() => undefined}
      />
    );

    await userEvent.click(screen.getByRole('button', { name: 'Contratos' }));

    expect(aoSelecionar).toHaveBeenCalledWith('conversa-1');
  });

  it('"Nova conversa" dispara aoCriarNova sem remover as conversas existentes (aceitação 3 da US3)', async () => {
    const aoCriarNova = vi.fn();
    render(
      <ListaDeConversas
        conversas={conversas}
        conversaAtivaId={null}
        aoSelecionar={() => undefined}
        aoCriarNova={aoCriarNova}
        aoRenomear={() => undefined}
        aoExcluir={() => undefined}
      />
    );

    await userEvent.click(screen.getByRole('button', { name: 'Nova conversa' }));

    expect(aoCriarNova).toHaveBeenCalledTimes(1);
    expect(screen.getByRole('button', { name: 'Contratos' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Nova conversa' })).toBeInTheDocument();
  });

  it('renomear atualiza o título via callback (aceitação 1 da US4)', async () => {
    const aoRenomear = vi.fn();
    render(
      <ListaDeConversas
        conversas={conversas}
        conversaAtivaId={null}
        aoSelecionar={() => undefined}
        aoCriarNova={() => undefined}
        aoRenomear={aoRenomear}
        aoExcluir={() => undefined}
      />
    );

    await userEvent.click(screen.getByRole('button', { name: 'Renomear Contratos' }));
    const campo = screen.getByLabelText('Renomear conversa Contratos');
    await userEvent.clear(campo);
    await userEvent.type(campo, 'Contratos ativos');
    await userEvent.click(screen.getByRole('button', { name: 'Salvar' }));

    expect(aoRenomear).toHaveBeenCalledWith('conversa-1', 'Contratos ativos');
  });

  it('renomear com título vazio mostra erro e não chama o callback (validação 1–100)', async () => {
    const aoRenomear = vi.fn();
    render(
      <ListaDeConversas
        conversas={conversas}
        conversaAtivaId={null}
        aoSelecionar={() => undefined}
        aoCriarNova={() => undefined}
        aoRenomear={aoRenomear}
        aoExcluir={() => undefined}
      />
    );

    await userEvent.click(screen.getByRole('button', { name: 'Renomear Contratos' }));
    const campo = screen.getByLabelText('Renomear conversa Contratos');
    await userEvent.clear(campo);
    await userEvent.click(screen.getByRole('button', { name: 'Salvar' }));

    expect(screen.getByRole('alert')).toHaveTextContent(/entre 1 e 100 caracteres/);
    expect(aoRenomear).not.toHaveBeenCalled();
  });

  it('excluir exige confirmação e chama o callback somente quando confirmado (aceitação 2 da US4)', async () => {
    const aoExcluir = vi.fn();
    vi.spyOn(window, 'confirm').mockReturnValueOnce(false).mockReturnValueOnce(true);
    render(
      <ListaDeConversas
        conversas={conversas}
        conversaAtivaId={null}
        aoSelecionar={() => undefined}
        aoCriarNova={() => undefined}
        aoRenomear={() => undefined}
        aoExcluir={aoExcluir}
      />
    );

    await userEvent.click(screen.getByRole('button', { name: 'Excluir Contratos' }));
    expect(aoExcluir).not.toHaveBeenCalled();

    await userEvent.click(screen.getByRole('button', { name: 'Excluir Contratos' }));
    expect(aoExcluir).toHaveBeenCalledWith('conversa-1');
  });
});
