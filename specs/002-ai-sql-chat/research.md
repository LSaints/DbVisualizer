# Pesquisa e Decisões — Assistente IA de Consultas SQL

Fase 0 do `/speckit.plan`. Consolida as decisões técnicas da spec.md
(`specs/002-ai-sql-chat/spec.md`) em conformidade com a constituição do projeto
(`.specify/memory/constitution.md`).

## Decisões

### D1 — Chaves do contrato estruturado: preservar o formato informado pelo usuário

- **Decision**: o contrato da resposta do assistente usa exatamente as chaves
  do exemplo dado pelo usuário: `consulta`, `explicacao`, `objetivo`, `tabelas`,
  `relacionamentos`, `filtros`, `campos_retorno`, `tipo_consulta`,
  `resultado_esperado`, `parametros`; e nas chaves compostas `tabela_origem`,
  `campo_origem`, `tabela_destino`, `campo_destino` (no sub-objeto
  `relacionamentos`) e `campos_retorno`/`tipo_consulta`/`resultado_esperado`.
- **Rationale**: o usuário explicitou o formato desejado palavra por palavra;
  o frontend renderiza esse contrato e a validação do backend segue as mesmas
  chaves. Desviar silenciosamente criaria uma quebra entre o que o usuário
  pediu e o que o produto retorna. O contrato é um artefato de dados (pt-BR),
  não um identificador de código (G2 não impõe camelCase).
- **Alternatives considered**:
  - Normalizar para camelCase (`tabelaOrigem`, `camposRetorno`,
    `tipoConsulta`...): rejeitado — seria consistente com `contracts/api.md` do
    MVP 001, mas descaracterizaria o exemplo fornecido pelo usuário para a
    resposta do chat.
  - Híbrido com chaves compostas snake_case e demais camelCase: rejeitado por
    criar convenção inconsistente dentro do próprio contrato.
- **Nota**: caso o usuário prefira camelCase, o ajuste é localizado (mapeamento
  documentado em `contracts/api.md` e instrução do prompt); pode ser revisitado
  via `/speckit.clarify`.

### D2 — Contexto de banco enviado ao provedor vem do schema já carregado no frontend

- **Decision**: o frontend (que já mantém `EsquemaDeBanco` no estado do app
  após o carregamento do diagrama) envia o schema como parte do corpo do
  pedido de consulta; o backend apenas o valida levemente (não vazio, tamanho
  máximo) e monta o prompt de sistema com ele.
- **Rationale**: evita re-introspecção por mensagem (custosa e limitada por rate
  limit), garante que o contexto corresponde exatamente ao que o usuário vê no
  diagrama, e mantém o backend sem estado.
- **Alternatives considered**:
  - Backend re-introspectar o banco a cada mensagem usando as credenciais:
    rejeitado — desperdício, expõe as credenciais de banco a cada chamada e
    concorre com o rate limit de introspecção.
  - Backend guardar o schema em memória com um id de sessão: rejeitado — adiciona
    estado no backend sem necessidade (o frontend já o possui).

### D3 — Versão do banco adicionada ao `EsquemaDeBanco` pelo provider

- **Decision**: adicionar o campo `versao` (string, opcional) ao modelo comum
  `EsquemaDeBanco`. Cada provider é responsável por preenchê-lo
  (MySQL: `SELECT VERSION()`; outros bancos pela sua própria consulta).
- **Rationale**: a spec exige envio de "provider e versão" ao provedor de IA
  (FR-005). A versão é um metadado do ambiente e pertence à introspecção do
  provider (G4: particularidade isolada no provider). Alteração aditiva, sem
  quebrar consumidores existentes.
- **Alternatives considered**: endpoint separado só para a versão — rejeitado,
  indireção desnecessária (G1) quando o schema já traz a informação.

### D4 — Provedores de IA isolados com contrato comum

