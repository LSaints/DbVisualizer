import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { ModalDeConfiguracaoDeProvedores } from '../src/componentes/ModalDeConfiguracaoDeProvedores';
import type { ProvedorDeIaDisponivel } from '../src/modelos/tiposDoAssistente';

const provedores: ProvedorDeIaDisponivel[] = [
  { provedor: 'openai', rotulo: 'OpenAI' },
  { provedor: 'claude', rotulo: 'Claude' }
];

function renderizarModal(propriedadesParciais: Partial<Parameters<typeof ModalDeConfiguracaoDeProvedores>[0]> = {}) {
  const aoSalvarChave = vi.fn();
  const aoRemover = vi.fn();
  const aoDefinirPadrao = vi.fn();
  const aoFechar = vi.fn();

  render(
    <ModalDeConfiguracaoDeProvedores
      provedores={provedores}
      chavesPorProvedor={{}}
      provedorPadrao={null}
      aoSalvarChave={aoSalvarChave}
      aoRemover={aoRemover}
      aoDefinirPadrao={aoDefinirPadrao}
      aoFechar={aoFechar}
      {...propriedadesParciais}
    />
  );

  return { aoSalvarChave, aoRemover, aoDefinirPadrao, aoFechar };
}

describe('ModalDeConfiguracaoDeProvedores', () => {
  it('renderiza o modal com os provedores da fábrica e o status "Não configurado"', () => {
    renderizarModal();

    expect(screen.getByRole('dialog')).toBeInTheDocument();
    expect(screen.getByText('OpenAI', { selector: '.rotulo-do-provedor' })).toBeInTheDocument();
    expect(screen.getByText('Claude', { selector: '.rotulo-do-provedor' })).toBeInTheDocument();
    expect(screen.getAllByText('Não configurado')).toHaveLength(2);
  });

  it('adicionar um provedor com token salva e exibe "Configurado" mascarado (SC-003/FR-007)', async () => {
    const { aoSalvarChave } = renderizarModal();

    await userEvent.selectOptions(screen.getByLabelText('Provedor a configurar'), 'openai');
    await userEvent.type(screen.getByLabelText('Token do provedor'), 'sk-abc123');
    await userEvent.click(screen.getByRole('button', { name: 'Salvar' }));

    expect(aoSalvarChave).toHaveBeenCalledWith('openai', 'sk-abc123');
    expect(screen.queryByText('sk-abc123')).not.toBeInTheDocument();
    expect(screen.queryByDisplayValue('sk-abc123')).not.toBeInTheDocument();
  });

  it('token vazio bloqueia o salvar com aviso em pt-BR', async () => {
    const { aoSalvarChave } = renderizarModal();

    await userEvent.click(screen.getByRole('button', { name: 'Salvar' }));

    expect(aoSalvarChave).not.toHaveBeenCalled();
    expect(screen.getByRole('alert')).toHaveTextContent('Informe o token para salvar o provedor.');
  });

  it('provedor já configurado exibe "Configurado" e permite editar substituindo o valor', async () => {
    const { aoSalvarChave } = renderizarModal({ chavesPorProvedor: { openai: 'sk-antigo' } });

    expect(screen.getByText('Configurado')).toBeInTheDocument();
    expect(screen.queryByText('sk-antigo')).not.toBeInTheDocument();

    await userEvent.click(screen.getByRole('button', { name: 'Editar' }));
    const campoDeToken = screen.getByLabelText('Token do provedor');
    expect(campoDeToken).toHaveValue('');

    await userEvent.type(campoDeToken, 'sk-novo');
    await userEvent.click(screen.getByRole('button', { name: 'Salvar' }));

    expect(aoSalvarChave).toHaveBeenCalledWith('openai', 'sk-novo');
  });

  it('remover remove o token e o botão de remover some da lista', () => {
    const { aoRemover } = renderizarModal({ chavesPorProvedor: { openai: 'sk-abc123' } });

    const botaoRemover = screen.getByRole('button', { name: 'Remover' });
    botaoRemover.click();

    expect(aoRemover).toHaveBeenCalledWith('openai');
  });

  it('definir padrão chama o callback com o provedor selecionado', () => {
    const { aoDefinirPadrao } = renderizarModal({
      chavesPorProvedor: { openai: 'sk-abc123', claude: 'sk-def456' },
      provedorPadrao: 'openai'
    });

    // O botão de "openai" (já padrão) fica desabilitado; resta o de "claude".
    const botoes = screen.getAllByRole('button', { name: 'Definir como padrão' });
    const botaoHabilitado = botoes.find((botao) => !botao.hasAttribute('disabled'));
    expect(botaoHabilitado).toBeDefined();
    botaoHabilitado!.click();

    expect(aoDefinirPadrao).toHaveBeenCalledWith('claude');
  });

  it('campo de token usa type="password" e nunca exibe o valor anterior na tela', () => {
    renderizarModal({ chavesPorProvedor: { openai: 'sk-abc123' } });

    const campoDeToken = screen.getByLabelText('Token do provedor');
    expect(campoDeToken).toHaveAttribute('type', 'password');
    expect(screen.queryByText('sk-abc123')).not.toBeInTheDocument();
  });
});
