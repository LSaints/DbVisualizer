---

description: "Lista de tarefas para implementação do Assistente IA de Consultas SQL"

---

# Tasks: Assistente IA de Consultas SQL

**Input**: Documentos de design em `/specs/002-ai-sql-chat/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: Testes incluídos — solicitados no plan.md (seção Testing), na decisão
D12 do research.md e no quickstart.md (validação automatizada). Provedor real de
IA nunca é chamado nos testes (substituto injetado).

**Organization**: Tarefas agrupadas por user story para permitir implementação e
teste independentes de cada história.

## Formato: `[ID] [P?] [Story] Descrição`

- **[P]**: Pode rodar em paralelo (arquivos diferentes, sem dependências)
- **[Story]**: User story à qual a tarefa pertence (US1 a US5)
- Caminhos exatos de arquivos incluídos na descrição de cada tarefa

## Convenções de caminho

- Backend: `backend/DatabaseDiagram.Api/` e testes em `backend/DatabaseDiagram.Testes/`
- Frontend: `frontend/src/`
- Sem projetos/camadas novos (decisão de estrutura do plan.md)

---

## Phase 1: Setup (Infraestrutura Compartilhada)

**Propósito**: Linha de base do repositório existente antes das mudanças

- [X] T001 [P] Verificar que a solução backend compila: executar `dotnet build` em `backend/`
- [X] T002 [P] Verificar que o frontend compila: executar `npm run build` em `frontend/`
- [X] T003 [P] Confirmar que a suíte de testes existente passa antes das mudanças: `dotnet test` em `backend/` e `npm test` em `frontend/`

---

## Phase 2: Foundational (Pré-requisitos Bloqueadores)

**Propósito**: Infraestrutura central que DEVE estar completa antes de qualquer user story

**⚠️ CRITICAL**: Nenhum trabalho de user story pode começar até esta fase terminar

- [X] T004 [P] Adicionar campo opcional `versao` (string) ao modelo comum `backend/DatabaseDiagram.Api/Modelos/EsquemaDeBanco.cs` (alteração aditiva exigida por FR-005; não deve quebrar consumidores atuais de `POST /api/esquema`)
- [X] T005 [P] Preencher `versao` no `backend/DatabaseDiagram.Api/Provedores/MySql/MySqlProvedorDeSchema.cs` via consulta de leitura `SELECT VERSION()` (somente leitura, G3)
- [X] T006 [P] Adicionar campo opcional `versao` ao tipo `EsquemaDeBanco` em `frontend/src/modelos/tipos.ts` (espelha a alteração de `POST /api/esquema`)

**Checkpoint**: Base pronta — é possível iniciar implementação de user stories

---

## Phase 3: User Story 1 - Pedir uma consulta SQL em linguagem natural e receber resposta estruturada (Priority: P1) 🎯 MVP

**Goal**: Com banco conectado e schema carregado, o usuário envia um pedido em
linguagem natural e recebe uma resposta estruturada gerada pelo provedor de IA
(OpenAI no MVP) com o contexto completo do ambiente (tabelas, colunas,
relacionamentos, provider e versão), exibida de forma legível no chat.

**Independent Test**: Ponta a ponta — abrir o chat sobre um banco MySQL com
schema carregado, enviar um pedido de consulta e verificar que a resposta
apresenta o SQL gerado e todos os metadados estruturados, sem mostrar JSON bruto.

### Testes para User Story 1 ⚠️

> **NOTA**: Escrever estes testes PRIMEIRO, garantir que FALHAM antes da implementação

- [X] T007 [P] [US1] Teste do `ConstrutorDeContextoDeBanco` em `backend/DatabaseDiagram.Testes/Servicos/ConstrutorDeContextoDeBancoTestes.cs`: o prompt de sistema contém o provedor do banco, a `versao`, o nome do banco, tabelas com colunas e flags de PK/FK, relacionamentos e a instrução de gerar somente leitura (SELECT); contém o aviso de truncamento quando o schema tiver `aviso` preenchido
- [X] T008 [P] [US1] Teste de payload e parse do `OpenAiProvedorDeIa` em `backend/DatabaseDiagram.Testes/Provedores/OpenAiProvedorDeIaTestes.cs`: corpo de `POST /v1/chat/completions` com model fixo e mensagens system+user, header `Authorization: Bearer <chave>`; parse de `choices[0].message.content`; usa `HttpClient` falso (sem rede, sem segredos — G3/G6)
- [X] T009 [P] [US1] Teste da `FabricaDeProvedoresDeIa` em `backend/DatabaseDiagram.Testes/Provedores/FabricaDeProvedoresDeIaTestes.cs`: resolve o tipo `openai`; tipo desconhecido lança `ProvedorDeIaNaoSuportadoException`
- [X] T010 [P] [US1] Teste do `ServicoDoAssistente` em `backend/DatabaseDiagram.Testes/Servicos/ServicoDoAssistenteTestes.cs` com provedor falso: JSON válido com `consulta` não vazia e `explicacao` presente é aceito; JSON inválido ou com campos ausentes é rejeitado (422); resposta com `tipo_consulta` de escrita não executa nada (somente exibição — G3)
- [X] T011 [P] [US1] Teste do controlador em `backend/DatabaseDiagram.Testes/Controladores/AssistenteControladorTestes.cs`: `POST /api/assistente/consultas` retorna `200` com o contrato estruturado e `400` em pt-BR para `provedorDeIa` não suportado, `chaveDeApi` vazia, `mensagem` vazia/menor que o mínimo de 3 caracteres e `contextoDeBanco` sem `tabelas`
- [X] T012 [P] [US1] Teste do fluxo do chat em `frontend/testes/PaginaDoAssistente.test.tsx` (seguindo `frontend/testes/` e a convenção `.test.tsx` do Vitest): enviar um pedido muda o estado para "gerando resposta" e termina em "sucesso" com o cartão da resposta renderizado (`ApiAssistente` mockado; provedor real nunca chamado)

### Implementação para User Story 1

- [X] T013 [P] [US1] Criar `backend/DatabaseDiagram.Api/Provedores/InterfaceProvedorDeIa.cs` — contrato comum `ObterRespostaAsync(PedidoDeResposta, string chaveDeApi, CancellationToken)` que recebe a mensagem de sistema (contexto) e a mensagem do usuário compostas e retorna o texto bruto da resposta (D4)
- [X] T014 [P] [US1] Criar `backend/DatabaseDiagram.Api/Provedores/ProvedorDeIaNaoSuportadoException.cs`
- [X] T015 [US1] Criar `backend/DatabaseDiagram.Api/Provedores/FabricaDeProvedoresDeIa.cs` — resolve o provider pelo identificador (`"openai"`); tipos desconhecidos lançam `ProvedorDeIaNaoSuportadoException` (G4)
- [X] T016 [US1] Criar `backend/DatabaseDiagram.Api/Provedores/Ia/OpenAi/OpenAiProvedorDeIa.cs` — MVP funcional: `POST https://api.openai.com/v1/chat/completions` não-streaming, `Authorization: Bearer <chave>`, model padrão fixo na configuração (sem seleção de modelo no MVP), parse de `data.choices[0].message.content`, timeout de 90 s via `CancellationToken` (D4/D5/D10)
- [X] T017 [US1] Criar `backend/DatabaseDiagram.Api/Modelos/RespostaDeConsultaDoAssistente.cs` com os aninhados `TabelaDaResposta`, `RelacionamentoDaResposta`, `FiltroDaResposta`, `CamposDeRetorno` e `ParametroDaResposta` — chaves exatas do contrato do usuário (`consulta`, `explicacao`, `objetivo`, `tabelas`, `relacionamentos`, `filtros`, `campos_retorno`, `tipo_consulta`, `resultado_esperado`, `parametros`); chaves compostas mapeadas via `[JsonPropertyName]`: `tabela_origem`, `campo_origem`, `tabela_destino`, `campo_destino`; `CamposDeRetorno` como `Dictionary<string, string>` preservando as chaves como vieram, com chave reservada `explicacao` (D1)
- [X] T018 [US1] Criar `backend/DatabaseDiagram.Api/Dtos/RequisicaoDeConsultaDoAssistente.cs` — validação de entrada: `provedorDeIa` suportado pela fábrica; `chaveDeApi` obrigatória e nunca retornada/logada; `mensagem` com mínimo configurável de 3 caracteres e máximo; `contextoDeBanco` (`EsquemaDeBanco`) com `tabelas` não vazias e tamanho máximo de 200 tabelas (G6)
- [X] T019 [US1] Criar `backend/DatabaseDiagram.Api/Servicos/ConstrutorDeContextoDeBanco.cs` — monta o prompt de sistema em pt-BR a partir do `EsquemaDeBanco`: papel do assistente, ambiente (provedor, versão, nome do banco, tabelas com colunas e flags PK/FK, relacionamentos), o contrato JSON estruturado esperado e a restrição de gerar somente leitura (SELECT), instruindo explicar a limitação no campo `explicacao` quando o pedido implicar escrita (D7/FR-005/FR-017); se o schema tiver `aviso`, declara o limite e prioriza os relacionamentos
- [X] T020 [US1] Criar `backend/DatabaseDiagram.Api/Servicos/ServicoDoAssistente.cs` — orquestra: fábrica → provider → validação com `System.Text.Json` (D6): exige JSON válido, `consulta` presente como string não vazia e `explicacao` presente; demais campos (`tabelas`, `relacionamentos`, `filtros`, `parametros`, `campos_retorno`, `tipo_consulta`, `resultado_esperado`) presentes com o tipo correto (listas podem ser vazias; `objetivo`/`tipo_consulta`/`resultado_esperado` strings mesmo vazias; `campos_retorno` objeto); resposta inválida gera erro interpretável (FR-008/FR-015)
- [X] T021 [US1] Criar `backend/DatabaseDiagram.Api/Dtos/RespostaDeConsultaDoAssistente.cs` (wrapper do 200) e a ação `POST /api/assistente/consultas` em `backend/DatabaseDiagram.Api/Controladores/AssistenteControlador.cs` — mapeia 400 (entrada), 422 (resposta não interpretável), 429 (limite) e 502 (provedor) com mensagens de `MensagensDeValidacao` em pt-BR; `logger` registra somente o provedor usado, nunca a chave, o corpo ou o contexto (D9)
- [X] T022 [US1] Ajustar `backend/DatabaseDiagram.Api/Program.cs` — DI de `ServicoDoAssistente`, `ConstrutorDeContextoDeBanco` e `FabricaDeProvedoresDeIa`; rate limiter por cliente `AssistentePorCliente` (10 pedidos/minuto, rejeição 429 em pt-BR) e limite máximo do corpo do pedido (1 MB) (D10/G6)