- **Decision**: abstração mínima `InterfaceProvedorDeIa` com
  `ObterRespostaAsync(PedidoDeResposta, string chaveDeApi, CancellationToken)`
  que recebe a mensagem de sistema (contexto) e a mensagem do usuário já
  compostas e retorna o **texto bruto** da resposta. Cada provedor traduz para o
  próprio formato: OpenAI (`POST /v1/chat/completions`, `Authorization: Bearer`,
  `data.choices[0].message.content`), Anthropic/Claude
  (`POST /v1/messages`, headers `x-api-key` + `anthropic-version`,
  `content[0].text`), Google/Gemini
  (`POST {model}:generateContent`, `x-goog-api-key`,
  `candidates[0].content.parts[*].text`) — conforme notas em
  `contracts/api.md` e template de prompt. Uma `FabricaDeProvedoresDeIa`
  resolve o provider pelo `Tipo` (`"openai"`, `"claude"`, `"gemini"`). Modelo
  padrão por provider fixo na configuração (sem seleção de modelo no MVP).
- **Rationale**: atende ao G4 (provedor isolado, contrato comum) espelhando o
  padrão já usado em `FabricaDeProvedoresDeSchema`. A tradução para o formato
  de cada API fica isolada no provider, e o fluxo do chat não muda quando um
  novo provedor é adicionado.
- **Alternatives considered**:
  - SDK proprietário por provedor (OpenAI SDK, Anthropic SDK, SDK GenAI do
    Google): rejeitado — três dependências e três modelos de API no MVP;
    contradiz o G1 e sem necessidade (a chamada é HTTP/JSON simples).
  - Chamada direta no serviço com `if` por provedor: rejeitado — viola o G4.
- **MVP**: ao menos um provedor funcional (recomendado: OpenAI Chat Completions).
  Claude e Gemini entram como novos providers sem tocar controllers/serviços/
  frontend.

### D5 — Resposta sem streaming (não-streaming) com timeout e cancelamento

- **Decision**: a chamada ao provedor é síncrona (sem SSE streaming): o backend
  aguarda a resposta completa do provedor com timeout de 90 s; o frontend mostra
  o estado "gerando resposta" e permite cancelar (AbortController), e o backend
  repassa o `CancellationToken`.
- **Rationale**: a resposta útil é o JSON estruturado completo; streaming de
  texto bruto não melhoraria a experiência de renderizar o cartão. O
  cancelamento cobre o caso de demora (edge case da spec).
- **Alternatives considered**: SSE do backend para o frontend — rejeitado, não
  há ganho para o caso de uso no MVP (G1).

### D6 — Validação do contrato estruturado com `System.Text.Json`

- **Decision**: validação no `ServicoDoAssistente` com `System.Text.Json`
  (jsonDocument): exigir o JSON válido e o campo `consulta` presente (string
  não vazia) e `explicacao` presente; os demais grupos (`tabelas`,
  `relacionamentos`, `filtros`, `parametros`, `campos_retorno`,
  `tipo_consulta`, `resultado_esperado`) presentes com o tipo correto, aceitando
  listas vazias.
- **Rationale**: sem dependência nova (G1); a validação transforma a resposta
  do modelo em um contrato confiável (FR-008). Se inválida, o servidor responde
  um erro interpretável ("resposta não pôde ser interpretada") e o chat permite
  nova tentativa (FR-015).
- **Alternatives considered**: biblioteca de schema JSON (ex.: JsonSchema.Net)
  — rejeitado, peso desnecessário para um contrato pequeno e estável.

### D7 — Prompt de sistema composto pelo `ConstrutorDeContextoDeBanco`

- **Decision**: prompt de sistema em pt-BR montado a partir do
  `EsquemaDeBanco` enviado: informa o papel do assistente, o ambiente (provedor,
  versão, nome do banco, tabelas com colunas e flags de PK/FK, relacionamentos),
  o contrato estruturado esperado (JSON), a restrição de gerar somente leitura
  (SELECT) e a instrução de, quando o pedido implicar escrita, explicar a
  limitação no campo `explicacao`. Se o schema vier truncado (`aviso`), o prompt
  declara o limite e prioriza os relacionamentos.
