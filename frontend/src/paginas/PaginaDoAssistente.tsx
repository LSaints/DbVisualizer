import { useEffect, useRef, useState, type FormEvent } from 'react';
import { CartaoDeRespostaDeConsulta } from '../componentes/CartaoDeRespostaDeConsulta';
import { ConfiguracaoDoAssistente } from '../componentes/ConfiguracaoDoAssistente';
import { ListaDeConversas } from '../componentes/ListaDeConversas';
import type { EsquemaDeBanco } from '../modelos/tipos';
import type {
  Conversa,
  IdentidadeDeBanco,
  ProvedorDeIaDisponivel,
  RespostaDeConsultaDoAssistente
} from '../modelos/tiposDoAssistente';
import {
  criarConversa,
  excluirConversa,
  gerarConsulta,
  listarConversas,
  listarProvedores,
  obterConversa,
  renomearConversa
} from '../servicos/ApiAssistente';

type EstadoDoChat = 'pronto' | 'gerando' | 'sucesso' | 'erro';

interface MensagemDoChat {
  id: number;
  remetente: 'usuario' | 'assistente';
  texto?: string;
  resposta?: RespostaDeConsultaDoAssistente;
}

interface Propriedades {
  contextoDeBanco?: EsquemaDeBanco | null;
  aoVoltar: () => void;
}

/**
 * Chat do assistente (FR-004/FR-006/FR-011/FR-013/FR-017). Mantém o histórico
 * completo da sessão em estado React (pedidos + respostas estruturadas), na
 * ordem, com rolagem automática até a troca mais recente e releitura de
 * respostas antigas (cada cartão é copiável). A lista de conversas persistidas
 * permite abrir, renomear e excluir conversas, e trocar entre elas sem perder
 * o estado da atual (US2/US3/US4). Um follow-up sobre a mesma conversa
 * (`conversaId`) envia o histórico como contexto ao provedor; quando o header
 * `X-Contexto-Truncado` vem `true`, um aviso sutil informa que somente as
 * mensagens mais recentes foram usadas (US5). A chave de API é usada somente
 * em memória, por requisição, e nunca aparece em tela após configurada.
 */
