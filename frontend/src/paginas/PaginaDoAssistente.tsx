import { useEffect, useRef, useState, type FormEvent } from 'react';
import { CartaoDeRespostaDeConsulta } from '../componentes/CartaoDeRespostaDeConsulta';
import { ListaDeConversas } from '../componentes/ListaDeConversas';
import { ModalDeConfiguracaoDeProvedores } from '../componentes/ModalDeConfiguracaoDeProvedores';
import { EntradaDeChat } from '../componentes/EntradaDeChat';
import type { EsquemaDeBanco } from '../modelos/tipos';
import type {
  Conversa,
  IdentidadeDeBanco,
  ProvedorConfigurado,
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
 * mensagens mais recentes foram usadas (US5).
 *
 * Os provedores de IA e seus tokens são configurados em um modal
 * (`ModalDeConfiguracaoDeProvedores` — US1) e vivem somente em memória da
 * sessão (`chavesPorProvedor` + `provedorPadrao` — constituição III/FR-015).
 * O rodapé do chat exibe um `SeletorDeProvedor` (badges) para trocar o
 * provedor ativo sem abrir configurações (US2); o provedor ativo é associado
 * à conversa e restaurado ao reabri-la (US3).
 */
export function PaginaDoAssistente({ contextoDeBanco, aoVoltar }: Propriedades) {
  const [provedores, setProvedores] = useState<ProvedorDeIaDisponivel[]>([]);
  const [chavesPorProvedor, setChavesPorProvedor] = useState<Record<string, string>>({});
  const [provedorPadrao, setProvedorPadrao] = useState<string | null>(null);
  const [provedorSelecionado, setProvedorSelecionado] = useState<string | null>(null);
  const [modalAberto, setModalAberto] = useState(false);
  const [mensagem, setMensagem] = useState('');
  const [estado, setEstado] = useState<EstadoDoChat>('pronto');
  const [mensagens, setMensagens] = useState<MensagemDoChat[]>([]);
  const [mensagemDeErro, setMensagemDeErro] = useState('');
  const [avisoDeConfiguracao, setAvisoDeConfiguracao] = useState('');
  const [conversas, setConversas] = useState<Conversa[]>([]);
  const [conversaId, setConversaId] = useState<string | null>(null);
  const [contextoDaConversa, setContextoDaConversa] = useState<IdentidadeDeBanco | null>(null);
  const [contextoTruncado, setContextoTruncado] = useState(false);
  const historicoRef = useRef<HTMLDivElement>(null);
  const controladorRef = useRef<AbortController | null>(null);
  const proximoIdRef = useRef(1);

  useEffect(() => {
    listarProvedores()
      .then(setProvedores)
      .catch(() => setProvedores([]));

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
    setProvedorSelecionado(provedorPadrao);

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

      const provedorDaConversa = detalhada.provedorDeIa;
      if (provedorDaConversa != null && chavesPorProvedor[provedorDaConversa]) {
        setProvedorSelecionado(provedorDaConversa);
        setAvisoDeConfiguracao('');
      } else {
        setProvedorSelecionado(provedorPadrao);
        if (provedorDaConversa != null && !chavesPorProvedor[provedorDaConversa]) {
          setAvisoDeConfiguracao(
            'O provedor desta conversa não está mais configurado; usando o provedor padrão.'
          );
        } else {
          setAvisoDeConfiguracao('');
        }
      }
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

  function salvarChaveDeProvedor(provedor: string, token: string): void {
    setChavesPorProvedor((anteriores) => ({ ...anteriores, [provedor]: token }));
    setProvedorPadrao((atual) => atual ?? provedor);
    setProvedorSelecionado((atual) => atual ?? provedor);
  }

  function removerChaveDeProvedor(provedor: string): void {
    setChavesPorProvedor((anteriores) => {
      const proximas = { ...anteriores };
      delete proximas[provedor];
      return proximas;
    });

    setProvedorPadrao((atual) => {
      if (atual !== provedor) {
        return atual;
      }
      const restantes = Object.keys(chavesPorProvedor).filter((item) => item !== provedor);
      return restantes[0] ?? null;
    });

    setProvedorSelecionado((atual) => {
      if (atual !== provedor) {
        return atual;
      }
      const restantes = Object.keys(chavesPorProvedor).filter((item) => item !== provedor);
      return restantes[0] ?? null;
    });
  }

  const provedoresConfigurados: ProvedorConfigurado[] = provedores
    .filter((provedor) => Boolean(chavesPorProvedor[provedor.provedor]))
    .map((provedor) => ({
      provedor: provedor.provedor,
      rotulo: provedor.rotulo,
      tokenConfigurado: true
    }));

  const chaveDoProvedorSelecionado =
    provedorSelecionado != null ? chavesPorProvedor[provedorSelecionado] : undefined;

  const podeEnviar =
    contextoDeBanco != null &&
    estado !== 'gerando' &&
    provedorSelecionado != null &&
    Boolean(chaveDoProvedorSelecionado) &&
    mensagem.trim().length >= 3;

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
      provedorSelecionado == null ||
      !chaveDoProvedorSelecionado ||
      estado === 'gerando'
    ) {
      if (provedorSelecionado == null || !chaveDoProvedorSelecionado) {
        setAvisoDeConfiguracao(
          'Configure um provedor de IA em "Configurações" para enviar mensagens.'
        );
      }
      return;
    }

    controladorRef.current?.abort();
    const controlador = new AbortController();
    controladorRef.current = controlador;

    setMensagemDeErro('');
    setAvisoDeConfiguracao('');
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
          provedorDeIa: provedorSelecionado,
          chaveDeApi: chaveDoProvedorSelecionado,
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

      // Mensagem sempre genérica: nunca expõe trecho do token, mesmo em
      // falhas de autenticação do provedor (FR-014/SC-007).
      setMensagemDeErro('Não foi possível gerar a consulta pelo provedor selecionado.');
      setEstado('erro');
      void falha;
    }
  }

  return (
    <div className="pagina-do-assistente">
      <header className="barra-superior">
        <button className="botao-secundario" onClick={aoVoltar}>
          Voltar ao diagrama
        </button>
        <h2 className="titulo-do-assistente">Assistente de IA</h2>
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

      {modalAberto && (
        <ModalDeConfiguracaoDeProvedores
          provedores={provedores}
          chavesPorProvedor={chavesPorProvedor}
          provedorPadrao={provedorPadrao}
          aoSalvarChave={salvarChaveDeProvedor}
          aoRemover={removerChaveDeProvedor}
          aoDefinirPadrao={setProvedorPadrao}
          aoFechar={() => setModalAberto(false)}
        />
      )}

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

          {avisoDeConfiguracao !== '' && (
            <div className="alerta-de-aviso-sutil" role="status">
              {avisoDeConfiguracao}{' '}
              <button
                type="button"
                className="botao-secundario"
                onClick={() => setModalAberto(true)}
              >
                Configurações
              </button>
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

          <EntradaDeChat
            mensagem={mensagem}
            aoAlterarMensagem={setMensagem}
            aoEnviar={enviar}
            podeEnviar={podeEnviar}
            provedoresConfigurados={provedoresConfigurados}
            provedorSelecionado={provedorSelecionado}
            aoSelecionarProvedor={(provedor) => {
              setProvedorSelecionado(provedor);
              setAvisoDeConfiguracao('');
            }}
            aoAbrirConfiguracoes={() => setModalAberto(true)}
          />
        </div>
      </div>
    </div>
  );
}
