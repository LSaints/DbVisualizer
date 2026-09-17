import { useState } from 'react';
import type { Conversa } from '../modelos/tiposDoAssistente';

interface Propriedades {
  conversas: Conversa[];
  conversaAtivaId: string | null;
  aoSelecionar: (id: string) => void;
  aoCriarNova: () => void;
  aoRenomear: (id: string, titulo: string) => void;
  aoExcluir: (id: string) => void;
}

/**
 * Lista de navegação das conversas persistidas (FR-004/FR-009): título,
 * data/hora da última atualização e prévia curta (`resumo`, "Conversa vazia"
 * quando ainda sem mensagens). Permite abrir, renomear (US4/D6) e excluir
 * (com confirmação em pt-BR) cada conversa, além de iniciar uma nova sem
 * apagar as anteriores.
 */
export function ListaDeConversas({
  conversas,
  conversaAtivaId,
  aoSelecionar,
  aoCriarNova,
  aoRenomear,
  aoExcluir
}: Propriedades) {
  const [idEmEdicao, setIdEmEdicao] = useState<string | null>(null);
  const [tituloEmEdicao, setTituloEmEdicao] = useState('');
  const [erroDeTitulo, setErroDeTitulo] = useState('');

  function iniciarRenomeacao(conversa: Conversa): void {
    setIdEmEdicao(conversa.id);
    setTituloEmEdicao(conversa.titulo);
    setErroDeTitulo('');
  }

  function confirmarRenomeacao(id: string): void {
    const titulo = tituloEmEdicao.trim();
    if (titulo.length < 1 || titulo.length > 100) {
      setErroDeTitulo('Informe um título entre 1 e 100 caracteres.');
      return;
    }

    aoRenomear(id, titulo);
    setIdEmEdicao(null);
    setErroDeTitulo('');
  }

  function confirmarExclusao(conversa: Conversa): void {
    const confirmado = window.confirm(
      `Excluir a conversa "${conversa.titulo}"? Esta ação não pode ser desfeita.`
    );
    if (confirmado) {
      aoExcluir(conversa.id);
    }
  }

  return (
    <nav className="lista-de-conversas" aria-label="Conversas">
      <button type="button" className="botao-primario" onClick={aoCriarNova}>
        Nova conversa
      </button>

      <ul>
        {conversas.map((conversa) => (
          <li
            key={conversa.id}
            className={`item-de-conversa${conversa.id === conversaAtivaId ? ' item-de-conversa-ativa' : ''}`}
          >
            {idEmEdicao === conversa.id ? (
              <div className="edicao-de-titulo">
                <input
                  aria-label={`Renomear conversa ${conversa.titulo}`}
                  value={tituloEmEdicao}
                  onChange={(evento) => setTituloEmEdicao(evento.target.value)}
                  onKeyDown={(evento) => {
                    if (evento.key === 'Enter') {
                      confirmarRenomeacao(conversa.id);
                    } else if (evento.key === 'Escape') {
                      setIdEmEdicao(null);
                    }
                  }}
                />
                <button type="button" onClick={() => confirmarRenomeacao(conversa.id)}>
                  Salvar
                </button>
                <button type="button" onClick={() => setIdEmEdicao(null)}>
                  Cancelar
                </button>
                {erroDeTitulo && (
                  <span role="alert" className="erro-de-titulo">
                    {erroDeTitulo}
                  </span>
                )}
              </div>
            ) : (
              <>
                <button
                  type="button"
                  className="botao-de-conversa"
                  aria-label={conversa.titulo}
                  onClick={() => aoSelecionar(conversa.id)}
                >
                  <strong>{conversa.titulo}</strong>
                  <time dateTime={conversa.atualizadaEm}>
                    {new Date(conversa.atualizadaEm).toLocaleString('pt-BR')}
                  </time>
                  <span className="resumo-da-conversa">{conversa.resumo}</span>
                </button>
                <button
                  type="button"
                  aria-label={`Renomear ${conversa.titulo}`}
                  onClick={() => iniciarRenomeacao(conversa)}
                >
                  Renomear
                </button>
                <button
                  type="button"
                  aria-label={`Excluir ${conversa.titulo}`}
                  onClick={() => confirmarExclusao(conversa)}
                >
                  Excluir
                </button>
              </>
            )}
          </li>
        ))}
      </ul>
    </nav>
  );
}
