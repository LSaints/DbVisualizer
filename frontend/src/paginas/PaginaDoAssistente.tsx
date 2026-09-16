import { useEffect, useRef, useState, type FormEvent } from 'react';
import { CartaoDeRespostaDeConsulta } from '../componentes/CartaoDeRespostaDeConsulta';
import { ConfiguracaoDoAssistente } from '../componentes/ConfiguracaoDoAssistente';
import type { EsquemaDeBanco } from '../modelos/tipos';
import type {
  ProvedorDeIaDisponivel,
  RespostaDeConsultaDoAssistente
} from '../modelos/tiposDoAssistente';
import { gerarConsulta, listarProvedores } from '../servicos/ApiAssistente';

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
 * Chat do assistente (FR-006/FR-011/FR-013/FR-017). Mantém o histórico completo
 * da sessão em estado React (pedidos + respostas estruturadas), na ordem, com
 * rolagem automática até a troca mais recente e releitura de respostas antigas
 * (cada cartão é copiável). "Nova conversa" descarta o histórico e reseta o
 * chat ao estado inicial. A geração pode ser cancelada enquanto responde
 * (FR-013); sem banco conectado o envio é bloqueado com aviso de contexto
 * ausente (FR-006); consultas que não sejam SELECT exibem aviso de somente
 * leitura (FR-017). A chave de API é usada somente em memória, por requisição,
 * e nunca aparece em tela após configurada.
 */
export function PaginaDoAssistente({ contextoDeBanco, aoVoltar }: Propriedades) {
  const [provedores, setProvedores] = useState<ProvedorDeIaDisponivel[]>([]);
  const [provedorDeIa, setProvedorDeIa] = useState('openai');
  const [chaveDeApi, setChaveDeApi] = useState('');
  const [mensagem, setMensagem] = useState('');
  const [estado, setEstado] = useState<EstadoDoChat>('pronto');
  const [mensagens, setMensagens] = useState<MensagemDoChat[]>([]);
  const [mensagemDeErro, setMensagemDeErro] = useState('');
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

  function iniciarNovaConversa(): void {
    controladorRef.current?.abort();
    controladorRef.current = null;
    proximoIdRef.current = 1;
    setMensagens([]);
    setEstado('pronto');
    setMensagemDeErro('');
    setMensagem('');
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
      const resposta = await gerarConsulta(
        {
          provedorDeIa,
          chaveDeApi,
          mensagem: texto,
          contextoDeBanco
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
      setEstado('sucesso');
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
        <button
          type="button"
          className="botao-secundario"
          onClick={iniciarNovaConversa}
          disabled={estado === 'gerando' || mensagens.length === 0}
        >
          Nova conversa
        </button>
      </header>

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
  );
}