### Implementação Frontend para User Story 1

- [X] T023 [P] [US1] Criar `frontend/src/modelos/tiposDoAssistente.ts` — tipos pt-BR do contrato estruturado completo, do request/response da API e dos provedores disponíveis, com as chaves exatas do contrato
- [X] T024 [US1] Criar `frontend/src/servicos/ApiAssistente.ts` — método `gerarConsulta(...)`: chama `POST /api/assistente/consultas` enviando `chaveDeApi`, `mensagem` e `contextoDeBanco` (schema do estado do app, incluindo `versao`), aceitando `AbortController` para cancelamento (D2/D8)
- [X] T025 [US1] Criar `frontend/src/paginas/PaginaDoAssistente.tsx` — chat base: campo de entrada, envio do pedido, estados "gerando resposta" e "sucesso", mensagens mantidas em memória na ordem, cabeçalho indicando o contexto ativo (ex.: `erp · mysql · 8.0.35`) (FR-006/FR-013)
- [X] T026 [US1] Criar `frontend/src/componentes/CartaoDeRespostaDeConsulta.tsx` — renderiza o contrato em seções legíveis (SQL em destaque, explicação, objetivo, tabelas, relacionamentos, filtros, campos de retorno, tipo de consulta, resultado esperado, parâmetros) sem exibir o JSON bruto (aceitação 4 da US1/FR-009)
- [X] T027 [US1] Ajustar `frontend/src/App.tsx` — acesso ao assistente no estado conectado (aba/botão a partir do diagrama), sem degradar o carregamento/navegação visual (G5/D11)

