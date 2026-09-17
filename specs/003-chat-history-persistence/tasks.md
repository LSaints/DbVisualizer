---

description: "Lista de tarefas para implementação da persistência do histórico do chat do assistente"

---

# Tasks: Persistência do Histórico do Chat do Assistente

**Input**: Documentos de design em `/specs/003-chat-history-persistence/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: Testes incluídos — solicitados no plan.md (seção Testing), na decisão
D10 do research.md e no quickstart.md (validação automatizada). Provedor real de
IA nunca é chamado nos testes (substituto injetado).

**Organization**: Tarefas agrupadas por user story para permitir implementação e
teste independentes de cada história.

## Formato: `[ID] [P?] [Story] Descrição`

- **[P]**: Pode rodar em paralelo (arquivos diferentes, sem dependências)
- **[Story]**: User story à qual a tarefa pertence (US1 a US5)
- Caminhos exatos de arquivos incluídos na descrição de cada tarefa

## Convenções de caminho

- Backend: `backend/DatabaseDiagram.Api/` e testes em `backend/DatabaseDiagram.Testes/`
- Frontend: `frontend/src/` e testes em `frontend/testes/`
- Sem projetos/camadas novos (decisão de estrutura do plan.md — persistência em
  um único `ServicoDeConversas`, sem camada de repositório)

---

## Phase 1: Setup (Infraestrutura Compartilhada)

**Propósito**: Linha de base do repositório existente antes das mudanças

- [X] T001 [P] Verificar que a solução backend compila: executar `dotnet build` em `backend/`
- [X] T002 [P] Verificar que o frontend compila: executar `npm run build` em `frontend/`
- [X] T003 [P] Confirmar que a suíte de testes existente passa antes das mudanças: `dotnet test` em `backend/` e `npm test` em `frontend/`

---

## Phase 2: Foundational (Pré-requisitos Bloqueadores)

**Propósito**: Núcleo de armazenamento (modelos, serviço de persistência, DTOs e
CRUD de conversas) que DEVE estar completo antes de qualquer user story. O
`ServicoDeConversas` concentra as decisões D4 (janela), D6 (título), D8
(sanitização) e D9 (versão + escrita atômica) do research.md.

**⚠️ CRITICAL**: Nenhum trabalho de user story pode começar até esta fase terminar

### Modelos de domínio

- [X] T004 [P] Criar `backend/DatabaseDiagram.Api/Modelos/IdentidadeDeBanco.cs` — campos `provedor` (string, ex.: `mysql`), `nomeDoBanco` (string, ex.: `erp`) e `versao` (string?, opcional, ex.: `8.0.35`); representa o contexto de banco registrado na conversa (FR-013, D7)
- [X] T005 [P] Criar `backend/DatabaseDiagram.Api/Modelos/MensagemDaConversa.cs` — campos `papel` (string, somente `"usuario"`/`"assistente"`), `conteudo` (string `OU` `RespostaDeConsultaDoAssistente`: `usuario` → texto do pedido; `assistente` → resposta estruturada do contrato da 002) e `criadaEm` (string ISO-8601); papel traduzido pelo provedor (D3)
- [X] T006 Criar `backend/DatabaseDiagram.Api/Modelos/Conversa.cs` — modelo persistido com campos camelCase (formato interno, fora do contrato de API): `versao` (int, "Versão do formato; hoje 1 (FR-016)"), `id` (string, "Guid (file name). Obrigatório"), `titulo` (string, "Título da conversa (listagem); padrão inicial "Nova conversa"; renomeável (US4)"), `tituloAutomatico` (bool, "true até a primeira mensagem; título derivado da 1ª mensagem; PATCH renomear marca false (D6)"), `criadaEm` (ISO-8601), `atualizadaEm` (ISO-8601), `contextoDeBanco` (`IdentidadeDeBanco`) e `mensagens` (`List<MensagemDaConversa>` "Ordem cronológica das trocas; vazio logo após a criação"); serialização com `System.Text.Json` (depende de T004, T005)
- [X] T007 Criar `backend/DatabaseDiagram.Api/Servicos/ServicoDeConversas.cs` — único serviço de persistência (G1), diretório `Armazenamento:DiretorioDeConversas` injetado (padrão `<conteúdo>/dados/conversas/`), **fora** de `wwwroot`/pasta servida, criado lazy (primeira escrita — não no bootstrap); CRUD (`CriarConversaAsync` com "Nova conversa" se título ausente, `ListarConversasAsync`, `ObterConversaAsync` com 404, `RenomearConversaAsync`, `ExcluirConversaAsync` pelo arquivo `{id}.json`), `PersistirTrocaAsync` (salva par usuário+assistente somente em sucesso — D5), `ObterJanelaDeContexto` (últimas N mensagens, N = `Assistente:QuantidadeMaximaDeMensagensDeContexto` com "padrão 20 mensagens", "a mensagem atual do usuário é sempre incluída", retorna flag de truncamento para `X-Contexto-Truncado` — D4/FR-010), título automático (D6: "título é derivado de um trecho da mensagem (até ~40 caracteres, resumido com "…") e o flag vira false", somente se `tituloAutomatico` for true), sanitização (D8) e escrita sempre atômica (temp + rename, D9) (depende de T006)

### DTOs de API

- [X] T008 [P] Criar `backend/DatabaseDiagram.Api/Dtos/ConversaResumo.cs` — item de `GET /api/conversas`: `id`, `titulo`, `criadaEm`, `atualizadaEm`, `resumo` (prévia curta da última troca — "ou "Conversa vazia"")
- [X] T009 [P] Criar `backend/DatabaseDiagram.Api/Dtos/ConversaDetalhada.cs` — resposta de `GET /api/conversas/{id}`: tudo do `ConversaResumo` + `contextoDeBanco` (`IdentidadeDeBanco`) + `mensagens` (`List<MensagemDaConversa>`) (depende de T008)
- [X] T010 [P] Criar `backend/DatabaseDiagram.Api/Dtos/CriarConversa.cs` — corpo do `POST /api/conversas`: `titulo` opcional; "se ausente, "Nova conversa""; inválido (informado e fora de 1–100 caracteres) → `400` "O título deve ter entre 1 e 100 caracteres."
- [X] T011 [P] Criar `backend/DatabaseDiagram.Api/Dtos/RenomearConversa.cs` — corpo do `PATCH /api/conversas/{id}/titulo`: `titulo` obrigatório, "tamanho 1–100; vazio inválido" → `400` "Informe um título entre 1 e 100 caracteres."

### Controlador e infraestrutura

- [X] T012 Criar `backend/DatabaseDiagram.Api/Controladores/ConversasControlador.cs` — rota `api/conversas` (G2) com CRUD completo: `GET /api/conversas` (`200`, "ordenação por `atualizadaEm` decrescente", itens `ConversaResumo`), `POST /api/conversas` (`201` com `ConversaResumo`, `400` para título inválido), `GET /api/conversas/{id}` (`200` com `ConversaDetalhada`, `404` "Conversa não encontrada."), `PATCH /api/conversas/{id}/titulo` (`200`, `400`, `404`; marca `tituloAutomatico = false` — D6) e `DELETE /api/conversas/{id}` (`204`, `404`); sem rate limit dedicado no MVP (a lista/CRUD é barata — contrato api.md); logs somente id de conversa, nunca corpo de mensagem (depende de T007–T011)
- [X] T013 [P] Criar `backend/DatabaseDiagram.Testes/Controladores/ConversasControladorTestes.cs` — CRUD completo com `ConversasControladorTestes` injetando diretório temporário: listar ordenado por `atualizadaEm` decrescente, criar (padrão "Nova conversa" e `201`), abrir (`200` com histórico completo), renomear (`200`, título novo persiste após reinício), excluir (`204` e `404` depois), `400` para título inválido/vazio, `404` para id inexistente (SC-006/SC-007)
- [X] T014 [P] Criar `backend/DatabaseDiagram.Testes/Servicos/ServicoDeConversasTestes.cs` — `ServicoDeConversas` com diretório temporário injetado: persistência entre instâncias (SC-001), criar/listar/abrir/renomear/excluir, título automático derivado da 1ª mensagem (D6), `resumo` com "Conversa vazia", carga de arquivo com `versao` não suportada → conversa parcial com aviso (FR-016), escrita atômica não deixa arquivo parcial
- [X] T015 [P] Ajustar `backend/DatabaseDiagram.Api/appsettings.json` e `backend/DatabaseDiagram.Api/appsettings.Development.json` — novas seções `Armazenamento:DiretorioDeConversas` (padrão `dados/conversas` sob o conteúdo da API, fora da pasta servida) e `Assistente:QuantidadeMaximaDeMensagensDeContexto` ("padrão 20 mensagens")
- [X] T016 Ajustar `backend/DatabaseDiagram.Api/Program.cs` — registro no DI do `ServicoDeConversas` (diretório a partir de `Armazenamento:DiretorioDeConversas`, criado lazy no primeiro uso) e do novo modelo, sem alterar o fluxo dos controladores existentes

**Checkpoint**: Backbone de armazenamento pronto — CRUD de conversas funcional via API e testável por curl (cenários 1–2 e 6 do quickstart.md); user stories podem começar

---

## Phase 3: User Story 1 - Continuar a conversa com o contexto das interações anteriores (Priority: P1) 🎯 MVP

**Goal**: Com uma conversa criada, o usuário envia um follow-up ("na consulta anterior, inclua o nome do cliente no retorno") sem repetir contexto; o backend envia o histórico (janela) como contexto ao provedor, a resposta modifica a consulta anterior e o par usuário+assistente é persistido; no frontend, os follow-ups usam o mesmo `conversaId`.

**Independent Test**: Enviar um primeiro pedido com `conversaId` e, em seguida, um follow-up referenciando a consulta anterior ("na consulta anterior..."), verificando `200` com o contrato estruturado intacto + header `X-Contexto-Truncado`, resposta que altera o trecho solicitado sem exigir repetição do contexto e a troca persistida (SC-002/SC-008).

### Testes para User Story 1 ⚠️

> **NOTA**: Escrever estes testes PRIMEIRO, garantir que FALHAM antes da implementação

- [X] T017 [P] [US1] Ajustar `backend/DatabaseDiagram.Testes/Provedores/OpenAiProvedorDeIaTestes.cs` — payload de `POST /v1/chat/completions` com histórico: `messages = [ {role:"system",...}, ...{role:"user"/"assistant"} das trocas anteriores..., {role:"user", mensagem atual} ]` na ordem cronológica (HTTP falso, sem rede — G3/G6)
- [X] T018 [P] [US1] Ajustar `backend/DatabaseDiagram.Testes/Provedores/GoogleAiStudioProvedorDeIaTestes.cs` — payload de `generateContent` com histórico: `contents` com `role` `"user"`/`"model"`, **mesclando trocas consecutivas do mesmo papel em uma única entrada** (exigência da API — D3), `system_instruction` separado e a mensagem atual ao final (HTTP falso, sem rede)
- [X] T019 [P] [US1] Ajustar `backend/DatabaseDiagram.Testes/Servicos/ServicoDoAssistenteTestes.cs` — com provedor falso, o `PedidoDeResposta` recebido pelo provider contém `MensagensDaConversa` com o histórico (janela) na ordem, além da mensagem atual; conversa curta envia todas as mensagens sem truncamento
- [X] T020 [P] [US1] Ajustar `backend/DatabaseDiagram.Testes/Controladores/AssistenteControladorTestes.cs` — `POST /api/assistente/consultas` com `conversaId`: id inexistente → `404` "Conversa não encontrada." (pt-BR); identidade de banco divergente → `409` "O banco conectado difere do registrado nesta conversa."; sucesso → `200` com o contrato estruturado intacto + header `X-Contexto-Truncado: true|false` presente; par usuário+assistente persistido (verificável via `GET /api/conversas/{id}`); sem `conversaId` → fluxo legado de troca única sem persistência
- [X] T021 [P] [US1] Ajustar `frontend/testes/PaginaDoAssistente.test.tsx` — follow-up que referencia a consulta anterior envia `conversaId` no corpo do pedido (`ApiAssistente` mockado) e a resposta atualiza o histórico mantendo as mensagens anteriores intactas (aceitações 1, 2 e 4 da US1)

### Implementação para User Story 1

- [X] T022 [P] [US1] Ajustar `backend/DatabaseDiagram.Api/Provedores/PedidoDeResposta.cs` — adicionar `MensagensDaConversa` (`IReadOnlyList<MensagemDaConversa>`) ao record, mantendo `MensagemDeSistema` e `MensagemDoUsuario`; contrato comum preservado (G4)
- [X] T023 [US1] Ajustar `backend/DatabaseDiagram.Api/Provedores/Ia/OpenAi/OpenAiProvedorDeIa.cs` — traduzir o histórico para `messages` com papéis nativos: `[ {role:"system", ...}, ...{role:"user"/"assistant"}..., {role:"user", mensagem atual} ]`, na ordem da janela (D3)
- [X] T024 [US1] Ajustar `backend/DatabaseDiagram.Api/Provedores/Ia/GoogleAiStudio/GoogleAiStudioProvedorDeIa.cs` — traduzir o histórico para `contents` com `role` `"user"`/`"model"`, mesclando trocas consecutivas do mesmo papel em um único objeto `parts`; `system_instruction` separado (D3/notas de implementação do research.md)
- [X] T025 [US1] Ajustar `backend/DatabaseDiagram.Api/Servicos/ServicoDoAssistente.cs` — montar o `PedidoDeResposta` com a janela de contexto do `ServicoDeConversas` (schema do `ConstrutorDeContextoDeBanco` + histórico da janela + mensagem atual) e repassar `MensagensDaConversa` ao provider (FR-002)
- [X] T026 [US1] Ajustar `backend/DatabaseDiagram.Api/Dtos/RequisicaoDeConsultaDoAssistente.cs` — adicionar `conversaId` opcional (Guid?); sem `conversaId` → comportamento legado de troca única (sem persistência)
- [X] T027 [US1] Ajustar `backend/DatabaseDiagram.Api/Controladores/AssistenteControlador.cs` — com `conversaId`: carregar conversa (`404`), validar identidade de banco (divergente → `409` "O banco conectado difere do registrado nesta conversa."), sanitizar a mensagem (D8), montar contexto com a janela, chamar o provedor, e **em sucesso** persistir o par usuário+assistente e adicionar em toda resposta `200` o header `X-Contexto-Truncado: true|false`; em falha nada é persistido (D5); logger registra somente o provedor e o id de conversa, nunca o corpo
- [X] T028 [P] [US1] Ajustar `frontend/src/modelos/tiposDoAssistente.ts` — adicionar `conversaId` (opcional) ao request de consulta e os tipos pt-BR `Conversa`, `MensagemDaConversa` (`papel`, `conteudo`, `criadaEm`) e `IdentidadeDeBanco`
- [X] T029 [P] [US1] Ajustar `frontend/src/servicos/ApiAssistente.ts` — `gerarConsulta(...)` envia `conversaId` no corpo quando presente e lê o header `X-Contexto-Truncado` da resposta para uso posterior (FR-011)
- [X] T030 [US1] Ajustar `frontend/src/paginas/PaginaDoAssistente.tsx` — manter o `conversaId` ativo no estado; follow-ups reutilizam o mesmo `conversaId` para continuar a conversa; nova resposta adicionada ao histórico preservando o restante abaixo (D2)

**Checkpoint**: User Story 1 completa e testável isoladamente — follow-up modifica a consulta anterior sem repetir contexto (MVP backend de continuação)

---

## Phase 4: User Story 2 - Persistir o histórico entre sessões e continuar depois (Priority: P1)

**Goal**: O usuário fecha a aplicação, reabre e reencontra a mesma conversa com todas as mensagens anteriores completas (SQL legível e copiável); um novo pedido é tratado como continuação do histórico persistido, e o sistema avisa quando o banco conectado difere do registrado na conversa (FR-013).

**Independent Test**: Abrir o chat, fazer algumas trocas, fechar a aplicação, reabrir, verificar que a conversa reaparece com o histórico completo e que um novo pedido é respondido como continuação; conectar a outro banco e verificar o aviso de divergência (SC-001/FR-013).

### Testes para User Story 2 ⚠️

> **NOTA**: Escrever estes testes PRIMEIRO, garantir que FALHAM antes da implementação

- [X] T031 [P] [US2] Ajustar `backend/DatabaseDiagram.Testes/Controladores/AssistenteControladorTestes.cs` — após persistir trocas com `conversaId`, `GET /api/conversas/{id}` restaura integralmente o histórico (todos os metadados da resposta estruturada presentes); um novo `POST` com o mesmo `conversaId` usa o histórico persistido como contexto e adiciona a nova resposta sem duplicar trocas (aceitações 1–3 da US2)
- [X] T032 [P] [US2] Ajustar `frontend/testes/PaginaDoAssistente.test.tsx` — reabrir uma conversa restaura todas as mensagens (pedidos e cartões completos, SQL copiável via `CartaoDeRespostaDeConsulta`); novo follow-up continua a conversa com o histórico; banco conectado diferente do registrado exibe aviso em pt-BR (aceitação 4 da US2)

### Implementação para User Story 2

- [X] T033 [P] [US2] Ajustar `frontend/src/servicos/ApiAssistente.ts` — adicionar `obterConversa(id)` chamando `GET /api/conversas/{id}` e retornando `ConversaDetalhada` (`contextoDeBanco` + `mensagens`)
- [X] T034 [US2] Ajustar `frontend/src/paginas/PaginaDoAssistente.tsx` — ao abrir uma conversa, carregar `obterConversa(id)`, restaurar o histórico completo na ordem com o `CartaoDeRespostaDeConsulta` existente (SQL copiável — SC-003) e permitir continuar enviando novos pedidos com o mesmo `conversaId` (FR-005/FR-006)
- [X] T035 [US2] Ajustar `frontend/src/paginas/PaginaDoAssistente.tsx` — indicar o contexto de banco registrado na conversa (`contextoDeBanco`) e exibir aviso sutil em pt-BR quando o banco/schema atualmente conectado diferir do registrado, sem bloquear a visualização do histórico (FR-013/aceitação 4 da US2)

**Checkpoint**: User Stories 1 e 2 funcionam — histórico sobrevive ao reinício e continua no ponto exato

---

## Phase 5: User Story 3 - Listar, revisitar e copiar conversas anteriores (Priority: P2)

**Goal**: O usuário vê a lista de conversas persistidas (título, data/hora e prévia), abre qualquer uma para reler e copiar o SQL, inicia conversas novas sem apagar as anteriores e troca entre conversas sem perder o estado da atual.

**Independent Test**: Criar duas conversas com assuntos distintos, verificar que ambas aparecem na lista com títulos identificáveis, que abrir qualquer uma restaura o histórico e que o SQL segue copiável; iniciar uma nova conversa mantém as anteriores (SC-007).

### Testes para User Story 3 ⚠️

> **NOTA**: Escrever estes testes PRIMEIRO, garantir que FALHAM antes da implementação

- [X] T036 [P] [US3] Criar `frontend/testes/ListaDeConversas.test.tsx` — a lista mostra título, data/hora e prévia curta (`resumo`, "Conversa vazia" quando vazia) de cada conversa; abrir uma conversa restaura o histórico completo com SQL copiável; "Nova conversa" cria uma vazia e as anteriores permanecem na lista; trocar para outra conversa salva a atual sem perder a última mensagem (aceitações 1–4 da US3)
- [X] T037 [P] [US3] Ajustar `frontend/testes/PaginaDoAssistente.test.tsx` — a página do assistente exibe a lista de conversas; iniciar nova conversa cria vazia; trocar entre conversas preserva o estado (FR-004/FR-017)

### Implementação para User Story 3

- [X] T038 [US3] Criar `frontend/src/componentes/ListaDeConversas.tsx` — lista de navegação com título, `criadaEm`/`atualizadaEm` e `resumo` por item, botão "Nova conversa", seleção para abrir a conversa (FR-004/FR-009)
- [X] T039 [P] [US3] Ajustar `frontend/src/servicos/ApiAssistente.ts` — adicionar `listarConversas()` (`GET /api/conversas`) e `criarConversa()` (`POST /api/conversas`)
- [X] T040 [US3] Ajustar `frontend/src/paginas/PaginaDoAssistente.tsx` — integrar a `ListaDeConversas` à página: exibir a lista, abrir a conversa selecionada, criar nova conversa e trocar entre conversas salvando automaticamente a atual (FR-017)

**Checkpoint**: User Stories 1 a 3 — conversas encontráveis pela lista e navegáveis

---

## Phase 6: User Story 4 - Renomear e excluir conversas (Priority: P2)

**Goal**: O usuário renomeia uma conversa para um título reconhecível (persistido) e exclui uma conversa permanentemente com confirmação; após excluir, o chat volta ao estado inicial sem mensagens remanescentes.

**Independent Test**: Renomear uma conversa e conferir o novo título na lista (persistente após reiniciar); excluir uma conversa com confirmação e verificar que ela some da lista e não reaparece após reiniciar (SC-006).

### Testes para User Story 4 ⚠️

> **NOTA**: Escrever estes testes PRIMEIRO, garantir que FALHAM antes da implementação

- [X] T041 [P] [US4] Ajustar `frontend/testes/ListaDeConversas.test.tsx` — renomear atualiza o título na lista e persiste após reabrir; excluir exige confirmação, remove a conversa da lista e remove as mensagens (aceitações 1–3 da US4)

### Implementação para User Story 4

- [X] T042 [P] [US4] Ajustar `frontend/src/servicos/ApiAssistente.ts` — adicionar `renomearConversa(id, titulo)` (`PATCH /api/conversas/{id}/titulo`) e `excluirConversa(id)` (`DELETE /api/conversas/{id}`)
- [X] T043 [US4] Ajustar `frontend/src/componentes/ListaDeConversas.tsx` — ações de renomear (edição do título, validação 1–100) e excluir (com confirmação em pt-BR) por conversa; título renomeado refletido na lista (FR-008/FR-007)
- [X] T044 [US4] Ajustar `frontend/src/paginas/PaginaDoAssistente.tsx` — após excluir a conversa em andamento, o chat volta ao estado inicial (lista de conversas ou nova conversa) sem mensagens remanescentes da excluída; título renomeado exibido no cabeçalho da conversa ativa (aceitação 3 da US4)

**Checkpoint**: User Stories 1 a 4 — controle completo de identificação e exclusão de conversas

---

## Phase 7: User Story 5 - Garantir privacidade e manter respostas em tempo útil com históricos longos (Priority: P3)

**Goal**: Nenhum segredo (chave de API, senha) é persistido nem enviado como contexto; com conversas longas, o contexto ao provedor é limitado à janela configurável (padrão 20), o histórico completo permanece visível e a interface informa sutilmente quando mensagens antigas ficaram fora do contexto.

**Independent Test**: Conversa longa (muitas trocas) responde em tempo útil com janela ativa, histórico antigo segue visível e nada de chave/senha aparece no histórico persistido (SC-004/SC-005).

### Testes para User Story 5 ⚠️

> **NOTA**: Escrever estes testes PRIMEIRO, garantir que FALHAM antes da implementação

- [X] T045 [P] [US5] Ajustar `backend/DatabaseDiagram.Testes/Servicos/ServicoDeConversasTestes.cs` — a janela de contexto seleciona as **últimas N mensagens** (N = `Assistente:QuantidadeMaximaDeMensagensDeContexto`, padrão 20) e sinaliza truncamento quando omitir mensagens antigas; a mensagem atual é sempre incluída; a sanitização (D8) substitui padrões típicos de segredo (`sk-...`, `Bearer ...`, `key=...`) por `"<segredo removido>"` tanto na persistência quanto no contexto; nenhuma `chaveDeApi`/`senha` presente em mensagem persistida (SC-005)
- [X] T046 [P] [US5] Ajustar `frontend/testes/PaginaDoAssistente.test.tsx` — com header `X-Contexto-Truncado: true`, a interface informa de forma sutil que somente as mensagens mais recentes foram usadas como contexto; todas as mensagens anteriores permanecem visíveis mesmo fora da janela; nenhuma chave de API é exibida em tela (FR-011/aceitações 2 e 4 da US5)

### Implementação para User Story 5

- [X] T047 [US5] Garantir no `backend/DatabaseDiagram.Api/Servicos/ServicoDeConversas.cs` que a janela de contexto respeita `Assistente:QuantidadeMaximaDeMensagensDeContexto` (padrão 20, configurável) omitindo as mensagens mais antigas além do limite e que o truncamento é refletido na flag usada pelo header `X-Contexto-Truncado` do `AssistenteControlador` (FR-010/SC-004)
- [X] T048 [US5] Garantir no `backend/DatabaseDiagram.Api/Servicos/ServicoDeConversas.cs` que a sanitização leve (D8) é aplicada ao gravar a mensagem do usuário e ao montar o contexto (padrões `sk-...`, `Bearer ...`, `key=...` → `"<segredo removido>"`); `chaveDeApi` e `senha` continuam somente em memória por requisição, nunca no arquivo de conversa (FR-012/SC-005)
- [X] T049 [US5] Ajustar `frontend/src/paginas/PaginaDoAssistente.tsx` — exibir aviso sutil quando a resposta trouxer `X-Contexto-Truncado: true` (somente as mensagens mais recentes foram usadas como contexto), mantendo todo o histórico visível e copiável (FR-011/aceitação 4 da US5); nenhuma chave exibida

**Checkpoint**: Todas as user stories funcionam de forma independente

---

## Phase 8: Polish & Cross-Cutting Concerns

**Propósito**: Melhorias que afetam múltiplas user stories

- [X] T050 [P] Auditoria de segurança G3/SC-005: verificar que nenhuma chave de API ou senha é persistida nos arquivos `{id}.json` de conversa, logada, exibida ou retornada em resposta/testes; confirmar que o SQL gerado nunca é executado (varredura por padrões de segredo e por chamadas de execução no `ServicoDeConversas`, `AssistenteControlador` e provedores)
- [X] T051 [P] Auditoria G2: revisar identificadores, endpoints, DTOs, campos JSON (`conversaId`, `titulo`, `criadaEm`, `atualizadaEm`, `resumo`, `mensagens`, `papel`, `conteudo`), rotas (`api/conversas`) e mensagens em pt-BR (exceções técnicas autorizadas: headers HTTP e roles de provedor `user`/`assistant`/`model`)
- [X] T052 [P] Auditoria G1/G4: confirmar superfície mínima (sem repositório, CQRS, banco, fila, autenticação ou SDKs novos) e que a tradução do histórico vive dentro de cada provider — um provedor novo não exige alterar controllers, serviços comuns nem frontend
- [X] T053 Rebuild e suíte completa: `dotnet build` + `dotnet test` em `backend/` e `npm run build` + `npm test` em `frontend/` sem falhas
- [ ] T054 Validação do `quickstart.md`: executar os cenários 1–7 ponta a ponta (criar conversa; listar; continuar gerando com contexto + header `X-Contexto-Truncado`; follow-up modificando a consulta anterior; persistência entre sessões API; renomear/excluir; fluxo completo no chat com lista, restauração, avisos de contexto divergente e de truncamento) com banco MySQL e chave de teste
- [ ] T055 Validação de performance SC-004: resposta com contexto de histórico (janela) em conversas de até 30 mensagens mantém tempos comparáveis ao fluxo sem contexto (coberto pela janela de 20 mensagens, timeout de 90 s por provedor e testes automatizados com provedor falso; medição de relógio com provedor real permanece como passo manual com chave de teste — D10)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: Sem dependências — começa imediatamente
- **Foundational (Phase 2)**: Depende do Setup — BLOQUEIA todas as user stories (o CRUD e o `ServicoDeConversas` são a base de todas)
- **User Stories (Phase 3+)**: Todas dependem da fase Foundational
  - As histórias podem então avançar em sequência por prioridade (US1 P1 → US2 P1 → US3 P2 → US4 P2 → US5 P3)
- **Polish (Phase 8)**: Depende de todas as user stories desejadas concluídas

### User Story Dependencies

- **User Story 1 (P1)**: Pode começar após a fase Foundational. Entrega o motor de continuação (histórico como contexto no provedor + integração `conversaId` + persistência da troca) que as demais histórias consomem.
- **User Story 2 (P1)**: Depende da US1 e da base Foundational (restauração + continuação com o motor da US1). Inclui o aviso de contexto de banco divergente (FR-013).
- **User Story 3 (P2)**: Depende da US1 (o CRUD backend já existe na Foundational; a história entrega a lista/navegação no frontend). A mexida em `PaginaDoAssistente.tsx` é sequencial com a US2.
- **User Story 4 (P2)**: Depende das US1–US3 — renomear/excluir usam os endpoints da Foundational e completam `ListaDeConversas.tsx`/`PaginaDoAssistente.tsx`.
- **User Story 5 (P3)**: Depende das US1–US4 — validações de janela/sanitização sobre o serviço já existente e aviso de truncamento no frontend.

### Dentro de Cada User Story

- Testes (quando incluídos) DEVEM ser escritos antes e falhar antes da implementação
- Backend antes do frontend; modelos antes de serviços; serviços antes de endpoints
- Implementação central antes da integração; história completa antes de avançar para a próxima prioridade

### Parallel Opportunities

- Todas as tarefas de Setup marcadas [P] rodam em paralelo
- Todas as tarefas Foundational marcadas [P] rodam em paralelo (modelos/DTOs/testes/config são arquivos distintos de T004–T016; T006/T007/T012 são sequenciais entre si)
- Uma vez concluída a fase Foundational, as user stories podem começar em sequência por prioridade (US1 → US5)
- Todos os testes de uma história marcados [P] rodam em paralelo
- Tarefas de DTOs/modelos de uma história marcadas [P] rodam em paralelo
- Cuidado com arquivos compartilhados — tarefas sequenciais entre histórias, nunca paralelas:
  `ServicoDeConversas.cs`, `ServicoDoAssistente.cs`, `AssistenteControlador.cs`,
  `PaginaDoAssistente.tsx`, `ListaDeConversas.tsx`, `ApiAssistente.ts`,
  `AssistenteControladorTestes.cs`, `ServicoDeConversasTestes.cs`,
  `PaginaDoAssistente.test.tsx` e `ListaDeConversas.test.tsx`

---

## Parallel Example: User Story 1

```bash
# Lançar todos os testes da US1 juntos:
Task: "T017 Teste do payload com histórico no OpenAiProvedorDeIaTestes.cs"
Task: "T018 Teste do payload com histórico e merge no GoogleAiStudioProvedorDeIaTestes.cs"
Task: "T019 Teste do contexto de histórico no ServicoDoAssistenteTestes.cs"
Task: "T020 Teste do 404/409/header/persistência no AssistenteControladorTestes.cs"
Task: "T021 Teste do follow-up com conversaId em PaginaDoAssistente.test.tsx"

