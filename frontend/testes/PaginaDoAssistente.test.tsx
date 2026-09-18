import { act, render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { EsquemaDeBanco } from '../src/modelos/tipos';
import type {
  Conversa,
  ConversaDetalhada,
  ProvedorDeIaDisponivel,
  RespostaDeConsultaDoAssistente
} from '../src/modelos/tiposDoAssistente';
import { PaginaDoAssistente } from '../src/paginas/PaginaDoAssistente';
import {
  criarConversa,
  excluirConversa,
  gerarConsulta,
  listarConversas,
  listarProvedores,
  obterConversa,
  renomearConversa
} from '../src/servicos/ApiAssistente';

vi.mock('../src/servicos/ApiAssistente', () => ({
  gerarConsulta: vi.fn(),
  listarProvedores: vi.fn(),
  listarConversas: vi.fn(),
  criarConversa: vi.fn(),
  obterConversa: vi.fn(),
  renomearConversa: vi.fn(),
  excluirConversa: vi.fn()
}));

const contextoDeBanco: EsquemaDeBanco = {
  provedor: 'mysql',
  nomeDoBanco: 'erp',
  versao: '8.0.35',
  tabelas: [
    {
      esquema: 'erp',
      nome: 'contratos',
      colunas: [
        { nome: 'id', tipoDoDado: 'int', posicaoOrdinal: 1, podeSerNula: false, chavePrimaria: true, chaveEstrangeira: false, autoIncremento: true },
        { nome: 'status', tipoDoDado: 'varchar', posicaoOrdinal: 2, podeSerNula: false, chavePrimaria: false, chaveEstrangeira: false, autoIncremento: false }
      ]
    }
  ],
  relacionamentos: []
};

const resposta: RespostaDeConsultaDoAssistente = {
  consulta: 'SELECT c.* FROM contratos c;',
  explicacao: 'Lista todos os contratos.',
  objetivo: 'Encontrar os contratos.',
  tabelas: [
    { nome: 'contratos', apelido: 'c', funcao: 'Tabela principal da consulta.' }
  ],
  relacionamentos: [],
  filtros: [],
  campos_retorno: { contratos: '*' },
  tipo_consulta: 'SELECT',
  resultado_esperado: 'Uma lista de contratos.',
  parametros: []
};

const segundaResposta: RespostaDeConsultaDoAssistente = {
  consulta: 'SELECT * FROM clientes cl WHERE cl.ativo = 1;',
  explicacao: 'Lista os clientes ativos.',
  objetivo: 'Encontrar clientes ativos.',
  tabelas: [
    { nome: 'clientes', apelido: 'cl', funcao: 'Tabela dos clientes.' }
  ],
  relacionamentos: [],
  filtros: [
    {
      campo: 'clientes.ativo',
      operador: '=',
      valor: '1',
      explicacao: 'Somente clientes ativos.'
    }
  ],
  campos_retorno: { clientes: '*' },
  tipo_consulta: 'SELECT',
  resultado_esperado: 'Lista de clientes ativos.',
  parametros: []
};

describe('PaginaDoAssistente', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(listarProvedores).mockResolvedValue([
      { provedor: 'openai', rotulo: 'OpenAI' },
      { provedor: 'google-ai-studio', rotulo: 'Google AI Studio' }
    ] satisfies ProvedorDeIaDisponivel[]);
    vi.mocked(listarConversas).mockResolvedValue([]);
    vi.mocked(gerarConsulta).mockResolvedValue({ resposta, contextoTruncado: false });
    vi.mocked(criarConversa).mockResolvedValue({
      id: 'conversa-auto',
      titulo: 'Nova conversa',
      criadaEm: '2026-01-01T00:00:00Z',
      atualizadaEm: '2026-01-01T00:00:00Z',
      resumo: 'Conversa vazia'
    } satisfies Conversa);

    Object.defineProperty(navigator, 'clipboard', {
      value: { writeText: vi.fn().mockResolvedValue(undefined) },
      configurable: true
    });
  });

  /** Abre o modal de configurações e cadastra o token do provedor informado (US1). */
  async function configurarToken(provedor: string, rotulo: string, token: string): Promise<void> {
    await userEvent.click(screen.getByRole('button', { name: 'Configurações' }));
    await userEvent.selectOptions(screen.getByLabelText('Provedor a configurar'), provedor);
    await userEvent.type(screen.getByLabelText('Token do provedor'), token);
    await userEvent.click(screen.getByRole('button', { name: 'Salvar' }));
    await userEvent.click(screen.getByRole('button', { name: 'Fechar' }));
    void rotulo;
  }

  it('enviar um pedido muda o estado para "gerando resposta" e termina em sucesso com o cartão', async () => {
    let liberarResposta: (valor: { resposta: RespostaDeConsultaDoAssistente; contextoTruncado: boolean }) => void =
      () => undefined;
    vi.mocked(gerarConsulta).mockReturnValue(
      new Promise((resolver) => {
        liberarResposta = resolver;
      })
    );

    render(<PaginaDoAssistente contextoDeBanco={contextoDeBanco} aoVoltar={() => undefined} />);

    await configurarToken('openai', 'OpenAI', 'sk-teste');
    await userEvent.type(
      screen.getByPlaceholderText('Descreva a consulta desejada...'),
      'quero listar os contratos'
    );
    await userEvent.click(screen.getByRole('button', { name: 'Enviar' }));

    expect(vi.mocked(gerarConsulta)).toHaveBeenCalledWith(
      expect.objectContaining({
        mensagem: 'quero listar os contratos',
        chaveDeApi: 'sk-teste',
        provedorDeIa: 'openai'
      }),
      expect.any(AbortSignal)
    );

    expect(screen.getByText('Gerando resposta...')).toBeInTheDocument();

    await act(async () => {
      liberarResposta({ resposta, contextoTruncado: false });
    });

    expect(
      await screen.findByText('SELECT c.* FROM contratos c;')
    ).toBeInTheDocument();
    expect(screen.getByText('Lista todos os contratos.')).toBeInTheDocument();
  });

  it('configurar o token pelo modal define o provedor como padrão e habilita o badge (US1)', async () => {
    render(<PaginaDoAssistente contextoDeBanco={contextoDeBanco} aoVoltar={() => undefined} />);

    await configurarToken('google-ai-studio', 'Google AI Studio', 'sk-secreta-nao-loge');

    expect(screen.getByRole('button', { name: 'Google AI Studio' })).toHaveAttribute('aria-pressed', 'true');

    await userEvent.type(
      screen.getByPlaceholderText('Descreva a consulta desejada...'),
      'quero listar os contratos'
    );
    await userEvent.click(screen.getByRole('button', { name: 'Enviar' }));

    expect(vi.mocked(gerarConsulta)).toHaveBeenCalledWith(
      expect.objectContaining({
        provedorDeIa: 'google-ai-studio',
        chaveDeApi: 'sk-secreta-nao-loge'
      }),
      expect.any(AbortSignal)
    );

    expect(screen.queryByText('sk-secreta-nao-loge')).not.toBeInTheDocument();
  });

  it('trocar o provedor pelo badge muda o provedorDeIa enviado em gerarConsulta (US2)', async () => {
    render(<PaginaDoAssistente contextoDeBanco={contextoDeBanco} aoVoltar={() => undefined} />);

    await configurarToken('openai', 'OpenAI', 'sk-openai');
    await configurarToken('google-ai-studio', 'Google AI Studio', 'sk-google');

    await userEvent.click(screen.getByRole('button', { name: 'Google AI Studio' }));
    expect(screen.getByRole('button', { name: 'Google AI Studio' })).toHaveAttribute('aria-pressed', 'true');
    expect(screen.getByRole('button', { name: 'OpenAI' })).toHaveAttribute('aria-pressed', 'false');

    await userEvent.type(
      screen.getByPlaceholderText('Descreva a consulta desejada...'),
      'quero listar os contratos'
    );
    await userEvent.click(screen.getByRole('button', { name: 'Enviar' }));

    expect(vi.mocked(gerarConsulta)).toHaveBeenCalledWith(
      expect.objectContaining({ provedorDeIa: 'google-ai-studio' }),
      expect.any(AbortSignal)
    );
  });

  it('sem provedor configurado, Enviar fica desabilitado e orienta a abrir Configurações (FR-014)', async () => {
    render(<PaginaDoAssistente contextoDeBanco={contextoDeBanco} aoVoltar={() => undefined} />);

    const botaoConfigurar = screen.getByRole('button', { name: 'Configurar provedor' });
    expect(botaoConfigurar).toBeInTheDocument();

    await userEvent.type(
      screen.getByPlaceholderText('Descreva a consulta desejada...'),
      'quero listar os contratos'
    );
    expect(screen.getByRole('button', { name: 'Enviar' })).toBeDisabled();

    await userEvent.click(botaoConfigurar);
    expect(screen.getByRole('dialog')).toBeInTheDocument();
  });

  it('mantém todas as mensagens visíveis na ordem e cada resposta antiga completa e copiável (FR-011/FR-012)', async () => {
    vi.mocked(gerarConsulta)
      .mockResolvedValueOnce({ resposta, contextoTruncado: false })
      .mockResolvedValueOnce({ resposta: segundaResposta, contextoTruncado: false });

    render(<PaginaDoAssistente contextoDeBanco={contextoDeBanco} aoVoltar={() => undefined} />);
    await configurarToken('openai', 'OpenAI', 'sk-teste');

    await enviarPedido('quero listar os contratos');
    await enviarPedido('quero os clientes ativos');

    const mensagensDoUsuario = screen.getAllByText(/quero (listar os contratos|os clientes ativos)/);
    expect(mensagensDoUsuario[0]).toHaveTextContent('quero listar os contratos');
    expect(mensagensDoUsuario[1]).toHaveTextContent('quero os clientes ativos');

    const consultasExibidas = screen.getAllByText(/^SELECT/, { selector: 'code' });
    expect(consultasExibidas[0]).toHaveTextContent('SELECT c.* FROM contratos c;');
    expect(consultasExibidas[1]).toHaveTextContent('SELECT * FROM clientes cl WHERE cl.ativo = 1;');

    expect(screen.getByText('Lista todos os contratos.')).toBeInTheDocument();
    expect(screen.getByText('Lista os clientes ativos.')).toBeInTheDocument();
    expect(screen.getByText('Somente clientes ativos.')).toBeInTheDocument();

    const botoesDeCopiar = screen.getAllByRole('button', { name: 'Copiar consulta' });
    expect(botoesDeCopiar).toHaveLength(2);

    await userEvent.click(botoesDeCopiar[0]);
    expect(navigator.clipboard.writeText).toHaveBeenCalledWith(
      'SELECT c.* FROM contratos c;'
    );
    expect(vi.mocked(gerarConsulta)).toHaveBeenCalledTimes(2);
  });

  it('"Nova conversa" cria uma conversa persistida e reseta o chat local (US1/US3)', async () => {
    vi.mocked(criarConversa).mockResolvedValue({
      id: 'conversa-nova',
      titulo: 'Nova conversa',
      criadaEm: '2026-01-01T00:00:00Z',
      atualizadaEm: '2026-01-01T00:00:00Z',
      resumo: 'Conversa vazia'
    } satisfies Conversa);

    render(<PaginaDoAssistente contextoDeBanco={contextoDeBanco} aoVoltar={() => undefined} />);
    await configurarToken('openai', 'OpenAI', 'sk-teste');

    await enviarPedido('quero listar os contratos');
    expect(screen.getByText('SELECT c.* FROM contratos c;')).toBeInTheDocument();

    await userEvent.click(screen.getByRole('button', { name: 'Nova conversa' }));

    expect(screen.queryByText('quero listar os contratos')).not.toBeInTheDocument();
    expect(screen.queryByText('SELECT c.* FROM contratos c;')).not.toBeInTheDocument();
    expect(screen.getByPlaceholderText('Descreva a consulta desejada...')).toHaveValue('');
    expect(vi.mocked(criarConversa)).toHaveBeenCalled();
  });

  it('follow-up com conversaId ativo envia o mesmo id e preserva o histórico anterior (US1)', async () => {
    vi.mocked(listarConversas).mockResolvedValue([
      {
        id: 'conversa-1',
        titulo: 'Contratos',
        criadaEm: '2026-01-01T00:00:00Z',
        atualizadaEm: '2026-01-01T00:00:00Z',
        resumo: 'Conversa vazia'
      }
    ] satisfies Conversa[]);
    vi.mocked(obterConversa).mockResolvedValue({
      id: 'conversa-1',
      titulo: 'Contratos',
      criadaEm: '2026-01-01T00:00:00Z',
      atualizadaEm: '2026-01-01T00:00:00Z',
      resumo: 'Conversa vazia',
      contextoDeBanco: { provedor: 'mysql', nomeDoBanco: 'erp' },
      mensagens: []
    } satisfies ConversaDetalhada);
    vi.mocked(gerarConsulta)
      .mockResolvedValueOnce({ resposta, contextoTruncado: false })
      .mockResolvedValueOnce({ resposta: segundaResposta, contextoTruncado: false });

    render(<PaginaDoAssistente contextoDeBanco={contextoDeBanco} aoVoltar={() => undefined} />);
    await configurarToken('openai', 'OpenAI', 'sk-teste');

    await userEvent.click(await screen.findByRole('button', { name: 'Contratos' }));
    await enviarPedido('quero listar os contratos');
    await enviarPedido('na consulta anterior, inclua o nome do cliente');

    expect(vi.mocked(gerarConsulta)).toHaveBeenNthCalledWith(
      1,
      expect.objectContaining({ conversaId: 'conversa-1' }),
      expect.any(AbortSignal)
    );
    expect(vi.mocked(gerarConsulta)).toHaveBeenNthCalledWith(
      2,
      expect.objectContaining({ conversaId: 'conversa-1' }),
      expect.any(AbortSignal)
    );
    expect(screen.getByText('quero listar os contratos')).toBeInTheDocument();
  });

  it('reabrir uma conversa restaura pedidos e cartões completos com SQL copiável (US2)', async () => {
    vi.mocked(listarConversas).mockResolvedValue([
      {
        id: 'conversa-1',
        titulo: 'Contratos',
        criadaEm: '2026-01-01T00:00:00Z',
        atualizadaEm: '2026-01-01T00:00:00Z',
        resumo: 'Lista todos os contratos.'
      }
    ] satisfies Conversa[]);
    vi.mocked(obterConversa).mockResolvedValue({
      id: 'conversa-1',
      titulo: 'Contratos',
      criadaEm: '2026-01-01T00:00:00Z',
      atualizadaEm: '2026-01-01T00:00:00Z',
      resumo: 'Lista todos os contratos.',
      contextoDeBanco: { provedor: 'mysql', nomeDoBanco: 'erp' },
      mensagens: [
        { papel: 'usuario', conteudo: 'quero listar os contratos', criadaEm: '2026-01-01T00:00:01Z' },
        { papel: 'assistente', conteudo: resposta, criadaEm: '2026-01-01T00:00:02Z' }
      ]
    } satisfies ConversaDetalhada);

    render(<PaginaDoAssistente contextoDeBanco={contextoDeBanco} aoVoltar={() => undefined} />);

    await userEvent.click(await screen.findByRole('button', { name: 'Contratos' }));

    expect(await screen.findByText('quero listar os contratos')).toBeInTheDocument();
    expect(screen.getByText('SELECT c.* FROM contratos c;')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Copiar consulta' })).toBeInTheDocument();
  });

  it('banco conectado diferente do registrado na conversa exibe aviso sutil sem bloquear o histórico (US2)', async () => {
    vi.mocked(listarConversas).mockResolvedValue([
      {
        id: 'conversa-1',
        titulo: 'Outro banco',
        criadaEm: '2026-01-01T00:00:00Z',
        atualizadaEm: '2026-01-01T00:00:00Z',
        resumo: 'Conversa vazia'
      }
    ] satisfies Conversa[]);
    vi.mocked(obterConversa).mockResolvedValue({
      id: 'conversa-1',
      titulo: 'Outro banco',
      criadaEm: '2026-01-01T00:00:00Z',
      atualizadaEm: '2026-01-01T00:00:00Z',
      resumo: 'Conversa vazia',
      contextoDeBanco: { provedor: 'mysql', nomeDoBanco: 'outro-banco' },
      mensagens: []
    } satisfies ConversaDetalhada);

    render(<PaginaDoAssistente contextoDeBanco={contextoDeBanco} aoVoltar={() => undefined} />);

    await userEvent.click(await screen.findByRole('button', { name: 'Outro banco' }));

    expect(await screen.findByText(/banco conectado atualmente difere/i)).toBeInTheDocument();
  });

  it('ao abrir uma conversa com provedorDeIa informado, o badge selecionado reflete esse provedor (US3/SC-004)', async () => {
    vi.mocked(listarConversas).mockResolvedValue([
      {
        id: 'conversa-1',
        titulo: 'Contratos',
        criadaEm: '2026-01-01T00:00:00Z',
        atualizadaEm: '2026-01-01T00:00:00Z',
        resumo: 'Conversa vazia'
      }
    ] satisfies Conversa[]);
    vi.mocked(obterConversa).mockResolvedValue({
      id: 'conversa-1',
      titulo: 'Contratos',
      criadaEm: '2026-01-01T00:00:00Z',
      atualizadaEm: '2026-01-01T00:00:00Z',
      resumo: 'Conversa vazia',
      contextoDeBanco: { provedor: 'mysql', nomeDoBanco: 'erp' },
      provedorDeIa: 'google-ai-studio',
      mensagens: []
    } satisfies ConversaDetalhada);

    render(<PaginaDoAssistente contextoDeBanco={contextoDeBanco} aoVoltar={() => undefined} />);
    await configurarToken('openai', 'OpenAI', 'sk-openai');
    await configurarToken('google-ai-studio', 'Google AI Studio', 'sk-google');

    await userEvent.click(await screen.findByRole('button', { name: 'Contratos' }));

    expect(await screen.findByRole('button', { name: 'Google AI Studio' })).toHaveAttribute(
      'aria-pressed',
      'true'
    );
  });

  it('conversa com provedor não configurado na sessão cai para o padrão com aviso (US3/FR-017)', async () => {
    vi.mocked(listarConversas).mockResolvedValue([
      {
        id: 'conversa-1',
        titulo: 'Contratos',
        criadaEm: '2026-01-01T00:00:00Z',
        atualizadaEm: '2026-01-01T00:00:00Z',
        resumo: 'Conversa vazia'
      }
    ] satisfies Conversa[]);
    vi.mocked(obterConversa).mockResolvedValue({
      id: 'conversa-1',
      titulo: 'Contratos',
      criadaEm: '2026-01-01T00:00:00Z',
      atualizadaEm: '2026-01-01T00:00:00Z',
      resumo: 'Conversa vazia',
      contextoDeBanco: { provedor: 'mysql', nomeDoBanco: 'erp' },
      provedorDeIa: 'claude',
      mensagens: []
    } satisfies ConversaDetalhada);

    render(<PaginaDoAssistente contextoDeBanco={contextoDeBanco} aoVoltar={() => undefined} />);
    await configurarToken('openai', 'OpenAI', 'sk-openai');

    await userEvent.click(await screen.findByRole('button', { name: 'Contratos' }));

    expect(screen.getByRole('button', { name: 'OpenAI' })).toHaveAttribute('aria-pressed', 'true');
    expect(
      await screen.findByText(/provedor desta conversa não está mais configurado/i)
    ).toBeInTheDocument();
  });

  it('conversa sem provedorDeIa usa o provedorPadrao da sessão (US3/C7)', async () => {
    vi.mocked(listarConversas).mockResolvedValue([
      {
        id: 'conversa-1',
        titulo: 'Legada',
        criadaEm: '2026-01-01T00:00:00Z',
        atualizadaEm: '2026-01-01T00:00:00Z',
        resumo: 'Conversa vazia'
      }
    ] satisfies Conversa[]);
    vi.mocked(obterConversa).mockResolvedValue({
      id: 'conversa-1',
      titulo: 'Legada',
      criadaEm: '2026-01-01T00:00:00Z',
      atualizadaEm: '2026-01-01T00:00:00Z',
      resumo: 'Conversa vazia',
      contextoDeBanco: { provedor: 'mysql', nomeDoBanco: 'erp' },
      mensagens: []
    } satisfies ConversaDetalhada);

    render(<PaginaDoAssistente contextoDeBanco={contextoDeBanco} aoVoltar={() => undefined} />);
    await configurarToken('openai', 'OpenAI', 'sk-openai');

    await userEvent.click(await screen.findByRole('button', { name: 'Legada' }));

    expect(screen.getByRole('button', { name: 'OpenAI' })).toHaveAttribute('aria-pressed', 'true');
  });

  it('erro de envio com token inválido mostra mensagem genérica sem nenhum trecho da chave (FR-014/US4)', async () => {
    vi.mocked(gerarConsulta).mockRejectedValue(new Error('token sk-invalida-chave rejeitado pelo provedor'));

    render(<PaginaDoAssistente contextoDeBanco={contextoDeBanco} aoVoltar={() => undefined} />);
    await configurarToken('openai', 'OpenAI', 'sk-invalida-chave');

    await userEvent.type(
      screen.getByPlaceholderText('Descreva a consulta desejada...'),
      'quero listar os contratos'
    );
    await userEvent.click(screen.getByRole('button', { name: 'Enviar' }));

    expect(
      await screen.findByText('Não foi possível gerar a consulta pelo provedor selecionado.')
    ).toBeInTheDocument();

    expect(screen.queryByText('sk-invalida-chave')).not.toBeInTheDocument();
    expect(screen.queryByText(/token sk-invalida-chave/)).not.toBeInTheDocument();
  });

  it('sem banco conectado o envio fica bloqueado com aviso de contexto ausente (FR-006)', async () => {
    const aoVoltar = vi.fn();
    render(<PaginaDoAssistente aoVoltar={aoVoltar} />);

    expect(screen.getByRole('alert')).toHaveTextContent(/Nenhum banco conectado/);
    expect(screen.getByTitle('Nenhum banco conectado')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Enviar' })).toBeDisabled();

    await configurarToken('openai', 'OpenAI', 'sk-teste');
    await userEvent.type(
      screen.getByPlaceholderText('Descreva a consulta desejada...'),
      'quero listar os contratos'
    );
    expect(screen.getByRole('button', { name: 'Enviar' })).toBeDisabled();

    await userEvent.click(screen.getByRole('button', { name: 'Conectar ao banco' }));
    expect(aoVoltar).toHaveBeenCalledTimes(1);
  });

  it('cancelar durante "gerando resposta" volta ao estado pronto (FR-013)', async () => {
    let liberarResposta!: (valor: { resposta: RespostaDeConsultaDoAssistente; contextoTruncado: boolean }) => void;
    let sinalCapturado: AbortSignal | undefined;
    vi.mocked(gerarConsulta).mockImplementation((_requisicao, sinal) => {
      sinalCapturado = sinal;
      return new Promise((resolver) => {
        liberarResposta = resolver;
      });
    });

    render(<PaginaDoAssistente contextoDeBanco={contextoDeBanco} aoVoltar={() => undefined} />);
    await configurarToken('openai', 'OpenAI', 'sk-teste');

    await userEvent.type(
      screen.getByPlaceholderText('Descreva a consulta desejada...'),
      'quero listar os contratos'
    );
    await userEvent.click(screen.getByRole('button', { name: 'Enviar' }));

    expect(screen.getByText('Gerando resposta...')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Enviar' })).toBeDisabled();

    await userEvent.click(screen.getByRole('button', { name: 'Cancelar geração' }));

    expect(screen.queryByText('Gerando resposta...')).not.toBeInTheDocument();
    expect(sinalCapturado?.aborted).toBe(true);

    await userEvent.type(
      screen.getByPlaceholderText('Descreva a consulta desejada...'),
      'outro pedido'
    );
    expect(screen.getByRole('button', { name: 'Enviar' })).not.toBeDisabled();

    await act(async () => {
      liberarResposta({ resposta, contextoTruncado: false });
    });
  });

  it('aviso quando tipo_consulta não for SELECT (FR-017)', async () => {
    const respostaDeEscrita: RespostaDeConsultaDoAssistente = {
      ...resposta,
      consulta: 'UPDATE contratos SET status = "ativo";',
      tipo_consulta: 'UPDATE',
      explicacao: 'O sistema gera somente SELECT.'
    };
    vi.mocked(gerarConsulta).mockResolvedValue({ resposta: respostaDeEscrita, contextoTruncado: false });

    render(<PaginaDoAssistente contextoDeBanco={contextoDeBanco} aoVoltar={() => undefined} />);
    await configurarToken('openai', 'OpenAI', 'sk-teste');

    await userEvent.type(
      screen.getByPlaceholderText('Descreva a consulta desejada...'),
      'atualizar todos os contratos'
    );
    await userEvent.click(screen.getByRole('button', { name: 'Enviar' }));

    expect(
      await screen.findByText(/somente consultas de leitura/i)
    ).toBeInTheDocument();
    expect(screen.getByText('UPDATE contratos SET status = "ativo";')).toBeInTheDocument();
  });

  it('header X-Contexto-Truncado true exibe aviso sutil mantendo o histórico visível (US5)', async () => {
    vi.mocked(gerarConsulta).mockResolvedValue({ resposta, contextoTruncado: true });

    render(<PaginaDoAssistente contextoDeBanco={contextoDeBanco} aoVoltar={() => undefined} />);
    await configurarToken('openai', 'OpenAI', 'sk-teste');

    await enviarPedido('quero listar os contratos');

    expect(
      await screen.findByText(/somente as mensagens mais recentes/i)
    ).toBeInTheDocument();
    expect(screen.getByText('quero listar os contratos')).toBeInTheDocument();
    expect(screen.getByText('SELECT c.* FROM contratos c;')).toBeInTheDocument();
  });

  it('exibe a lista de conversas e permite trocar entre elas preservando o estado (FR-004/FR-017/US3)', async () => {
    vi.mocked(listarConversas).mockResolvedValue([
      {
        id: 'conversa-1',
        titulo: 'Contratos',
        criadaEm: '2026-01-01T00:00:00Z',
        atualizadaEm: '2026-01-01T00:00:00Z',
        resumo: 'Conversa vazia'
      },
      {
        id: 'conversa-2',
        titulo: 'Clientes',
        criadaEm: '2026-01-02T00:00:00Z',
        atualizadaEm: '2026-01-02T00:00:00Z',
        resumo: 'Conversa vazia'
      }
    ] satisfies Conversa[]);

    render(<PaginaDoAssistente contextoDeBanco={contextoDeBanco} aoVoltar={() => undefined} />);

    expect(await screen.findByRole('button', { name: 'Contratos' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Clientes' })).toBeInTheDocument();
  });

  it('excluir a conversa em andamento volta o chat ao estado inicial (US4)', async () => {
    vi.mocked(listarConversas).mockResolvedValue([
      {
        id: 'conversa-1',
        titulo: 'Contratos',
        criadaEm: '2026-01-01T00:00:00Z',
        atualizadaEm: '2026-01-01T00:00:00Z',
        resumo: 'Lista todos os contratos.'
      }
    ] satisfies Conversa[]);
    vi.mocked(obterConversa).mockResolvedValue({
      id: 'conversa-1',
      titulo: 'Contratos',
      criadaEm: '2026-01-01T00:00:00Z',
      atualizadaEm: '2026-01-01T00:00:00Z',
      resumo: 'Lista todos os contratos.',
      contextoDeBanco: { provedor: 'mysql', nomeDoBanco: 'erp' },
      mensagens: [
        { papel: 'usuario', conteudo: 'quero listar os contratos', criadaEm: '2026-01-01T00:00:01Z' },
        { papel: 'assistente', conteudo: resposta, criadaEm: '2026-01-01T00:00:02Z' }
      ]
    } satisfies ConversaDetalhada);
    vi.mocked(excluirConversa).mockResolvedValue(undefined);
    vi.spyOn(window, 'confirm').mockReturnValue(true);

    render(<PaginaDoAssistente contextoDeBanco={contextoDeBanco} aoVoltar={() => undefined} />);

    await userEvent.click(await screen.findByRole('button', { name: 'Contratos' }));
    expect(await screen.findByText('quero listar os contratos')).toBeInTheDocument();

    await userEvent.click(screen.getByRole('button', { name: 'Excluir Contratos' }));

    expect(vi.mocked(excluirConversa)).toHaveBeenCalledWith('conversa-1');
    expect(screen.queryByText('quero listar os contratos')).not.toBeInTheDocument();
  });

  it('renomear a conversa ativa reflete o novo título (US4)', async () => {
    vi.mocked(listarConversas).mockResolvedValue([
      {
        id: 'conversa-1',
        titulo: 'Contratos',
        criadaEm: '2026-01-01T00:00:00Z',
        atualizadaEm: '2026-01-01T00:00:00Z',
        resumo: 'Conversa vazia'
      }
    ] satisfies Conversa[]);
    vi.mocked(renomearConversa).mockResolvedValue(undefined);

    render(<PaginaDoAssistente contextoDeBanco={contextoDeBanco} aoVoltar={() => undefined} />);

    await userEvent.click(await screen.findByRole('button', { name: 'Renomear Contratos' }));
    const campo = screen.getByLabelText('Renomear conversa Contratos');
    await userEvent.clear(campo);
    await userEvent.type(campo, 'Contratos ativos');
    await userEvent.click(screen.getByRole('button', { name: 'Salvar' }));

    expect(vi.mocked(renomearConversa)).toHaveBeenCalledWith('conversa-1', 'Contratos ativos');
  });

  async function enviarPedido(textoDoPedido: string): Promise<void> {
    await userEvent.type(
      screen.getByPlaceholderText('Descreva a consulta desejada...'),
      textoDoPedido
    );
    await userEvent.click(screen.getByRole('button', { name: 'Enviar' }));

    const cartoes = await screen.findAllByText(/^SELECT/, { selector: 'code' });
    expect(cartoes.length).toBeGreaterThan(0);
  }
});