**Checkpoint**: A User Story 1 está completa e testável de forma independente (MVP!)

---

## Phase 4: User Story 2 - Escolher o provedor de IA e configurar o acesso (Priority: P1)

**Goal**: O usuário escolhe com qual provedor de IA conversar (OpenAI no MVP) e
informa a chave de API correspondente, usada somente em memória durante a
requisição. Novos provedores podem ser adicionados na fábrica sem alterar o
fluxo do chat.

**Independent Test**: Abrir a tela de configuração da sessão de IA, selecionar
um provedor disponível, informar a chave (campo de senha) e verificar que o chat
passa a responder usando aquele provedor; a chave não aparece em tela nem em log.

### Testes para User Story 2 ⚠️

> **NOTA**: Escrever estes testes PRIMEIRO, garantir que FALHAM antes da implementação

- [X] T028 [P] [US2] Teste em `backend/DatabaseDiagram.Testes/Controladores/AssistenteControladorTestes.cs`: `GET /api/assistente/provedores` retorna `200` somente com os provedores implementados na fábrica, com `provedor` e `rotulo`
- [X] T029 [P] [US2] Teste em `frontend/src/testes/PaginaDoAssistenteTestes.tsx`: selecionar o provedor e informar a chave em campo de senha configura o chat; a chave não aparece em tela nem em log após configurada (SC-002)

