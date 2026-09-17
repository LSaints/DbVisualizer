# Pesquisa e Decisões — Persistência do Histórico do Chat do Assistente

Fase 0 do `/speckit.plan`. Consolida as decisões técnicas da spec.md
(`specs/003-chat-history-persistence/spec.md`) em conformidade com a
constituição do projeto (`.specify/memory/constitution.md`) e com o estado já
implementado da feature 002 (assistente de IA).

## Decisões

### D1 — Persistência no backend em arquivos JSON locais (não no frontend)

- **Decision**: o backend passa a persistir cada conversa em um arquivo JSON
  local, na forma `{id}.json` (id = `Guid`), dentro de um diretório
  configurável (`Armazenamento:DiretorioDeConversas`, padrão
  `<conteúdo>/dados/conversas/`) **fora da pasta servida**. Um arquivo por
  conversa contém o formato versionado, o título, timestamps, a identidade do
  banco e a lista ordenada das mensagens (pedidos e respostas estruturadas).
- **Rationale**: a chamada ao provedor acontece no backend, então quem precisa
  do histórico na hora de montar o contexto é o backend (FR-002). Persistir no
  backend centraliza a única fonte da verdade, sobrevive ao reinício (SC-001),
  permite exclusão permanente (SC-006) e validação via curl. Arquivos JSON
  evitam dependência nova de banco (G1) e o diretório configurável permite
  injetar um diretório temporário nos testes.
- **Alternatives considered**:
  - `localStorage` no navegador: rejeitado — limitações de tamanho, ciclo de
    vida dependente do navegador, e o backend (que chama o provedor) ficaria
    sem acesso direto ao histórico; exigiria o frontend reenviar tudo a cada
    pedido.
  - Banco SQLite/banco de aplicação no servidor: rejeitado — dependência e
    camada novas sem necessidade; a especificação separa o "banco de dados
    conectado pelo usuário" (somente leitura) do armazenamento de aplicação, e
    arquivos JSON bastam para a escala local de um usuário por vez.

### D2 — Continuar a conversa via `conversaId`; contrato da resposta preservado

- **Decision**: `POST /api/assistente/consultas` passa a aceitar **`conversaId`
  opcional (Guid)**. Quando presente: o backend carrega a conversa, valida a
  identidade do banco, adiciona a mensagem do usuário, chama o provedor com o
  histórico (janela) como contexto, e **em sucesso** persiste o par
  usuário+assistente. Quando ausente: fluxo legado de uma única troca, sem
  persistência (compatibilidade). O **corpo do `200` permanece o contrato
  estruturado intacto** (a validação do backend e a renderização do frontend
  não mudam); o flag de contexto truncado vai em um **header de resposta**
  `X-Contexto-Truncado: true|false`.
- **Rationale**: manter o corpo do `200` inalterado preserva o contrato
  estruturado da feature 002 (decisão D6 da 002) e todo o código de
  renderização/validação (G1). O header é a superfície mínima para o aviso
  sutil de truncamento (FR-011) — é um identificador de protocolo HTTP,
  exceção técnica permitida (G2).
- **Alternatives considered**:
  - Invólucro `{ conversa: {...}, resposta: {...}, contextoTruncado: bool }`:
    rejeitado — alteraria o contrato do `200` documentado na 002 e todos os
    consumidores (renderização, testes, validação), sem ganho real para o
    usuário.
  - Criação implícita de conversa no `POST /api/assistente/consultas`:
    rejeitado — obrigaria a resposta a devolver o novo `conversaId` (header ou
    invólucro) e misturaria criação com geração; o `POST /api/conversas`
    explícito mantém cada endpoint com uma responsabilidade (G1) e é facilmente
    testável por curl.

### D3 — `PedidoDeResposta` estendido com o histórico de conversa

