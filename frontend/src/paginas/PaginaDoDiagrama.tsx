import { useEffect, useMemo, useState } from 'react';
import ReactFlow, {
  Background,
  Controls,
  type Edge,
  type Node,
  type NodeDragHandler,
  type NodeMouseHandler
} from 'reactflow';
import { BarraDePesquisa } from '../componentes/BarraDePesquisa';
import { TabelaNoDiagrama, type NodoDeTabela } from '../componentes/TabelaNoDiagrama';
import type { EsquemaDeBanco } from '../modelos/tipos';
import { idDeRelacionamento, idDeTabela, idDeTabelaPorChaves } from '../modelos/tipos';
import { filtrarRelacionamentosDiretos } from '../servicos/FiltroDeRelacionamentos';
import { aplicarLayout, type Posicao } from '../servicos/LayoutDoDiagrama';

const tiposDeNodos: Record<string, (props: unknown) => JSX.Element> = {
  tabela: TabelaNoDiagrama as (props: unknown) => JSX.Element
};

interface Propriedades {
  esquema: EsquemaDeBanco;
  aoDesconectar: () => void;
}

/**
 * Página do diagrama: renderiza tabelas (nodes) e relacionamentos (edges) com
 * React Flow, e oferece navegação (zoom/pan/arrastar), busca, layout
 * automático, ocultação/restauração e foco em relacionamentos diretos.
 */