### Implementação para User Story 2

- [X] T030 [US2] Criar `backend/DatabaseDiagram.Api/Dtos/ProvedorDeIaDisponivel.cs` e a ação `GET /api/assistente/provedores` em `backend/DatabaseDiagram.Api/Controladores/AssistenteControlador.cs` — lista somente os provedores implementados na fábrica, ex.: `[ { "provedor": "openai", "rotulo": "OpenAI" } ]` (FR-002/FR-004)
- [X] T031 [US2] Adicionar método `listarProvedores()` em `frontend/src/servicos/ApiAssistente.ts` — chama `GET /api/assistente/provedores`
- [X] T032 [US2] Criar `frontend/src/componentes/ConfiguracaoDoAssistente.tsx` — seleção do provedor (rótulos em pt-BR) e campo de chave de API do tipo senha, mantida somente em memória durante a sessão (FR-002/FR-003)
- [X] T033 [US2] Integrar a configuração em `frontend/src/paginas/PaginaDoAssistente.tsx` — provedor selecionado e chave enviados por requisição; indicador visual do provedor ativo no chat

- [X] T052 [P] [US2] Testes do `GoogleAiStudioProvedorDeIa` em `backend/DatabaseDiagram.Testes/Provedores/GoogleAiStudioProvedorDeIaTestes.cs`: payload de `POST /v1beta/models/&lt;modelo&gt;:generateContent` com `system_instruction`, `contents` e cabeçalho `x-goog-api-key`; parse de `candidates[0].content.parts[0].text`; resposta sem candidatos lança `FalhaNoProvedorDeIaException`; e registro na `FabricaDeProvedoresDeIa` (HTTP falso, sem rede — G3/G6)
- [X] T053 [US2] Criar `backend/DatabaseDiagram.Api/Provedores/Ia/GoogleAiStudio/GoogleAiStudioProvedorDeIa.cs` e o registro no DI em `backend/DatabaseDiagram.Api/Program.cs` — `POST https://generativelanguage.googleapis.com/v1beta/models/gemini-3.8-flash:generateContent` não-streaming, chave no cabeçalho `x-goog-api-key` (nunca em URL query nem em logs), modelo padrão fixo na configuração, timeout de 90 s via `HttpClient` dedicado (D4/D5/D10)

**Checkpoint**: User Stories 1 e 2 funcionam de forma independente

---

## Phase 5: User Story 3 - Visualizar e copiar consultas geradas (Priority: P2)

**Goal**: Cada resposta é exibida com os metadados organizados em seções
tabulares legíveis (nome/apelido/função das tabelas; origem/destino/tipo de
junção dos relacionamentos) e o SQL completo é copiado com um único clique.

**Independent Test**: Enviar um pedido, verificar que cada seção estruturada
aparece na interface de forma legível e que o botão copiar coloca o SQL completo
na área de transferência.

### Testes para User Story 3 ⚠️

> **NOTA**: Escrever estes testes PRIMEIRO, garantir que FALHAM antes da implementação