- **Rationale**: centraliza a composição do contexto (FR-005, FR-017) e mantém
  a instrução de formato/alinhada ao contrato, facilitando testes unitários.
- **Alternatives considered**: prompt montado dentro de cada provider —
  rejeitado, duplicaria e acoplaria a regra de negócio ao provider (G4).

### D8 — Histórico do chat na sessão do frontend; backend sem estado

- **Decision**: o histórico da conversa (mensagens do usuário e cartões do
  assistente) vive no estado React do frontend, por sessão. Cada pedido ao
  backend é autocontido: mensagem do usuário + contexto do banco. Não há
  persistência nem estado de conversa no backend.
- **Rationale**: atende ao FR-011 (histórico via chat durante a sessão) e FR-012
  (nova conversa descarta o histórico) com a menor complexidade (G1), em linha
  com a constituição (nada persistido).
- **Alternatives considered**:
  - Histórico no backend com id de sessão: rejeitado — estado em servidor sem
    autenticação/multi-usuário; sem necessidade no MVP.
  - Enviar todo o histórico de conversa ao provedor a cada pedido (memória de
    turnos): rejeitado — aumenta tokens e latência; o contexto essencial é o do
    banco, não a conversa. Cada pedido é um novo pedido autocontido.

### D9 — Chave de API de IA efêmera, por requisição, no backend

- **Decision**: o frontend recebe a chave do provedor de IA no campo de senha e
  a envia A cada pedido; o backend a usa no momento da chamada, em memória, e
  nunca a persiste, loga ou devolve. A resposta dos endpoints nunca contém a
  chave; mensagens de erro são genéricas.
- **Rationale**: alinhado ao G3 (credenciais somente em memória) e aos SC-002 e
  SC-007 da spec. Não há autenticação de sessão no MVP, então "memória por
  requisição" é o mecanismo disponível mais simples e seguro.
- **Alternatives considered**: armazenar a chave no backend com id de sessão —
  rejeitado, cria estado secreto no servidor sem autenticação (maior superfície
  de risco) e ainda exigiria transporte da chave na configuração inicial.
- **Nota de segurança**: o `logger` do controlador registra apenas o provedor
  usado; nunca a chave, a senha do banco ou o contexto em claro.

### D10 — Rate limiting e proteção básica do assistente

- **Decision**: novo rate limiter por cliente no `Program.cs`
  (`AssistentePorCliente`, ex.: 10 pedidos/minuto, rejeição `429` com mensagem
  pt-BR), tamanho máximo do corpo do pedido (incluindo o schema) no
  `AssistenteControlador`, e timeout de 90 s com `CancellationToken` vinculado
  ao `HttpClient` por provedor.
- **Rationale**: protege o provedor de IA e o servidor de abuso/erros de loop,
  requisito de desempenho do G6; reutiliza o padrão já existente
  (`IntrospeccaoPorCliente`).
- **Alternatives considered**: fila + processamento assíncrono — rejeitado,
  sem necessidade para um usuário por vez (G1).

### D11 — Interface do chat acessível a partir do estado conectado

- **Decision**: com o banco conectado e schema carregado, o usuário alterna entre
  a página do diagrama e a nova página do assistente (aba/botão). O chat indica
  o contexto ativo ("banco `erp` · mysql · 8.0.x") e, sem contexto, bloqueia o
  envio com aviso. Toda a interface em pt-BR.
- **Rationale**: atende ao FR-006, FR-013 e FR-018 e ao G5 (a funcionalidade
  complementa o diagrama, não compete com ele).
- **Alternatives considered**: chat dentro da própria tela do diagrama em um
  painel lateral — rejeitado, reduz o espaço do diagrama (contraria o foco
  visual G5) e adiciona estados complexos.

### D12 — Testes

