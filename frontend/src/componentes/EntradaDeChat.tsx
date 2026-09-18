import { useEffect, useRef, type FormEvent, type KeyboardEvent } from 'react';
import { SeletorDeProvedor } from './SeletorDeProvedor';
import type { ProvedorConfigurado } from '../modelos/tiposDoAssistente';

interface Propriedades {
  mensagem: string;
  aoAlterarMensagem: (mensagem: string) => void;
  aoEnviar: (evento: FormEvent) => void;
  podeEnviar: boolean;
  provedoresConfigurados: ProvedorConfigurado[];
  provedorSelecionado: string | null;
  aoSelecionarProvedor: (provedor: string) => void;
  aoAbrirConfiguracoes: () => void;
}

const ALTURA_MAXIMA_EM_PIXELS = 200;

/**
 * Campo de mensagem do assistente: textarea que cresce com o conteúdo
 * (até um limite) e botão de enviar circular embutido, no estilo de
 * assistentes de chat conhecidos. Os badges de provedor ficam acima da
 * caixa de texto (FR-008).
 */
export function EntradaDeChat({
  mensagem,
  aoAlterarMensagem,
  aoEnviar,
  podeEnviar,
  provedoresConfigurados,
  provedorSelecionado,
  aoSelecionarProvedor,
  aoAbrirConfiguracoes
}: Propriedades) {
  const textareaRef = useRef<HTMLTextAreaElement>(null);

  useEffect(() => {
    const textarea = textareaRef.current;
    if (textarea == null) {
      return;
    }

    textarea.style.height = 'auto';
    textarea.style.height = `${Math.min(textarea.scrollHeight, ALTURA_MAXIMA_EM_PIXELS)}px`;
  }, [mensagem]);

  function aoPressionarTecla(evento: KeyboardEvent<HTMLTextAreaElement>): void {
    if (evento.key === 'Enter' && !evento.shiftKey) {
      evento.preventDefault();
      if (podeEnviar) {
        aoEnviar(evento);
      }
    }
  }

  return (
    <form className="entrada-de-chat" onSubmit={aoEnviar}>
      <SeletorDeProvedor
        provedoresConfigurados={provedoresConfigurados}
        selecionado={provedorSelecionado}
        aoSelecionar={aoSelecionarProvedor}
        aoAbrirConfiguracoes={aoAbrirConfiguracoes}
      />

      <div className="caixa-de-mensagem">
        <textarea
          ref={textareaRef}
          className="campo-de-mensagem"
          placeholder="Descreva a consulta desejada..."
          aria-label="Mensagem"
          value={mensagem}
          onChange={(evento) => aoAlterarMensagem(evento.target.value)}
          onKeyDown={aoPressionarTecla}
          rows={1}
        />
        <button
          type="submit"
          className="botao-de-enviar"
          aria-label="Enviar"
          disabled={!podeEnviar}
        >
          <svg
            width="18"
            height="18"
            viewBox="0 0 24 24"
            fill="none"
            aria-hidden="true"
          >
            <path
              d="M12 19V5M12 5L6 11M12 5L18 11"
              stroke="currentColor"
              strokeWidth="2"
              strokeLinecap="round"
              strokeLinejoin="round"
            />
          </svg>
        </button>
      </div>
    </form>
  );
}