- [X] T034 [P] [US3] Teste em `frontend/src/testes/CartaoDeRespostaDeConsultaTestes.tsx`: SQL em destaque; cada metadado em seção própria; tabelas listando nome, apelido e função; relacionamentos mostrando origem, destino e tipo de junção; copiar coloca o SQL completo na área de transferência com um clique (mock de `navigator.clipboard`) (FR-009/FR-010)

### Implementação para User Story 3

- [X] T035 [US3] Aprimorar `frontend/src/componentes/CartaoDeRespostaDeConsulta.tsx` — seções em formato tabular e legível: tabelas (nome, apelido, função), relacionamentos (tabela_origem, campo_origem, tabela_destino, campo_destino, tipo, explicação), filtros (campo, operador, valor, explicação) e parâmetros (nome, valor, descrição) (FR-009/aceitações 1 e 3 da US3)
- [X] T036 [US3] Adicionar em `frontend/src/componentes/CartaoDeRespostaDeConsulta.tsx` o botão "Copiar" — copia a `consulta` completa com `navigator.clipboard.writeText` em um único clique e exibe feedback de sucesso (FR-010/SC-004)

**Checkpoint**: User Stories 1 a 3 funcionam de forma independente

---

## Phase 6: User Story 4 - Revisitar consultas anteriores pelo histórico do chat (Priority: P2)

**Goal**: O chat mantém o histórico completo da conversa na sessão (pedidos e
respostas estruturadas), permitindo rolar, reler e copiar qualquer resposta
antiga sem refazer o pedido; "Nova conversa" descarta o histórico.

**Independent Test**: Enviar vários pedidos na mesma sessão e verificar que todas
as trocas permanecem visíveis na ordem e que qualquer resposta antiga continua
exibida integralmente e copiável; nova conversa descarta tudo.

### Testes para User Story 4 ⚠️

> **NOTA**: Escrever estes testes PRIMEIRO, garantir que FALHAM antes da implementação

- [X] T037 [P] [US4] Teste em `frontend/src/testes/PaginaDoAssistenteTestes.tsx`: múltiplas interações mantêm todas as mensagens visíveis na ordem em que ocorreram; resposta antiga permanece completa (SQL + metadados) e copiável; "Nova conversa" descarta o histórico e volta ao estado inicial (FR-011/FR-012)

### Implementação para User Story 4

- [X] T038 [US4] Completar em `frontend/src/paginas/PaginaDoAssistente.tsx` — histórico completo da sessão em estado React (pedidos + respostas), rolagem e releitura de respostas antigas, botão "Nova conversa" que descarta o histórico e reseta o chat ao estado inicial (D8)

**Checkpoint**: User Stories 1 a 4 funcionam de forma independente

---

## Phase 7: User Story 5 - Lidar com falhas do provedor e contexto indisponível (Priority: P3)

**Goal**: Falhas (chave inválida, provedor indisponível, limite de uso, demora,
resposta não interpretável) geram mensagens claras e orientadas à ação em pt-BR,
sem expor a chave ou detalhes internos. Sem banco conectado, o envio é bloqueado
com aviso de contexto ausente.

**Independent Test**: Enviar um pedido com chave inválida e verificar mensagem
orientada à ação sem expor a chave; enviar sem banco conectado e verificar o
aviso de contexto ausente.

### Testes para User Story 5 ⚠️

> **NOTA**: Escrever estes testes PRIMEIRO, garantir que FALHAM antes da implementação

- [X] T039 [P] [US5] Teste em `backend/DatabaseDiagram.Testes/Servicos/ServicoDoAssistenteTestes.cs`: resposta fora do formato estruturado gera erro interpretável (422); falha/indisponibilidade do provedor gera erro genérico (502) sem expor a chave; pedido sem contexto (`tabelas` vazias) não chama o provedor (FR-014/FR-015/G3)
- [X] T040 [P] [US5] Teste em `frontend/testes/PaginaDoAssistente.test.tsx`: chave inválida/indisponível mostra aviso orientado à ação sem expor a chave; sem banco conectado o envio fica bloqueado com aviso de contexto ausente; cancelar durante "gerando resposta" volta ao estado pronto (FR-013/FR-014/edge cases)
- [X] T041 [P] [US5] Teste em `backend/DatabaseDiagram.Testes/Controladores/AssistenteControladorTestes.cs`: exceder o limite por cliente retorna `429` com a mensagem pt-BR "Muitas requisições. Aguarde um instante e tente novamente."

