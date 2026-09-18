import { useEffect, useState } from 'react';
import type { ProvedorConfigurado, ProvedorDeIaDisponivel } from '../modelos/tiposDoAssistente';

interface Propriedades {
  provedores: ProvedorDeIaDisponivel[];
  chavesPorProvedor: Record<string, string>;
  provedorPadrao: string | null;
  aoSalvarChave: (provedor: string, token: string) => void;
  aoRemover: (provedor: string) => void;
  aoDefinirPadrao: (provedor: string) => void;
  aoFechar: () => void;
}

/**
 * Modal de configuração dos provedores de IA (US1). Lista os provedores
 * suportados pela fábrica, permite adicionar/editar/remover o token de cada
 * um (somente em memória da sessão — FR-015) e definir o provedor padrão. O
 * token nunca é exibido por completo (FR-007): a lista mostra apenas
 * "Configurado"/"Não configurado" e o formulário usa `input type="password"`.
 */
export function ModalDeConfiguracaoDeProvedores({
  provedores,
  chavesPorProvedor,
  provedorPadrao,
  aoSalvarChave,
  aoRemover,
  aoDefinirPadrao,
  aoFechar
}: Propriedades) {
  const [provedorEmEdicao, setProvedorEmEdicao] = useState<string>(
    provedores[0]?.provedor ?? ''
  );
  const [tokenDigitado, setTokenDigitado] = useState('');
  const [aviso, setAviso] = useState('');

  useEffect(() => {
    function aoPressionarTecla(evento: KeyboardEvent): void {
      if (evento.key === 'Escape') {
        aoFechar();
      }
    }

    document.addEventListener('keydown', aoPressionarTecla);
    return () => document.removeEventListener('keydown', aoPressionarTecla);
  }, [aoFechar]);

  const provedoresConfigurados: ProvedorConfigurado[] = provedores.map((provedor) => ({
    provedor: provedor.provedor,
    rotulo: provedor.rotulo,
    tokenConfigurado: Boolean(chavesPorProvedor[provedor.provedor])
  }));

  function salvar(): void {
    if (tokenDigitado.trim() === '') {
      setAviso('Informe o token para salvar o provedor.');
      return;
    }

    aoSalvarChave(provedorEmEdicao, tokenDigitado.trim());
    setTokenDigitado('');
    setAviso('');
  }

  return (
    <div
      className="overlay-do-modal"
      role="presentation"
      onMouseDown={(evento) => {
        if (evento.target === evento.currentTarget) {
          aoFechar();
        }
      }}
    >
      <div
        className="modal-de-configuracao"
        role="dialog"
        aria-modal="true"
        aria-label="Configuração de provedores de IA"
      >
        <header className="cabecalho-do-modal">
          <h2>Configuração de provedores de IA</h2>
          <button
            type="button"
            className="botao-secundario"
            aria-label="Fechar"
            onClick={aoFechar}
          >
            Fechar
          </button>
        </header>

        <ul className="lista-de-provedores-configurados">
          {provedoresConfigurados.map((provedor) => (
            <li key={provedor.provedor} className="item-de-provedor-configurado">
              <span className="rotulo-do-provedor">{provedor.rotulo}</span>
              <span
                className={
                  provedor.tokenConfigurado
                    ? 'status-do-token status-do-token-configurado'
                    : 'status-do-token status-do-token-ausente'
                }
              >
                {provedor.tokenConfigurado ? 'Configurado' : 'Não configurado'}
              </span>
              {provedor.provedor === provedorPadrao && (
                <span className="rotulo-padrao">Padrão</span>
              )}
              <div className="acoes-do-provedor">
                <button
                  type="button"
                  className="botao-secundario"
                  onClick={() => {
                    setProvedorEmEdicao(provedor.provedor);
                    setTokenDigitado('');
                    setAviso('');
                  }}
                >
                  {provedor.tokenConfigurado ? 'Editar' : 'Adicionar'}
                </button>
                {provedor.tokenConfigurado && (
                  <>
                    <button
                      type="button"
                      className="botao-secundario"
                      onClick={() => aoDefinirPadrao(provedor.provedor)}
                      disabled={provedor.provedor === provedorPadrao}
                    >
                      Definir como padrão
                    </button>
                    <button
                      type="button"
                      className="botao-secundario"
                      onClick={() => aoRemover(provedor.provedor)}
                    >
                      Remover
                    </button>
                  </>
                )}
              </div>
            </li>
          ))}
        </ul>

        <form
          className="formulario-de-token"
          onSubmit={(evento) => {
            evento.preventDefault();
            salvar();
          }}
        >
          <label className="campo">
            <span className="campo-rotulo">Provedor</span>
            <select
              aria-label="Provedor a configurar"
              value={provedorEmEdicao}
              onChange={(evento) => {
                setProvedorEmEdicao(evento.target.value);
                setTokenDigitado('');
                setAviso('');
              }}
            >
              {provedores.map((provedor) => (
                <option key={provedor.provedor} value={provedor.provedor}>
                  {provedor.rotulo}
                </option>
              ))}
            </select>
          </label>

          <label className="campo">
            <span className="campo-rotulo">Token</span>
            <input
              type="password"
              aria-label="Token do provedor"
              value={tokenDigitado}
              onChange={(evento) => setTokenDigitado(evento.target.value)}
              placeholder="••••••••••••••••"
              autoComplete="off"
            />
          </label>

          {aviso !== '' && (
            <p className="alerta-de-aviso-sutil" role="alert">
              {aviso}
            </p>
          )}

          <button type="submit" className="botao-primario">
            Salvar
          </button>
        </form>
      </div>
    </div>
  );
}
