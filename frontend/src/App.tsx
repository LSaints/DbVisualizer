import { useState } from 'react';
import { FormularioDeConexao } from './componentes/FormularioDeConexao';
import type { ConexaoDeBanco, EsquemaDeBanco } from './modelos/tipos';
import { PaginaDoAssistente } from './paginas/PaginaDoAssistente';
import { PaginaDoDiagrama } from './paginas/PaginaDoDiagrama';
import { obterEsquema, obterMaisTabelas, testarConexao } from './servicos/ApiDiagrama';

type EstadoDaAplicacao =
  | { tipo: 'desconectado' }
  | { tipo: 'conectando'; conexao: ConexaoDeBanco }
  | { tipo: 'carregandoSchema'; conexao: ConexaoDeBanco }
  | {
      tipo: 'sucesso';
      conexao: ConexaoDeBanco;
      esquema: EsquemaDeBanco;
      temMaisTabelas: boolean;
    }
  | { tipo: 'erro'; conexao: ConexaoDeBanco; mensagem: string };

type Tela = 'diagrama' | 'assistente';

/**
 * Estados da interface (spec.md, FR-024): desconectado, conectando,
 * carregando schema, sucesso e erro — mensagens em pt-BR geradas conforme o
 * provider.
 */
export function App() {
  const [estado, setEstado] = useState<EstadoDaAplicacao>({ tipo: 'desconectado' });
  const [tela, setTela] = useState<Tela>('diagrama');
  const [carregandoMaisTabelas, setCarregandoMaisTabelas] = useState(false);

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
      setEstado({ tipo: 'sucesso', conexao, esquema, temMaisTabelas: true });
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

  async function carregarMaisTabelas(): Promise<void> {
    if (estado.tipo !== 'sucesso' || carregandoMaisTabelas) {
      return;
    }

    setCarregandoMaisTabelas(true);

    try {
      const nomesCarregados = estado.esquema.tabelas.map((tabela) => tabela.nome);
      const pagina = await obterMaisTabelas(estado.conexao, nomesCarregados);

      setEstado((anterior) => {
        if (anterior.tipo !== 'sucesso') {
          return anterior;
        }

        return {
          ...anterior,
          esquema: {
            ...anterior.esquema,
            tabelas: [...anterior.esquema.tabelas, ...pagina.tabelas],
            relacionamentos: [...anterior.esquema.relacionamentos, ...pagina.relacionamentos]
          },
          temMaisTabelas: pagina.temMaisTabelas
        };
      });
    } catch {
      // Falha ao carregar mais tabelas não invalida o diagrama já carregado;
      // o usuário pode tentar novamente clicando no botão outra vez.
    } finally {
      setCarregandoMaisTabelas(false);
    }
  }

  function desconectar(): void {
    setEstado({ tipo: 'desconectado' });
    setTela('diagrama');
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
        {estado.tipo === 'sucesso' && (
          <nav className="navegacao-da-aplicacao" aria-label="Navegação">
            <button
              type="button"
              className={tela === 'diagrama' ? 'botao-secundario ativo' : 'botao-secundario'}
              onClick={() => setTela('diagrama')}
            >
              Diagrama
            </button>
            <button
              type="button"
              className={tela === 'assistente' ? 'botao-secundario ativo' : 'botao-secundario'}
              onClick={() => setTela('assistente')}
            >
              Assistente de IA
            </button>
          </nav>
        )}
      </header>

      <main className="conteudo">
        {estado.tipo === 'sucesso' && tela === 'assistente' ? (
          <PaginaDoAssistente
            contextoDeBanco={estado.esquema}
            aoVoltar={() => setTela('diagrama')}
          />
        ) : estado.tipo === 'sucesso' ? (
          <PaginaDoDiagrama
            esquema={estado.esquema}
            aoDesconectar={desconectar}
            temMaisTabelas={estado.temMaisTabelas}
            carregandoMaisTabelas={carregandoMaisTabelas}
            aoCarregarMaisTabelas={carregarMaisTabelas}
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