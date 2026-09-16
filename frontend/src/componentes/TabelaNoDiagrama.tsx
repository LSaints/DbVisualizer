import { Handle, Position, type Node, type NodeProps } from 'reactflow';
import type { TabelaDeBanco } from '../modelos/tipos';

export type NodoDeTabela = Node<{ tabela: TabelaDeBanco }, 'tabela'>;

/**
 * Node customizado do React Flow: nome da tabela, schema e lista de colunas
 * com indicadores de PK (🔑), FK (🔗) e auto incremento (↑). Consome o modelo
 * comum, sem dependência de provider (constituição IV).
 */
export function TabelaNoDiagrama({ data }: NodeProps<{ tabela: TabelaDeBanco }>) {
  const { tabela } = data;

  return (
    <div className="tabela-no-diagrama">
      <Handle type="target" position={Position.Left} className="manusear-entrada" />
      <div className="tabela-cabecalho">
        <span className="tabela-nome">{tabela.nome}</span>
        <span className="tabela-esquema">{tabela.esquema}</span>
      </div>
      <div className="tabela-colunas">
        {tabela.colunas.length === 0 && (
          <div className="coluna-vazia">Sem colunas</div>
        )}
        {tabela.colunas.map((coluna) => (
          <div
            className="coluna"
            key={`${coluna.nome}-${coluna.posicaoOrdinal}`}
            title={coluna.comentario ?? undefined}
          >
            <span className="coluna-indicadores">
              {coluna.chavePrimaria ? '🔑' : ''}
              {coluna.chaveEstrangeira ? ' 🔗' : ''}
              {coluna.autoIncremento ? ' ↑' : ''}
            </span>
            <span className="coluna-nome">{coluna.nome}</span>
            <span className="coluna-tipo">
              {coluna.tipoCompletoDoDado ?? coluna.tipoDoDado}
            </span>
          </div>
        ))}
      </div>
      <Handle type="source" position={Position.Right} className="manusear-saida" />
    </div>
  );
}