import type { ProvedorConfigurado } from '../modelos/tiposDoAssistente';

interface Propriedades {
  provedoresConfigurados: ProvedorConfigurado[];
  selecionado: string | null;
  aoSelecionar: (provedor: string) => void;
  aoAbrirConfiguracoes: () => void;
}

/**
 * Seletor de provedor por badges no rodapé do chat (FR-008/009/010). Um badge
 * por provedor com token configurado na sessão; o selecionado destaca com
 * `aria-pressed`. Sem provedor configurado, exibe uma dica clicável que abre
 * o modal de configurações (FR-014). Suporta até 8 provedores sem quebrar o
 * layout (flex-wrap — SC-005).
 */
export function SeletorDeProvedor({
  provedoresConfigurados,
  selecionado,
  aoSelecionar,
  aoAbrirConfiguracoes
}: Propriedades) {
  return (
    <div className="seletor-de-provedor" role="group" aria-label="Provedor de IA">
      {provedoresConfigurados.length === 0 && (
        <button
          type="button"
          className="badge-de-provedor badge-de-provedor-configurar"
          onClick={aoAbrirConfiguracoes}
        >
          Configurar provedor
        </button>
      )}
      {provedoresConfigurados.map((provedor) => (
        <button
          key={provedor.provedor}
          type="button"
          className={
            provedor.provedor === selecionado
              ? 'badge-de-provedor badge-de-provedor-ativa'
              : 'badge-de-provedor'
          }
          aria-pressed={provedor.provedor === selecionado}
          onClick={() => aoSelecionar(provedor.provedor)}
        >
          {provedor.rotulo}
        </button>
      ))}
      <button
        type="button"
        className="botao-de-configuracoes"
        aria-label="Configurações"
        title="Configurações"
        onClick={aoAbrirConfiguracoes}
      >
        <svg
          width="14"
          height="14"
          viewBox="0 0 24 24"
          fill="none"
          aria-hidden="true"
        >
          <path
            d="M12 15a3 3 0 1 0 0-6 3 3 0 0 0 0 6Z"
            stroke="currentColor"
            strokeWidth="2"
          />
          <path
            d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 1 1-2.83 2.83l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 1 1-4 0v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 1 1-2.83-2.83l.06-.06A1.65 1.65 0 0 0 4.68 15a1.65 1.65 0 0 0-1.51-1H3a2 2 0 1 1 0-4h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 1 1 2.83-2.83l.06.06A1.65 1.65 0 0 0 9 4.68a1.65 1.65 0 0 0 1-1.51V3a2 2 0 1 1 4 0v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 1 1 2.83 2.83l-.06.06A1.65 1.65 0 0 0 19.32 9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 1 1 0 4h-.09a1.65 1.65 0 0 0-1.51 1Z"
            stroke="currentColor"
            strokeWidth="2"
          />
        </svg>
      </button>
    </div>
  );
}
