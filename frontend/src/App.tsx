import { useState } from 'react';
import { FormularioDeConexao } from './componentes/FormularioDeConexao';
import type { ConexaoDeBanco, EsquemaDeBanco } from './modelos/tipos';
import { PaginaDoDiagrama } from './paginas/PaginaDoDiagrama';
import { obterEsquema, testarConexao } from './servicos/ApiDiagrama';

type EstadoDaAplicacao =
  | { tipo: 'desconectado' }
  | { tipo: 'conectando'; conexao: ConexaoDeBanco }
  | { tipo: 'carregandoSchema'; conexao: ConexaoDeBanco }
  | { tipo: 'sucesso'; conexao: ConexaoDeBanco; esquema: EsquemaDeBanco }
  | { tipo: 'erro'; conexao: ConexaoDeBanco; mensagem: string };

/**
 * Estados da interface (spec.md, FR-024): desconectado, conectando,
 * carregando schema, sucesso e erro — mensagens em pt-BR geradas conforme o
 * provider.
 */
export function App() {
  const [estado, setEstado] = useState<EstadoDaAplicacao>({ tipo: 'desconectado' });

  async function conectar(conexao: ConexaoDeBanco): Promise<void> {
    setEstado({ tipo: 'conectando', conexao });

    try {
      const teste = await testarConexao(conexao);

      if (!teste.sucesso) {
        setEstado({
          tipo: 'erro',
          conexao,
          mensagem: teste.mensagem ?? 'Não foi possível conectar ao banco.'
        });
        return;
      }

      setEstado({ tipo: 'carregandoSchema', conexao });

      const esquema = await obterEsquema(conexao);
      setEstado({ tipo: 'sucesso', conexao, esquema });
    } catch (falha) {
      setEstado({
        tipo: 'erro',
        conexao,
        mensagem:
          falha instanceof Error
            ? falha.message
            : 'Não foi possível carregar o schema.'
      });
    }
  }

  function desconectar(): void {
    setEstado({ tipo: 'desconectado' });
  }

  const mensagensDeStatus: Record<string, string> = {
    conectando: 'Conectando ao banco MySQL...',
    carregandoSchema: 'Lendo estrutura do banco MySQL...'
  };

  return (
    <div className="aplicativo">
      <header className="cabecalho">
        <h1>Database Diagram</h1>
        {(estado.tipo === 'conectando' || estado.tipo === 'carregandoSchema') && (
          <p className="estado-progresso" role="status">
            {mensagensDeStatus[estado.tipo]}
          </p>
        )}
        {estado.tipo === 'sucesso' && (
          <p className="estado-sucesso" role="status">
            Schema {estado.esquema.provedor} carregado.
          </p>
        )}
      </header>

      <main className="conteudo">
        {estado.tipo === 'sucesso' ? (
          <PaginaDoDiagrama
            esquema={estado.esquema}
            aoDesconectar={desconectar}
          />
        ) : (
          <div className="tela-de-conexao">
            <FormularioDeConexao
              aoSubmeter={conectar}
              carregando={
                estado.tipo === 'conectando' || estado.tipo === 'carregandoSchema'
              }
            />

            {estado.tipo === 'desconectado' && (
              <p className="orientacao">
                Nenhum banco conectado. Informe uma conexão MySQL com um usuário
                de somente leitura para visualizar o schema.
              </p>
            )}

            {estado.tipo === 'erro' && (
              <div className="alerta-de-erro" role="alert">
                <strong>Não foi possível carregar o schema.</strong>
                <p>{estado.mensagem}</p>
                <ul>
                  <li>conexão e disponibilidade do banco;</li>
                  <li>host e porta;</li>
                  <li>credenciais de somente leitura;</li>
                  <li>nome do database.</li>
                </ul>
              </div>
            )}
          </div>
        )}
      </main>
    </div>
  );
}