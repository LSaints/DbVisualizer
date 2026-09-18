import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { SeletorDeProvedor } from '../src/componentes/SeletorDeProvedor';
import type { ProvedorConfigurado } from '../src/modelos/tiposDoAssistente';

describe('SeletorDeProvedor', () => {
  it('renderiza um badge por provedor configurado', () => {
    const provedoresConfigurados: ProvedorConfigurado[] = [
      { provedor: 'openai', rotulo: 'OpenAI', tokenConfigurado: true },
      { provedor: 'claude', rotulo: 'Claude', tokenConfigurado: true }
    ];

    render(
      <SeletorDeProvedor
        provedoresConfigurados={provedoresConfigurados}
        selecionado="openai"
        aoSelecionar={vi.fn()}
        aoAbrirConfiguracoes={vi.fn()}
      />
    );

    expect(screen.getByRole('group', { name: 'Provedor de IA' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'OpenAI' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Claude' })).toBeInTheDocument();
  });

  it('clique seleciona e marca aria-pressed=true no badge ativo', async () => {
    const aoSelecionar = vi.fn();
    const provedoresConfigurados: ProvedorConfigurado[] = [
      { provedor: 'openai', rotulo: 'OpenAI', tokenConfigurado: true },
      { provedor: 'claude', rotulo: 'Claude', tokenConfigurado: true }
    ];

    render(
      <SeletorDeProvedor
        provedoresConfigurados={provedoresConfigurados}
        selecionado="openai"
        aoSelecionar={aoSelecionar}
        aoAbrirConfiguracoes={vi.fn()}
      />
    );

    expect(screen.getByRole('button', { name: 'OpenAI' })).toHaveAttribute('aria-pressed', 'true');
    expect(screen.getByRole('button', { name: 'Claude' })).toHaveAttribute('aria-pressed', 'false');

    await userEvent.click(screen.getByRole('button', { name: 'Claude' }));

    expect(aoSelecionar).toHaveBeenCalledWith('claude');
  });

  it('estado visual distinto: badge ativo recebe a classe badge-de-provedor-ativa', () => {
    const provedoresConfigurados: ProvedorConfigurado[] = [
      { provedor: 'openai', rotulo: 'OpenAI', tokenConfigurado: true }
    ];

    render(
      <SeletorDeProvedor
        provedoresConfigurados={provedoresConfigurados}
        selecionado="openai"
        aoSelecionar={vi.fn()}
        aoAbrirConfiguracoes={vi.fn()}
      />
    );

    expect(screen.getByRole('button', { name: 'OpenAI' }).className).toContain('badge-de-provedor-ativa');
  });

  it('com 8 provedores todos permanecem clicáveis (SC-005)', () => {
    const provedoresConfigurados: ProvedorConfigurado[] = Array.from({ length: 8 }, (_, indice) => ({
      provedor: `provedor-${indice}`,
      rotulo: `Provedor ${indice}`,
      tokenConfigurado: true
    }));

    render(
      <SeletorDeProvedor
        provedoresConfigurados={provedoresConfigurados}
        selecionado="provedor-0"
        aoSelecionar={vi.fn()}
        aoAbrirConfiguracoes={vi.fn()}
      />
    );

    const botoes = screen.getAllByRole('button');
    expect(botoes).toHaveLength(9); // 8 badges + botão de configurações
    botoes.forEach((botao) => expect(botao).toBeEnabled());
  });

  it('caso vazio exibe estado "configurar" sem quebrar o layout', async () => {
    const aoAbrirConfiguracoes = vi.fn();

    render(
      <SeletorDeProvedor
        provedoresConfigurados={[]}
        selecionado={null}
        aoSelecionar={vi.fn()}
        aoAbrirConfiguracoes={aoAbrirConfiguracoes}
      />
    );

    const botaoConfigurar = screen.getByRole('button', { name: 'Configurar provedor' });
    expect(botaoConfigurar).toBeInTheDocument();

    await userEvent.click(botaoConfigurar);
    expect(aoAbrirConfiguracoes).toHaveBeenCalled();
  });
});
