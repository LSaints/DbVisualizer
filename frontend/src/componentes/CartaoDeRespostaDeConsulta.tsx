import { useState } from 'react';
import type { RespostaDeConsultaDoAssistente } from '../modelos/tiposDoAssistente';
import { formatarConsultaSqlParaExibicao } from '../utilitarios/formatarConsultaSql';

interface Propriedades {
  resposta: RespostaDeConsultaDoAssistente;
}

/**
 * Renderiza o contrato estruturado em seções tabulares legíveis (SQL em
 * destaque, explicação, objetivo, tabelas, relacionamentos, filtros, campos de
 * retorno, tipo de consulta, resultado esperado e parâmetros) — nunca exibe o
 * JSON bruto (FR-009). O botão "Copiar" coloca a consulta completa na área de
 * transferência com um único clique (FR-010/SC-004).
 */
export function CartaoDeRespostaDeConsulta({ resposta }: Propriedades) {
  const [copiado, setCopiado] = useState(false);

  async function copiarConsulta(): Promise<void> {
    try {
      await navigator.clipboard.writeText(resposta.consulta);
      setCopiado(true);
      window.setTimeout(() => setCopiado(false), 2000);
    } catch {
      setCopiado(false);
    }
  }

  return (
    <article className="cartao-de-resposta">
      <section className="secao-sql" aria-label="Consulta gerada">
        <div className="cabecalho-da-secao-sql">
          <h3>Consulta gerada</h3>
          <button
            type="button"
            className="botao-copiar"
            onClick={copiarConsulta}
            aria-label="Copiar consulta"
          >
            {copiado ? 'Copiado!' : 'Copiar'}
          </button>
        </div>
        <pre>
          <code>{formatarConsultaSqlParaExibicao(resposta.consulta)}</code>
        </pre>
        <p className="tipo-de-consulta">{resposta.tipo_consulta}</p>
      </section>

      {resposta.explicacao && (
        <section aria-label="Explicação">
          <h3>Explicação</h3>
          <p>{resposta.explicacao}</p>
        </section>
      )}

      {resposta.objetivo && (
        <section aria-label="Objetivo">
          <h3>Objetivo</h3>
          <p>{resposta.objetivo}</p>
        </section>
      )}

      {resposta.tabelas.length > 0 && (
        <section aria-label="Tabelas">
          <h3>Tabelas</h3>
          <table className="tabela-de-metadados">
            <thead>
              <tr>
                <th scope="col">Nome</th>
                <th scope="col">Apelido</th>
                <th scope="col">Função</th>
              </tr>
            </thead>
            <tbody>
              {resposta.tabelas.map((tabela, indice) => (
                <tr key={`${tabela.nome}-${indice}`}>
                  <td>{tabela.nome}</td>
                  <td>{tabela.apelido || '—'}</td>
                  <td>{tabela.funcao}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </section>
      )}

      {resposta.relacionamentos.length > 0 && (
        <section aria-label="Relacionamentos">
          <h3>Relacionamentos</h3>
          <table className="tabela-de-metadados">
            <thead>
              <tr>
                <th scope="col">Origem</th>
                <th scope="col">Campo origem</th>
                <th scope="col">Destino</th>
                <th scope="col">Campo destino</th>
                <th scope="col">Tipo</th>
              </tr>
            </thead>
            <tbody>
              {resposta.relacionamentos.map((relacionamento, indice) => (
                <tr key={`${relacionamento.tabela_origem}-${indice}`}>
                  <td>{relacionamento.tabela_origem}</td>
                  <td>{relacionamento.campo_origem}</td>
                  <td>{relacionamento.tabela_destino}</td>
                  <td>{relacionamento.campo_destino}</td>
                  <td>{relacionamento.tipo}</td>
                </tr>
              ))}
            </tbody>
          </table>
          {resposta.relacionamentos.some((relacionamento) => relacionamento.explicacao) && (
            <ul>
              {resposta.relacionamentos.map(
                (relacionamento, indice) =>
                  relacionamento.explicacao && (
                    <li key={`explicacao-${indice}`}>{relacionamento.explicacao}</li>
                  )
              )}
            </ul>
          )}
        </section>
      )}

      {resposta.filtros.length > 0 && (
        <section aria-label="Filtros">
          <h3>Filtros</h3>
          <table className="tabela-de-metadados">
            <thead>
              <tr>
                <th scope="col">Campo</th>
                <th scope="col">Operador</th>
                <th scope="col">Valor</th>
                <th scope="col">Explicação</th>
              </tr>
            </thead>
            <tbody>
              {resposta.filtros.map((filtro, indice) => (
                <tr key={`${filtro.campo}-${indice}`}>
                  <td>{filtro.campo}</td>
                  <td>{filtro.operador}</td>
                  <td>{filtro.valor}</td>
                  <td>{filtro.explicacao}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </section>
      )}

      {Object.entries(resposta.campos_retorno).some(([chave]) => chave !== 'explicacao') && (
        <section aria-label="Campos de retorno">
          <h3>Campos de retorno</h3>
          <table className="tabela-de-metadados">
            <thead>
              <tr>
                <th scope="col">Tabela</th>
                <th scope="col">Campos</th>
              </tr>
            </thead>
            <tbody>
              {Object.entries(resposta.campos_retorno)
                .filter(([chave]) => chave !== 'explicacao')
                .map(([tabela, campos]) => (
                  <tr key={tabela}>
                    <td>{tabela}</td>
                    <td>{campos}</td>
                  </tr>
                ))}
            </tbody>
          </table>
          {resposta.campos_retorno.explicacao && (
            <p>{resposta.campos_retorno.explicacao}</p>
          )}
        </section>
      )}

      {resposta.resultado_esperado && (
        <section aria-label="Resultado esperado">
          <h3>Resultado esperado</h3>
          <p>{resposta.resultado_esperado}</p>
        </section>
      )}

      {resposta.parametros.length > 0 && (
        <section aria-label="Parâmetros">
          <h3>Parâmetros</h3>
          <table className="tabela-de-metadados">
            <thead>
              <tr>
                <th scope="col">Nome</th>
                <th scope="col">Valor</th>
                <th scope="col">Descrição</th>
              </tr>
            </thead>
            <tbody>
              {resposta.parametros.map((parametro, indice) => (
                <tr key={`${parametro.nome}-${indice}`}>
                  <td>{parametro.nome}</td>
                  <td>{parametro.valor}</td>
                  <td>{parametro.descricao}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </section>
      )}
    </article>
  );
}