- **Decision**: o record `PedidoDeResposta` ganha
  `MensagensDaConversa` (`IReadOnlyList<MensagemDaConversa>`), em que
  `MensagemDaConversa(Papel, Conteudo)` usa `papel` `"usuario"`/`"assistente"`.
  O provedor continua recebendo um **pedido comum** e é responsável por
  traduzir as trocas para o formato da própria API:
  - OpenAI: `messages = [ {role:"system", ...}, ...{role:"user"/"assistant"}..., {role:"user", mensagem atual} ]`.
  - Google AI Studio: `contents` com `role` `"user"`/`"model"`, **mesclando
    trocas consecutivas do mesmo papel** em uma única entrada (exigência da
    API), com `system_instruction` separado.
- **Rationale**: mantém o G4 (provedor isolado, contrato comum): o serviço
  monta o pedido comum com o histórico e cada provider traduz a
  particularidade da sua API. Adicionar um provedor continua não alterando
  controllers, serviços comuns nem frontend.
- **Alternatives considered**: `ServicoDoAssistente` montando o JSON específico
  de cada provedor: rejeitado — acoplaria a regra ao serviço e quebraria o G4.
  Enviar o texto bruto do histórico como um único bloco "histórico:":
  rejeitado — dependeria do modelo "entender" o formato, menos robusto que
  mensagens com papéis nativos de chat.

### D4 — Janela de contexto configurável e flag de truncamento

- **Decision**: o `ServicoDeConversas` aplica a janela de contexto
  (`Assistente:QuantidadeMaximaDeMensagensDeContexto`, padrão **20 mensagens**)
  selecionando as **últimas N mensagens** da conversa; a mensagem atual do
  usuário é sempre incluída. Se o histórico for maior que a janela, o backend
  marca o truncamento e responde com `X-Contexto-Truncado: true`; o frontend
  exibe o aviso sutil (FR-011). O histórico completo permanece persistido e
  visível (FR-010).
- **Rationale**: atende ao FR-010/SC-008 (janela = contexto, histórico sempre
  persistido) e ao SC-004 (latência controlada em conversas longas). O valor
  ser configurável (não "hardcoded") permite calibrar custo/tempo sem deploy de
  código.
- **Alternatives considered**: enviar toda a conversa como contexto: rejeitado —
  crescimento ilimitado de tokens/custo/latência, contradiz o SC-004 e o G6.
  Resumir as mensagens antigas com uma síntese (summarization): rejeitado —
  custo extra de chamadas e complexidade incompatíveis com o MVP (G1).

### D5 — Persistência em pares somente em sucesso da geração