export function PaginaDoDiagrama({ esquema, aoDesconectar }: Propriedades) {
  const [posicoes, setPosicoes] = useState<Record<string, Posicao>>(() =>
    aplicarLayout(esquema.tabelas, esquema.relacionamentos)
  );
  const [tabelasOcultas, setTabelasOcultas] = useState<Set<string>>(new Set());
  const [busca, setBusca] = useState('');
  const [filtroDeEsquema, setFiltroDeEsquema] = useState('todos');
  const [tabelaSelecionada, setTabelaSelecionada] = useState<string | null>(null);
  const [foco, setFoco] = useState<string | null>(null);

  useEffect(() => {
    setPosicoes(aplicarLayout(esquema.tabelas, esquema.relacionamentos));
    setTabelasOcultas(new Set());
    setBusca('');
    setFiltroDeEsquema('todos');
    setTabelaSelecionada(null);
    setFoco(null);
  }, [esquema]);

  const esquemasDisponiveis = useMemo(() => {
    const unicos = new Set(esquema.tabelas.map((tabela) => tabela.esquema));
    return [...unicos];
  }, [esquema]);

  const focoInfo = useMemo(
    () =>
      foco
        ? filtrarRelacionamentosDiretos(esquema.tabelas, esquema.relacionamentos, foco)
        : null,
    [esquema, foco]
  );

  const tabelasVisiveis = useMemo(() => {
    const termo = busca.trim().toLowerCase();

    let tabelas = esquema.tabelas;

    if (filtroDeEsquema !== 'todos') {
      tabelas = tabelas.filter((tabela) => tabela.esquema === filtroDeEsquema);
    }

    if (termo !== '') {
      tabelas = tabelas.filter((tabela) =>
        tabela.nome.toLowerCase().includes(termo)
      );
    }

    tabelas = tabelas.filter((tabela) => !tabelasOcultas.has(idDeTabela(tabela)));

    if (focoInfo) {
      tabelas = tabelas.filter((tabela) =>
        focoInfo.idsDeTabelas.has(idDeTabela(tabela))
      );
    }

    return tabelas;
  }, [esquema, busca, filtroDeEsquema, tabelasOcultas, focoInfo]);

  const { nodes, edges } = useMemo(() => {
    const idsVisiveis = new Set(tabelasVisiveis.map(idDeTabela));

    const relacoes = esquema.relacionamentos.filter((relacionamento) => {
      const origem = idDeTabelaPorChaves(
        relacionamento.esquemaOrigem,
        relacionamento.tabelaOrigem
      );
      const destino = idDeTabelaPorChaves(
        relacionamento.esquemaDestino,
        relacionamento.tabelaDestino
      );

      if (!idsVisiveis.has(origem) || !idsVisiveis.has(destino)) {
        return false;
      }

      return !focoInfo || focoInfo.idsDeEdges.has(idDeRelacionamento(relacionamento));
    });

    const idsDeArestasConectadas = tabelaSelecionada
      ? new Set(
          relacoes
            .filter(
              (relacionamento) =>
                idDeTabelaPorChaves(
                  relacionamento.esquemaOrigem,
                  relacionamento.tabelaOrigem
                ) === tabelaSelecionada ||
                idDeTabelaPorChaves(
                  relacionamento.esquemaDestino,
                  relacionamento.tabelaDestino
                ) === tabelaSelecionada
            )
            .map(idDeRelacionamento)
        )
      : null;

    const edges: Edge[] = relacoes.map((relacionamento) => {
      const id = idDeRelacionamento(relacionamento);
      const className = idsDeArestasConectadas
        ? idsDeArestasConectadas.has(id)
          ? 'aresta--destacada'
          : 'aresta--atenuada'
        : undefined;

      return {
        id,
        source: idDeTabelaPorChaves(
          relacionamento.esquemaOrigem,
          relacionamento.tabelaOrigem
        ),
        target: idDeTabelaPorChaves(
          relacionamento.esquemaDestino,
          relacionamento.tabelaDestino
        ),
        label: relacionamento.nomeDaRestricao ?? undefined,
        type: 'smoothstep',
        className
      };
    });

    const nodes: Node[] = tabelasVisiveis.map((tabela) => {
      const chave = idDeTabela(tabela);
      const className = tabelaSelecionada
        ? chave === tabelaSelecionada
          ? 'tabela--selecionada'
          : 'tabela--atenuada'
        : undefined;

      return {
        id: chave,
        position: posicoes[chave] ?? { x: 0, y: 0 },
        data: { tabela },
        type: 'tabela',
        className
      } as NodoDeTabela;
    });

    return { nodes, edges };
  }, [tabelasVisiveis, esquema, focoInfo, posicoes, tabelaSelecionada]);

  function organizarAutomaticamente(): void {
    const naoOcultas = esquema.tabelas.filter(
      (tabela) => !tabelasOcultas.has(idDeTabela(tabela))
    );
    setPosicoes(aplicarLayout(naoOcultas, esquema.relacionamentos));
  }

  function ocultarTabelaSelecionada(): void {
    if (!tabelaSelecionada) {
      return;
    }

    setTabelasOcultas((anteriores) => {
      const proximas = new Set(anteriores);
      proximas.add(tabelaSelecionada);
      return proximas;
    });
    setTabelaSelecionada(null);
    setFoco(null);
  }

  function restaurarTabela(chave: string): void {
    setTabelasOcultas((anteriores) => {
      const proximas = new Set(anteriores);
      proximas.delete(chave);
      return proximas;
    });
  }

  function restaurarTodas(): void {
    setTabelasOcultas(new Set());
  }

  const aoClicarNodo: NodeMouseHandler = (_evento, nodo) => {
    setTabelaSelecionada(nodo.id);
    setFoco(null);
  };

  function aoClicarAreaVazia(): void {
    setTabelaSelecionada(null);
    setFoco(null);
  }

  const aoArrastarNodo: NodeDragHandler = (_evento, nodo) => {
    setPosicoes((anteriores) => ({ ...anteriores, [nodo.id]: nodo.position }));
  };

  return (
    <div className="pagina-do-diagrama">
      <header className="barra-superior">
        <div className="informacoes-da-conexao">
          <span className="nome-do-banco">{esquema.nomeDoBanco}</span>
          <span className="provedor-do-banco">{esquema.provedor}</span>
          <button className="botao-secundario" onClick={aoDesconectar}>
            Nova conexão
          </button>
        </div>

        <BarraDePesquisa valor={busca} aoAlterar={setBusca} />

        {esquemasDisponiveis.length > 1 && (
          <select
            className="filtro-de-esquema"
            value={filtroDeEsquema}
            aria-label="Filtrar por schema"
            onChange={(evento) => setFiltroDeEsquema(evento.target.value)}
          >
            <option value="todos">Todos os schemas</option>
            {esquemasDisponiveis.map((esquemaDisponivel) => (
              <option key={esquemaDisponivel} value={esquemaDisponivel}>
                {esquemaDisponivel}
              </option>
            ))}
          </select>
        )}

        <div className="acoes">
          {tabelaSelecionada && !foco && (
            <button className="botao-secundario" onClick={() => setFoco(tabelaSelecionada)}>
              Mostrar relacionamentos
            </button>
          )}

          {foco && (
            <button className="botao-secundario" onClick={() => setFoco(null)}>
              Voltar ao diagrama completo
            </button>
          )}

          {tabelaSelecionada && (
            <button className="botao-secundario" onClick={ocultarTabelaSelecionada}>
              Ocultar tabela
            </button>
          )}

          <button className="botao-primario" onClick={organizarAutomaticamente}>
            Organizar automaticamente
          </button>
        </div>
      </header>

      {tabelasOcultas.size > 0 && (
        <div className="painel-de-ocultas" role="region" aria-label="Tabelas ocultas">
          <span>Ocultas ({tabelasOcultas.size}):</span>
          {[...tabelasOcultas].map((chave) => (
            <button
              key={chave}
              className="campo-oculto"
              onClick={() => restaurarTabela(chave)}
              title="Clique para restaurar"
            >
              {chave}
            </button>
          ))}
          <button className="botao-secundario" onClick={restaurarTodas}>
            Restaurar todas
          </button>
        </div>
      )}

      {esquema.aviso && <div className="aviso-do-banco">{esquema.aviso}</div>}

      <div className="area-do-diagrama">
        {tabelasVisiveis.length === 0 ? (
          <div className="diagrama-vazio">
            {busca.trim() !== '' || focoInfo
              ? 'Nenhuma tabela corresponde ao filtro atual.'
              : 'Nenhuma tabela encontrada no banco.'}
          </div>
        ) : (
          <ReactFlow
            nodes={nodes}
            edges={edges}
            nodeTypes={tiposDeNodos}
            onNodeClick={aoClicarNodo}
            onNodeDragStop={aoArrastarNodo}
            onPaneClick={aoClicarAreaVazia}
            fitView
            fitViewOptions={{ padding: 0.2 }}
            nodesDraggable
            proOptions={{ hideAttribution: true }}
          >
            <Background gap={18} color="#262c3b" />
            <Controls showInteractive={false} />
          </ReactFlow>
        )}
      </div>
    </div>
  );
}