- **Decision**: xUnit no backend (testes de `ConstrutorDeContextoDeBanco`,
  `ServicoDoAssistente` com provedor falso, `FabricaDeProvedoresDeIa`, payload
  do `OpenAiProvedorDeIa` e controlador) e Vitest + Testing Library no frontend
  (renderização do `CartaoDeRespostaDeConsulta`, fluxo do
  `PaginaDoAssistente` e estados de sucesso/erro/cancelamento). Provedor real de
  IA nunca é chamado nos testes (substituto injetado).
- **Rationale**: mesmo arcabouço do MVP 001 (D5 do 001); o provedor falso torna
  os testes determinísticos e sem rede/segredos (G3/G6).
- **Alternatives considered**: contratos de teste de contrato contra a API real —
  rejeitado, exigiria chaves e rede; inviável no MVP.

## Decisões herdadas do MVP 001 (sem alteração)

- Stack backend .NET 8 / ASP.NET Core Web API / REST; stack frontend React +
  TypeScript + Vite.
- Modelo comum `EsquemaDeBanco` consumido pelo frontend, independente do banco.
- Somente leitura; credenciais em memória; sem persistência; sem autenticação;
  interface e mensagens em pt-BR.
- Segurança: erros genéricos sem expor secrets; rate limit por cliente; timeout.

## Notas de implementação dos provedores de IA (para `tasks.md`)

- **OpenAI Chat Completions**: `POST https://api.openai.com/v1/chat/completions`,
  `Authorization: Bearer <chave>`, corpo `{ "model": <modelo>, "messages": [
  {"role":"developer"|"system", "content": <contexto>}, {"role":"user","content": <mensagem>} ] }`;
  resposta `data.choices[0].message.content`.
- **Anthropic Messages**: `POST https://api.anthropic.com/v1/messages`, headers
  `x-api-key: <chave>` e `anthropic-version: 2023-06-01`, corpo
  `{ "model": <modelo>, "max_tokens": <limite>, "system": <contexto>, "messages": [
  {"role":"user","content": <mensagem>} ] }`; resposta `content[0].text`.
- **Google Gemini**: `POST https://generativelanguage.googleapis.com/v1beta/{model}:generateContent`,
  header `x-goog-api-key: <chave>`, corpo
  `{ "systemInstruction": {"parts":[{"text": <contexto>}]}, "contents": [{"role":"user","parts":[{"text": <mensagem>}]}] }`;
  resposta `candidates[*].content.parts[*].text` (concatenado).
- Todos são não-streaming, retornam texto e aceitam instrução de sistema no
  corpo — base do contrato comum `ObterRespostaAsync`.

## Registro de conformidade

Reavaliação pós-design do Constitution Check:

- **G1 (YAGNI)**: nova superfície mínima — `InterfaceProvedorDeIa` + fábrica +
  `ServicoDoAssistente` + `ConstrutorDeContextoDeBanco`; sem repositório, fila,
  CQRS, autenticação ou SDKs desnecessários.
- **G2 (pt-BR)**: identificadores, endpoints (`api/assistente`), DTOs, testes,
  prompt e mensagens em pt-BR; exceções técnicas: nomes de providers/modelos de
  IA e palavras reservadas.
- **G3 (somente leitura)**: o SQL gerado NUNCA é executado (exibido/copiado);
  chave de API e senha somente em memória, nunca persistidas, logadas ou
  retornadas.
- **G4 (provedor isolado/contrato comum)**: IA isolada por provider
  (OpenAI/Claude/Gemini) via interface + fábrica; adicionar um provedor não
  toca controllers/serviços/frontend.
- **G5 (visual first)**: chat acessível a partir do diagrama, sem degradar a
  exploração visual do schema.
- **G6 (segurança/perf)**: erros genéricos; rate limit; limite de tamanho de
  corpo; timeout 90 s; nunca expor a chave ou o contexto em logs.

**Resultado: GATE PASSOU após o design — sem violações.**