# Lançar a extensão de PedidoDeResposta e os tipos do frontend juntos:
Task: "T022 Extender PedidoDeResposta.cs com MensagensDaConversa"
Task: "T028 Adicionar conversaId e tipos Conversa/Mensagem em tiposDoAssistente.ts"

# Lançar a tradução dos provedores juntos (T023, T024) e depois o serviço (T025):
Task: "T023 Traduzir histórico para roles user/assistant no OpenAiProvedorDeIa.cs"
Task: "T024 Traduzir histórico com merge de roles no GoogleAiStudioProvedorDeIa.cs"
```

---

## Parallel Example: User Story 2

```bash
# Lançar os testes da US2 juntos (Teste de backend == arquivos diferentes):
Task: "T031 Teste de restauração/continuação no AssistenteControladorTestes.cs"
Task: "T032 Teste de reabrir conversa em PaginaDoAssistente.test.tsx"

# Lançar o cliente API e a restauração frontend em sequência (T033 → T034 → T035):
Task: "T033 Adicionar obterConversa em ApiAssistente.ts"
Task: "T034 Restaurar histórico e continuar em PaginaDoAssistente.tsx"
```

---

## Parallel Example: User Story 3

```bash
# Lançar os testes da US3 juntos (arquivos de teste diferentes):
Task: "T036 Teste da ListaDeConversas em ListaDeConversas.test.tsx"
Task: "T037 Teste da integração em PaginaDoAssistente.test.tsx"