- **Decision**: a troca (mensagem do usuário + resposta estruturada do
  assistente) é persistida **somente quando a resposta válida chega**. Se o
  provedor falhar ou a resposta for inválida, nada é gravado; o frontend mostra
  o erro e o usuário pode tentar novamente. Fechar a aplicação durante a
  geração não causa estado parcial (edge case da spec: "o histórico persiste até
  a última mensagem salva").
- **Rationale**: evita conversas com mensagens sem resposta e permite "tentar de
  novo" sem duplicar trocas no histórico persistido; é o comportamento mais
  simples e previsível (G1).
- **Alternatives considered**: persistir a mensagem do usuário imediatamente e
  corrigir na falha: rejeitado — ciclos de correção/ruído no arquivo sem
  necessidade no MVP.

### D6 — Título automático a partir da primeira mensagem, renomeável depois

- **Decision**: `POST /api/conversas` cria a conversa com título padrão
  "Nova conversa" e `tituloAutomatico = true`. Na primeira troca (primeira
  mensagem do usuário), se `tituloAutomatico` for `true`, o título é derivado
  de um trecho da mensagem (até ~40 caracteres, resumido com "…") e o flag vira
  `false`. `PATCH /api/conversas/{id}/titulo` altera o título e marca
  `tituloAutomatico = false` (G1: sem heurística de quando não sobrescrever).
- **Rationale**: a lista precisa de títulos identificáveis (US3) sem exigir
  trabalho manual; a renomeação explícita (US4) sempre prevalece.
- **Alternatives considered**: título gerado sempre na listagem (primeira
  mensagem calculada na hora): rejeitado — a lista precisaria carregar as
  mensagens de todas as conversas para montar o título (custo) e a renomeação
  deixaria de ser persistida.

### D7 — Identidade de banco na conversa e divergência de contexto

- **Decision**: cada conversa guarda a **identidade do banco** usada na criação:
  `provedor`, `nomeDoBanco` e `versao`. No `GET /api/conversas/{id}` essa
  identidade é devolvida ao frontend, que compara com o banco atualmente
  conectado e exibe aviso. No `POST /api/assistente/consultas` com `conversaId`,
  se a identidade do banco do pedido divergir da registrada, o backend responde
  `409` com mensagem em pt-BR.
- **Rationale**: atende ao FR-013 e ao US2 (indicar o contexto da conversa e
  avisar divergência), evitando continuar uma conversa sobre um banco errado.
- **Alternatives considered**: comparar o schema inteiro (hash das tabelas):
  rejeitado — excesso de precisão; a identidade (provedor+banco+versão) é a
  divergência que o usuário consegue perceber e corrigir (G1).

### D8 — Sanitização de padrões de segredo antes de persistir e enviar

- **Decision**: ao gravar uma mensagem do usuário e ao montar o contexto, o
  backend aplica uma **sanitização leve** que substitui padrões típicos de
  segredo (`sk-...`, `Bearer ...`, `key=...`) por `"<segredo removido>"`. A
  `chaveDeApi` e a `senha` nunca fazem parte do payload de conversa (são campos
  transitórios da requisição, já nunca persistidos). Logs continuam sem corpo de
  mensagem.
- **Rationale**: garante o SC-005/FR-012 mesmo se o usuário colar uma chave
  dentro do texto do chat, e reforça o princípio III da constituição (segredos
  nunca persistidos/logados).
- **Alternatives considered**: bloquear mensagens que contenham segredos:
  rejeitado — rejeitaria pedidos legítimos e quebraria o fluxo; a substituição
  preserva a conversa e remove o segredo. Criptografia do arquivo de conversa:
  rejeitado — criptografar exige gestão de chave (complexidade nova, G1); a
  estratégia "não guardar segredo nenhum" é suficiente no escopo do MVP.

### D9 — Formato versionado e escrita atômica

- **Decision**: cada arquivo de conversa inicia com `versao` (hoje `1`); cargas
  com versão diferente são detectadas e, se não suportadas, o `GET` devolve a
  conversa parcial com aviso (FR-016). A escrita usa o padrão **gravar em
  arquivo temporário + substituir atomicamente** (temp + rename), evitando
  arquivos corrompidos se a aplicação cair no meio da gravação.
- **Rationale**: atende ao FR-016 (versão do formato e conversas legadas) e
  reduz risco de corrupção no armazenamento local.
- **Alternatives considered**: banco com migrações: rejeitado — sem dependência
  nova (G1); um campo inteiro versionado cobre a evolução do formato no MVP.

### D10 — Testes

- **Decision**: xUnit no backend — `ServicoDeConversas` com diretório
  temporário injetado (criar/listar/abrir/renomear/excluir, persistência entre
  instâncias, título automático, janela de contexto, sanitização, carga de
  formato inválido), tradução do histórico em ambos os provedores (payload e
  merge de papéis), `ConversasControlador` e `AssistenteControlador`
  (conversa inexistente → `404`, banco divergente → `409`, header truncado).
  Vitest + Testing Library no frontend — `ListaDeConversas` e
  `PaginaDoAssistente` (continuar conversa usa o histórico mockado, nova
  conversa cria, renomear/excluir, avisos de contexto divergente e de
  truncamento). Provedores reais nunca chamados (substitutos injetados).
- **Rationale**: mesmo arcabouço da 002 (D12); diretório temporário torna a
  persistência determinística e sem tocar o diretório real.
- **Alternatives considered**: testes de contrato contra a API real do
  provedor: rejeitado (exigiria chaves/rede — igual à 002).

## Decisões herdadas da feature 002 (mantidas)

- Stack backend .NET 8 / ASP.NET Core Web API; frontend React + TS + Vite.
- Contrato estruturado da resposta com chaves do exemplo do usuário (D1 da 002),
  validado com `System.Text.Json` (D6 da 002) e retornado no `200` intacto.
- Provedores de IA isolados com `InterfaceProvedorDeIa` + fábrica (D4 da 002);
  chamada não-streaming com timeout de 90 s (D5/D10 da 002).
- Chave de API efêmera por requisição, só em memória (D9 da 002).
- Rate limit por cliente `AssistentePorCliente` (D10 da 002) — mantido para o
  `POST /api/assistente/consultas`.
- Interface e mensagens em pt-BR (FR-015 da 002).

## Notas de implementação (para `tasks.md`)

- **Sequência de contexto no provedor**: `[system]` + trocas da janela na
  ordem + mensagem atual do usuário. Para o OpenAI, alternância
  user/assistant; para o Gemini, `role` `"user"`/`"model"` com mesclagem de
  trocas consecutivas do mesmo papel em um único objeto `parts`.
- **Arquivo da conversa**: `{id}.json` com `Guid` e campos camelCase
  (`versao`, `id`, `titulo`, `tituloAutomatico`, `criadaEm`, `atualizadaEm`,
  `contextoDeBanco` = `{provedor, nomeDoBanco, versao}`, `mensagens` =
  `[{papel, conteudo, criadaEm}]`). Importante: `conteudo` de papel
  `"assistente"` guarda a resposta estruturada completa (o mesmo objeto do
  contrato) e `conteudo` de papel `"usuario"` guarda o texto do pedido.
- **Header `X-Contexto-Truncado`**: `true` quando a janela omitiu mensagens
  antigas; sempre presente no `200` do `POST /api/assistente/consultas`.
- **Diretório**: não criar o diretório em disco no bootstrap; criar
  lazy (primeira escrita) ou no primeiro uso do `ServicoDeConversas` para não
  poluir repositórios/testes. Garantir que não fica sob `wwwroot` (não é
  servido).

## Registro de conformidade

Reavaliação pós-design do Constitution Check:

- **G1 (YAGNI)**: superfície mínima — `ServicoDeConversas` (com file IO direto)
  + `ConversasControlador` + extensões em `PedidoDeResposta`/
  `ServicoDoAssistente` e tradução nos dois provedores; sem repositório, CQRS,
  banco, fila, autenticação ou SDKs.
- **G2 (pt-BR)**: código, campos JSON (`conversaId`, `titulo`, `criadaEm`,
  `atualizadaEm`, `resumo`, `mensagens`, `papel`, `conteudo`), rotas
  (`api/conversas`) e mensagens em pt-BR; exceções técnicas de protocolo
  (headers HTTP, roles dos provedores `user`/`assistant`/`model`).
- **G3 (somente leitura)**: SQL nunca executado; histórico persistido e
  contexto nunca contêm chave/senha/segredo (sanitização D8); credenciais
  permanecem efêmeras (D9 da 002).
- **G4 (provedores isolados/contrato comum)**: tradução do histórico dentro de
  cada provider; `PedidoDeResposta` mantém o contrato comum; novos provedores
  não tocam controllers/serviços/frontend.
- **G5 (visual first)**: lista/navegação de conversas complementa a exploração
  visual do diagrama, sem degradá-la.
- **G6 (segurança/perf)**: janela de contexto limitada (D4); rate limit
  mantido; escrita atômica (D9); diretório fora da pasta servida; erros
  genéricos; logs sem corpo de mensagem.

**Resultado: GATE PASSOU após o design — sem violações.**