export function PaginaDoAssistente({ contextoDeBanco, aoVoltar }: Propriedades) {
  const [provedores, setProvedores] = useState<ProvedorDeIaDisponivel[]>([]);
  const [provedorDeIa, setProvedorDeIa] = useState('openai');
  const [chaveDeApi, setChaveDeApi] = useState('');
  const [mensagem, setMensagem] = useState('');
  const [estado, setEstado] = useState<EstadoDoChat>('pronto');
  const [mensagens, setMensagens] = useState<MensagemDoChat[]>([]);
  const [mensagemDeErro, setMensagemDeErro] = useState('');
  const [conversas, setConversas] = useState<Conversa[]>([]);
  const [conversaId, setConversaId] = useState<string | null>(null);
  const [contextoDaConversa, setContextoDaConversa] = useState<IdentidadeDeBanco | null>(null);
  const [contextoTruncado, setContextoTruncado] = useState(false);
  const historicoRef = useRef<HTMLDivElement>(null);
  const controladorRef = useRef<AbortController | null>(null);
  const proximoIdRef = useRef(1);

  useEffect(() => {
    listarProvedores()
      .then((provedoresDisponiveis) => {
        setProvedores(provedoresDisponiveis);
        setProvedorDeIa((atual) =>
          provedoresDisponiveis.some((provedor) => provedor.provedor === atual)
            ? atual
            : (provedoresDisponiveis[0]?.provedor ?? atual)
        );
      })
      .catch(() => {
        setProvedores([]);
      });

    listarConversas()
      .then(setConversas)
      .catch(() => setConversas([]));
  }, []);

  useEffect(() => () => controladorRef.current?.abort(), []);

  useEffect(() => {
    if (historicoRef.current) {
      historicoRef.current.scrollTop = historicoRef.current.scrollHeight;
    }
  }, [mensagens, estado]);

  function proximoId(): number {
    const id = proximoIdRef.current;
    proximoIdRef.current += 1;
    return id;
  }

  function atualizarListaDeConversas(): void {
    listarConversas()
      .then(setConversas)
      .catch(() => undefined);
  }

  function resetarChatLocal(): void {
    controladorRef.current?.abort();
    controladorRef.current = null;
    proximoIdRef.current = 1;
    setMensagens([]);
    setEstado('pronto');
    setMensagemDeErro('');
    setMensagem('');
    setContextoTruncado(false);
  }

  function iniciarNovaConversa(): void {
    resetarChatLocal();
    setConversaId(null);
    setContextoDaConversa(null);

    criarConversa()
      .then((conversa) => {
        setConversaId(conversa.id);
        atualizarListaDeConversas();
      })
      .catch(() => undefined);
  }

  async function selecionarConversa(id: string): Promise<void> {
    resetarChatLocal();
    setConversaId(id);

    try {
      const detalhada = await obterConversa(id);
      setContextoDaConversa(detalhada.contextoDeBanco ?? null);
      setMensagens(
        detalhada.mensagens.map((mensagemDaConversa) => ({
          id: proximoId(),
          remetente: mensagemDaConversa.papel,
          texto:
            typeof mensagemDaConversa.conteudo === 'string'
              ? mensagemDaConversa.conteudo
              : undefined,
          resposta:
            typeof mensagemDaConversa.conteudo === 'string'
              ? undefined
              : mensagemDaConversa.conteudo
        }))
      );
    } catch {
      setMensagemDeErro('Não foi possível abrir a conversa.');
      setEstado('erro');
    }
  }

  function renomearConversaAtiva(id: string, titulo: string): void {
    renomearConversa(id, titulo)
      .then(atualizarListaDeConversas)
      .catch(() => undefined);
  }

  function excluirConversaAtiva(id: string): void {
    excluirConversa(id)
      .then(() => {
        if (id === conversaId) {
          resetarChatLocal();
          setConversaId(null);
          setContextoDaConversa(null);
        }
        atualizarListaDeConversas();
      })
      .catch(() => undefined);
  }

  function cancelarGeracao(): void {
    controladorRef.current?.abort();
    controladorRef.current = null;
    setEstado('pronto');
  }

  const podeEnviar =
    contextoDeBanco != null &&
    estado !== 'gerando' &&
    chaveDeApi.trim() !== '' &&
    mensagem.trim().length >= 3;

  const provedorAtivo = provedores.find(
    (provedor) => provedor.provedor === provedorDeIa
  );

  const bancoDivergente =
    contextoDaConversa != null &&
    contextoDeBanco != null &&
    (contextoDaConversa.provedor.toLowerCase() !== contextoDeBanco.provedor.toLowerCase() ||
      contextoDaConversa.nomeDoBanco.toLowerCase() !== contextoDeBanco.nomeDoBanco.toLowerCase());

  async function enviar(evento?: FormEvent): Promise<void> {
    evento?.preventDefault();

    const texto = mensagem.trim();
    if (
      contextoDeBanco == null ||
      texto.length < 3 ||
      chaveDeApi.trim() === '' ||
      estado === 'gerando'
    ) {
      return;
    }

    controladorRef.current?.abort();
    const controlador = new AbortController();
    controladorRef.current = controlador;

    setMensagemDeErro('');
    setEstado('gerando');
    setMensagens((anteriores) => [
      ...anteriores,
      { id: proximoId(), remetente: 'usuario', texto }
    ]);
    setMensagem('');

    try {
      // Sem uma conversa ativa, cria uma automaticamente para que o
      // histórico da sessão seja persistido e enviado como contexto nas
      // trocas seguintes (evita perda de contexto quando o usuário não
      // clica em "Nova conversa" antes de digitar).
      let conversaIdAtiva = conversaId;
      if (conversaIdAtiva == null) {
        const conversa = await criarConversa();
        conversaIdAtiva = conversa.id;
        setConversaId(conversa.id);
        atualizarListaDeConversas();
      }

      const { resposta, contextoTruncado: truncado } = await gerarConsulta(
        {
          provedorDeIa,
          chaveDeApi,
          mensagem: texto,
          contextoDeBanco,
          conversaId: conversaIdAtiva
        },
        controlador.signal
      );

      if (controlador.signal.aborted) {
        return;
      }

      setMensagens((anteriores) => [
        ...anteriores,
        { id: proximoId(), remetente: 'assistente', resposta }
      ]);
      setContextoTruncado(truncado);
      setEstado('sucesso');

      setContextoDaConversa({
        provedor: contextoDeBanco.provedor,
        nomeDoBanco: contextoDeBanco.nomeDoBanco,
        versao: contextoDeBanco.versao ?? undefined
      });
      atualizarListaDeConversas();
    } catch (falha) {
      if (controlador.signal.aborted) {
        return;
      }

      setMensagemDeErro(
        falha instanceof Error
          ? falha.message
          : 'Não foi possível gerar a consulta.'
      );
      setEstado('erro');
    }
  }

  return (
    <div className="pagina-do-assistente">
      <header className="barra-superior">
        <button className="botao-secundario" onClick={aoVoltar}>
          Voltar ao diagrama
        </button>
        <h2 className="titulo-do-assistente">Assistente de IA</h2>
        <span className="provedor-ativo" title="Provedor ativo no chat">
          {provedorAtivo?.rotulo ?? provedorDeIa}
        </span>
        <span
          className={`contexto-ativo${contextoDeBanco == null ? ' contexto-ausente' : ''}`}
          title={
            contextoDeBanco == null
              ? 'Nenhum banco conectado'
              : 'Contexto de banco ativo'
          }
        >
          {contextoDeBanco == null
            ? 'Nenhum banco conectado'
            : `${contextoDeBanco.nomeDoBanco} · ${contextoDeBanco.provedor}${contextoDeBanco.versao ? ` · ${contextoDeBanco.versao}` : ''}`}
        </span>
      </header>

      <div className="corpo-do-assistente">
        <ListaDeConversas
          conversas={conversas}
          conversaAtivaId={conversaId}
          aoSelecionar={(id) => {
            void selecionarConversa(id);
          }}
          aoCriarNova={iniciarNovaConversa}
          aoRenomear={renomearConversaAtiva}
          aoExcluir={excluirConversaAtiva}
        />

        <div className="conteudo-do-chat">
          <ConfiguracaoDoAssistente
            provedores={provedores}
            provedorDeIa={provedorDeIa}
            chaveDeApi={chaveDeApi}
            aoAlterarProvedor={setProvedorDeIa}
            aoAlterarChave={setChaveDeApi}
          />

          {contextoDeBanco == null && (
            <div className="alerta-de-aviso" role="alert">
              <strong>Nenhum banco conectado.</strong>
              <span>
                {' '}
                Conecte um banco e carregue o schema para gerar consultas.
              </span>
              <button
                type="button"
                className="botao-secundario"
                onClick={aoVoltar}
              >
                Conectar ao banco
              </button>
            </div>
          )}

          {bancoDivergente && (
            <div className="alerta-de-aviso-sutil" role="status">
              O banco conectado atualmente difere do registrado nesta
              conversa. O histórico continua disponível para consulta.
            </div>
          )}

          <div className="historico-do-chat" aria-live="polite" ref={historicoRef}>
            {mensagens.map((mensagemDoChat) => (
              <div key={mensagemDoChat.id} className={`mensagem mensagem-${mensagemDoChat.remetente}`}>
                {mensagemDoChat.remetente === 'usuario' && <p>{mensagemDoChat.texto}</p>}
                {mensagemDoChat.remetente === 'assistente' && mensagemDoChat.resposta && (
                  <>
                    <CartaoDeRespostaDeConsulta resposta={mensagemDoChat.resposta} />
                    {mensagemDoChat.resposta.tipo_consulta !== 'SELECT' && (
                      <div className="alerta-de-aviso" role="alert">
                        Somente consultas de leitura (SELECT) são geradas pelo
                        assistente. Revise o pedido e tente novamente.
                      </div>
                    )}
                  </>
                )}
              </div>
            ))}

            {estado === 'gerando' && (
              <div className="linha-de-progresso">
                <p className="estado-progresso" role="status">
                  Gerando resposta...
                </p>
                <button
                  type="button"
                  className="botao-secundario"
                  aria-label="Cancelar geração"
                  onClick={cancelarGeracao}
                >
                  Cancelar
                </button>
              </div>
            )}

            {estado === 'erro' && (
              <div className="alerta-de-erro" role="alert">
                {mensagemDeErro}
              </div>
            )}

            {estado === 'sucesso' && contextoTruncado && (
              <p className="aviso-de-contexto-truncado" role="status">
                Somente as mensagens mais recentes desta conversa foram usadas
                como contexto.
              </p>
            )}
          </div>

          <form className="entrada-do-chat" onSubmit={enviar}>
            <textarea
              placeholder="Descreva a consulta desejada..."
              aria-label="Mensagem"
              value={mensagem}
              onChange={(evento) => setMensagem(evento.target.value)}
              rows={3}
            />
            <button type="submit" className="botao-primario" disabled={!podeEnviar}>
              Enviar
            </button>
          </form>
        </div>
      </div>
    </div>
  );
}