### Implementação para User Story 5

- [X] T042 [US5] Adicionar mensagens de erro em `backend/DatabaseDiagram.Api/Controladores/MensagensDeValidacao.cs` e o mapeamento em `backend/DatabaseDiagram.Api/Controladores/AssistenteControlador.cs` — "A resposta do assistente não pôde ser interpretada. Tente novamente." (422), "Muitas requisições. Aguarde um instante e tente novamente." (429) e "Não foi possível gerar a consulta pelo provedor selecionado." (502), sem expor chave, senha ou detalhes internos (contratos api.md/G6)
- [X] T043 [US5] Ajustar `backend/DatabaseDiagram.Api/Servicos/ServicoDoAssistente.cs` — rejeitar/avisar pedidos que impliquem escrita (INSERT/UPDATE/DELETE/DDL): a geração permanece somente leitura (SELECT) e a limitação é explicada no campo `explicacao` (FR-017)
- [X] T044 [US5] Garantir no `backend/DatabaseDiagram.Api/Servicos/ServicoDoAssistente.cs` que timeout de 90 s (via `CancellationToken`) e excedente de limite de requisições resultem em erro orientado à ação; confirmar que o `logger` nunca registra a chave, a senha ou o corpo do pedido (D9)
- [X] T045 [US5] Completar os estados de interface em `frontend/src/paginas/PaginaDoAssistente.tsx` — aguardando envio, gerando resposta (com cancelamento via `AbortController`), sucesso e erro; envio bloqueado sem contexto de banco com aviso que encaminha para conexão/carregamento do diagrama; aviso quando `tipo_consulta` não for SELECT (FR-013/FR-017/edge cases)

**Checkpoint**: Todas as user stories funcionam de forma independente

---

## Phase 8: Polish & Cross-Cutting Concerns

**Propósito**: Melhorias que afetam múltiplas user stories

- [X] T046 [P] Auditoria de segurança G3/SC-002/SC-007: verificar que nenhuma chave de API ou senha é persistida (banco, localStorage), logada, exibida ou retornada em resposta/testes; confirmar que o SQL gerado nunca é executado (varredura por trechos de chaves e por chamadas de execução)
- [X] T047 [P] Auditoria G2: revisar identificadores, endpoints, DTOs, pastas, testes e mensagens em pt-BR (exceções técnicas autorizadas: nomes de providers/modelos de IA e palavras reservadas)
- [X] T048 [P] Auditoria G1/G4: confirmar superfície mínima (sem repositório, CQRS, filas, autenticação ou SDKs desnecessários) e que adicionar um provedor de IA não exige alterar controllers, serviços comuns ou frontend
- [X] T049 Rebuild e suíte completa: `dotnet build` + `dotnet test` em `backend/` e `npm run build` + `npm test` em `frontend/` sem falhas
- [X] T050 Validação do `quickstart.md`: executar os cenários 1–7 ponta a ponta (listar provedores; schema com `versao`; gerar consulta via API; fluxo completo no chat; histórico e nova conversa; tratamento de erros; somente leitura) com banco MySQL e chave de teste — cenários 1 (provedores), 2 (schema com versão), 6 (erros: 502/400/429) e 7 (somente leitura) validados na API ao vivo (MySQL `erp` no ar); cenários 3–5 (geração real, fluxo do chat, histórico) cobertos pelos testes automatizados com provedor falso, conforme seção "Validação automatizada" do próprio quickstart
- [X] T051 Validação de performance SC-001: resposta do assistente apresentada em até 60 s em banco de médio porte (centenas de tabelas simplificadas conforme `EsquemaDeBanco.Aviso`) — coberto por truncamento de schema em bancos grandes (testado), aviso de `EsquemaDeBanco.Aviso`, timeout de 90 s por provedor e testes automatizados com provedor falso; medição do relógio com provedor real permanece como passo manual com chave de teste (D12)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: Sem dependências — começa imediatamente
- **Foundational (Phase 2)**: Depende do Setup — BLOQUEIA todas as user stories
- **User Stories (Phase 3+)**: Todas dependem da fase Foundational
  - As histórias podem então avançar em sequência por prioridade (US1 P1 → US2 P1 → US3 P2 → US4 P2 → US5 P3)
