import type { ProvedorDeIaDisponivel } from '../modelos/tiposDoAssistente';

interface Propriedades {
  provedores: ProvedorDeIaDisponivel[];
  provedorDeIa: string;
  chaveDeApi: string;
  aoAlterarProvedor: (provedor: string) => void;
  aoAlterarChave: (chave: string) => void;
}

/**
 * Configuração da sessão de IA: seleção do provedor (rótulos em pt-BR) e campo
 * da chave de API do tipo senha, mantida somente em memória durante a sessão
 * (FR-002/FR-003). A chave nunca é gravada, registrada ou exibida após
 * configurada — proposta apenas por requisição ao backend (SC-002).
 */
export function ConfiguracaoDoAssistente({
  provedores,
  provedorDeIa,
  chaveDeApi,
  aoAlterarProvedor,
  aoAlterarChave
}: Propriedades) {
  return (
    <div className="configuracao-do-assistente">
      <label className="campo">
        <span className="campo-rotulo">Provedor de IA</span>
        <select
          aria-label="Provedor de IA"
          value={provedorDeIa}
          onChange={(evento) => aoAlterarProvedor(evento.target.value)}
        >
          {provedores.length === 0 && <option value="openai">OpenAI</option>}
          {provedores.map((provedor) => (
            <option key={provedor.provedor} value={provedor.provedor}>
              {provedor.rotulo}
            </option>
          ))}
        </select>
      </label>

      <label className="campo">
        <span className="campo-rotulo">Chave de API</span>
        <input
          type="password"
          aria-label="Chave de API"
          value={chaveDeApi}
          onChange={(evento) => aoAlterarChave(evento.target.value)}
          placeholder="••••••••••••••••"
          autoComplete="off"
        />
      </label>
    </div>
  );
}