# Lançar o cliente API e o componente novo juntos (T038, T039), integrando em seguida (T040):
Task: "T038 Criar ListaDeConversas.tsx"
Task: "T039 Adicionar listarConversas e criarConversa em ApiAssistente.ts"
```

---

## Parallel Example: User Story 4

```bash
# Lançar o teste e o cliente API juntos (arquivos diferentes):
Task: "T041 Teste de renomear/excluir em ListaDeConversas.test.tsx"
Task: "T042 Adicionar renomearConversa e excluirConversa em ApiAssistente.ts"
```

---

## Parallel Example: User Story 5

```bash
# Lançar os testes da US5 juntos (arquivos de teste diferentes):
Task: "T045 Teste de janela/sanitização/versão em ServicoDeConversasTestes.cs"
Task: "T046 Teste do aviso de truncamento em PaginaDoAssistente.test.tsx"
```

---

## Implementation Strategy

### MVP First (User Story 1 + 2 — núcleo da persistência)

1. Completar a Phase 1: Setup
2. Completar a Phase 2: Foundational (CRITICAL — bloqueia todas as histórias: modelos, `ServicoDeConversas`, CRUD de conversas e config)
3. Completar a Phase 3: User Story 1 (continuação com contexto + persistência da troca)
4. **PARAR e VALIDAR**: testar a User Story 1 isoladamente
5. Completar a Phase 4: User Story 2 (restauração entre sessões + aviso de banco divergente)
6. Deploy/demo se pronto

### Entrega Incremental

1. Setup + Foundational → base de persistência pronta (CRUD via curl)
2. User Story 1 → continuar conversa com contexto → MVP parcial (continuação e persistência no backend)
3. User Story 2 → restaurar e continuar após reinício → MVP completo (persistência ponta a ponta)
4. User Story 3 → lista/navegação de conversas → histórico utilizável
5. User Story 4 → renomear/excluir → controle do histórico
6. User Story 5 → aviso de truncamento + garantias de privacidade → prontidão para uso real
7. Cada história adiciona valor sem quebrar as anteriores

### Estratégia paralela por equipe

Com múltiplos desenvolvedores:

1. Equipe completa Setup + Foundational juntos
2. Após o Foundational — em sequência de prioridade:
   - Desenvolvedor A: US1 (motor de continuação + persistência da troca)
   - Desenvolvedor B: aguarda a US1 e segue US2 (restauração) e depois US3/US4 (lista, renomear, excluir)
   - Desenvolvedor C: US5 após US1–US4 (janela/sanitização verificadas + aviso de truncamento)
3. Evitar conflitos nos arquivos compartilhados listados na seção "Parallel Opportunities"

---

## Notes

- [P] tarefas = arquivos diferentes, sem dependências
- A persistência backend (modelos + `ServicoDeConversas` + `ConversasControlador` + DTOs) pertence à fase Foundational porque todas as histórias dependem dela; as histórias entregam: US1 o motor de contexto, US2 a restauração, US3/US4 a lista/navegação e US5 as garantias de privacidade/perf
- Constraints verbatim do `data-model.md` citadas nas descrições: "título é derivado de um trecho da mensagem (até ~40 caracteres, resumido com "…")", "versao ... hoje 1 (FR-016)", "padrão 20 mensagens" da janela e regras de validação de título (1–100)
- Exceções técnicas de protocolo autorizadas (G2): header `X-Contexto-Truncado` e roles de provedor `user`/`assistant`/`model`
- Provedor real de IA nunca é chamado nos testes; sempre substituído por falso (D10)
- Commit após cada tarefa ou grupo lógico; mensagens em pt-BR
- Parar em cada checkpoint para validar a história isoladamente
- Evitar: tarefas vagas, conflitos no mesmo arquivo, dependências entre histórias que quebrem a independência