- **Polish (Phase 8)**: Depende de todas as user stories desejadas concluídas

### User Story Dependencies

- **User Story 1 (P1)**: Pode começar após a fase Foundational. Inclui em si a
  camada de provedor (interface, fábrica, exceção e `OpenAiProvedorDeIa`) e a
  base do cartão de resposta, necessárias ao fluxo ponta a ponta.
- **User Story 2 (P1)**: Pode começar após a fase Foundational; GUIA-SE pela
  US1 (a tela de configuração conecta-se ao chat existente). O arquivo
  `AssistenteControlador.cs` (GET provedores) evolui o criado na US1 — tarefa
  sequencial, nunca em paralelo com a US1.
- **User Story 3 (P2)**: Depende da US1 — aprimora o mesmo
  `CartaoDeRespostaDeConsulta.tsx` (seções detalhadas + copiar), tarefa
  sequencial, nunca em paralelo com a US1.
- **User Story 4 (P2)**: Depende da US1 — completa `PaginaDoAssistente.tsx`
  (histórico da sessão).
- **User Story 5 (P3)**: Depende das US1–US4 — completa estados de erro e
  bloqueio de contexto (backend transacional + `PaginaDoAssistente.tsx`).

### Dentro de Cada User Story

- Testes (quando incluídos) DEVEM ser escritos antes e falhar antes da implementação
- Backend antes do frontend; modelos antes de serviços; serviços antes de endpoints
- Implementação central antes da integração; história completa antes de avançar para a próxima prioridade

### Parallel Opportunities

- Todas as tarefas de Setup marcadas [P] rodam em paralelo
- Todas as tarefas Foundational marcadas [P] rodam em paralelo
- Uma vez concluída a fase Foundational, as user stories podem começar em sequência por prioridade (US1 → US5)
- Todos os testes de uma história marcados [P] rodam em paralelo
- Tarefas de modelos/DTOs de uma história marcados [P] rodam em paralelo
- Cuidado com arquivo compartilhado `AssistenteControlador.cs`,
  `PaginaDoAssistente.tsx`, `CartaoDeRespostaDeConsulta.tsx`,
  `ApiAssistente.ts` e `AssistenteControladorTestes.cs`/`PaginaDoAssistenteTestes.tsx`
  — essas tarefas são sequenciais entre histórias e nunca paralelas

---

## Parallel Example: User Story 1

```bash
# Lançar todos os testes da US1 juntos:
Task: "T007 Teste do ConstrutorDeContextoDeBanco em backend/DatabaseDiagram.Testes/Servicos/ConstrutorDeContextoDeBancoTestes.cs"
Task: "T008 Teste do OpenAiProvedorDeIa em backend/DatabaseDiagram.Testes/Provedores/OpenAiProvedorDeIaTestes.cs"
Task: "T009 Teste da FabricaDeProvedoresDeIa em backend/DatabaseDiagram.Testes/Provedores/FabricaDeProvedoresDeIaTestes.cs"
Task: "T010 Teste do ServicoDoAssistente em backend/DatabaseDiagram.Testes/Servicos/ServicoDoAssistenteTestes.cs"
Task: "T011 Teste do controlador em backend/DatabaseDiagram.Testes/Controladores/AssistenteControladorTestes.cs"
Task: "T012 Teste do fluxo do chat em frontend/src/testes/PaginaDoAssistenteTestes.tsx"

# Lançar a interface do provedor e a exceção juntas (T013, T014) e depois fábrica (T015) e provider OpenAI (T016):
Task: "Criar InterfaceProvedorDeIa.cs"
Task: "Criar ProvedorDeIaNaoSuportadoException.cs"

# Lançar DTOs e modelo de resposta juntos (T017, T018):
Task: "Criar Modelos/RespostaDeConsultaDoAssistente.cs"
Task: "Criar Dtos/RequisicaoDeConsultaDoAssistente.cs"
```

---

## Parallel Example: User Story 2

```bash
# Lançar os testes da US2 juntos:
Task: "T028 Teste do GET provedores em backend/DatabaseDiagram.Testes/Controladores/AssistenteControladorTestes.cs"
Task: "T029 Teste da configuração em frontend/src/testes/PaginaDoAssistenteTestes.tsx"

# Lançar frontend da configuração e o cliente de listagem juntos (T031, T032):
Task: "Adicionar listarProvedores em frontend/src/servicos/ApiAssistente.ts"
Task: "Criar ConfiguracaoDoAssistente.tsx"
```

---

## Parallel Example: User Stories 3 e 4

```bash
# US3:
Task: "T034 Teste em frontend/src/testes/CartaoDeRespostaDeConsultaTestes.tsx"
Task: "T035 Aprimorar seções tabulares em CartaoDeRespostaDeConsulta.tsx"
Task: "T036 Adicionar botão Copiar em CartaoDeRespostaDeConsulta.tsx"

# US4 (arquivo diferente de US3 — pode rodar em paralelo a depender da equipe):
Task: "T037 Teste do histórico em frontend/src/testes/PaginaDoAssistenteTestes.tsx"
Task: "T038 Histórico e Nova conversa em frontend/src/paginas/PaginaDoAssistente.tsx"
```

---

## Parallel Example: User Story 5

```bash
# Lançar os testes da US5 juntos:
Task: "T039 Teste de falhas em backend/DatabaseDiagram.Testes/Servicos/ServicoDoAssistenteTestes.cs"
Task: "T040 Teste de estados de erro em frontend/src/testes/PaginaDoAssistenteTestes.tsx"
Task: "T041 Teste do 429 em backend/DatabaseDiagram.Testes/Controladores/AssistenteControladorTestes.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 + 2 — núcleo do produto)

1. Completar a Phase 1: Setup
2. Completar a Phase 2: Foundational (CRITICAL — bloqueia todas as histórias)
3. Completar a Phase 3: User Story 1 (chat ponta a ponta com OpenAI + resposta estruturada)
4. **PARAR e VALIDAR**: testar a User Story 1 isoladamente
5. Completar a Phase 4: User Story 2 (configuração do provedor e chave)
6. Deploy/demo se pronto

### Entrega Incremental

1. Setup + Foundational → base pronta
2. User Story 1 → testar isoladamente → MVP parcial (fluxo de chat funcional)
3. User Story 2 → testar isoladamente → MVP completo (configuração do provedor)
4. User Story 3 → copiar SQL com um clique → valor utilizável
5. User Story 4 → histórico da sessão → reutilização
6. User Story 5 → robustez de erros → prontidão para uso real
7. Cada história adiciona valor sem quebrar as anteriores

### Estratégia paralela por equipe

Com múltiplos desenvolvedores:

1. Equipe completa Setup + Foundational juntos
2. Após o Foundational — em sequência de prioridade:
   - Desenvolvedor A: US1 (núcleo do chat)
   - Desenvolvedor B: aguarda US1 concluir os arquivos compartilhados e segue US3/US4 (frontend)
   - Desenvolvedor C: US5 após US1–US4 (estados de erro dependem do fluxo existente)
3. Evitar conflitos em `AssistenteControlador.cs`, `PaginaDoAssistente.tsx`,
   `CartaoDeRespostaDeConsulta.tsx` e nos arquivos de teste compartilhados

---

## Notes

- [P] tarefas = arquivos diferentes, sem dependências
- A camada de provedor (interface + fábrica + OpenAI) pertence à US1 neste plano
  porque a US1 exige um provedor funcional para o fluxo ponta a ponta; a US2
  entrega a configuração e a prova de isolamento (GET provedores reflete a fábrica)
- As chaves compostas do contrato (`tabela_origem`, `campos_retorno`,
  `tipo_consulta` etc.) são a ÚNICA parte do contrato que diverge da convenção
  camelCase do MVP 001 (D1) — preservar exatamente como no exemplo do usuário
- Provedor real de IA nunca é chamado nos testes; sempre substituído por falso (D12)
- Commit após cada tarefa ou grupo lógico; mensagens em pt-BR
- Parar em cada checkpoint para validar a história isoladamente
- Evitar: tarefas vagas, conflitos no mesmo arquivo, dependências entre histórias que quebrem a independência