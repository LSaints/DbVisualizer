---

description: "Task list template for feature implementation"
---

# Tasks: Database Diagram MVP

**Input**: Design documents from `/specs/001-database-diagram-mvp/`

**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: Os testes estão incluídos porque o plan.md e a constituição (workflow
de desenvolvimento) exigem testes específicos de introspecção por provider e a
validação automatizada referenciada no quickstart.md.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Web app**: `backend/`, `frontend/` na raiz do repositório (ver plan.md)
- Identificadores e mensagens em pt-BR (constituição, princípio II)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [X] T001 Create repository structure per plan.md (backend/, frontend/, docs/, specs/)
- [X] T002 Create .NET 8 solution and ASP.NET Core API project `DatabaseDiagram.Api` in backend/
- [X] T003 Create xUnit test project `DatabaseDiagram.Testes` in backend/ and add project reference to `DatabaseDiagram.Api`
- [X] T004 [P] Add MySqlConnector NuGet package to backend/DatabaseDiagram.Api/DatabaseDiagram.Api.csproj
- [X] T005 [P] Scaffold Vite + React + TypeScript app in frontend/ (npm create vite)
- [X] T006 [P] Install React Flow and Dagre packages in frontend/ (package.json)
- [X] T007 Configure development CORS on the backend and Vite dev proxy so frontend (5173) reaches API (5000)
- [X] T008 [P] Add root .gitignore and .editorconfig

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T009 [P] Create domain models EsquemaDeBanco, TabelaDeBanco, ColunaDeBanco e RelacionamentoDeBanco in backend/DatabaseDiagram.Api/Modelos/ following data-model.md (fields and validations verbatim)
- [X] T010 [P] Create domain model ConexaoDeBanco in backend/DatabaseDiagram.Api/Modelos/ConexaoDeBanco.cs (provedor, host, porta default 3306, bancoDeDados, usuario, senha — nunca persistida, nunca logada)
- [X] T011 [P] Create DTOs RequisicaoConexao, RespostaTesteConexao e RespostaEsquema in backend/DatabaseDiagram.Api/Dtos/ matching contracts/api.md JSON (pt-BR camelCase)
- [X] T012 [P] Create TipoDeProvedor (enum com "mysql") and ConfiguradorDeConexao (connection string em memória, nunca persistida) in backend/DatabaseDiagram.Api/Infraestrutura/
- [X] T013 Create InterfaceProvedorDeSchema (`ObterSchemaAsync(ConexaoDeBanco, CancellationToken)`) and FabricaDeProvedoresDeSchema (resolve provider pelo campo provedor) in backend/DatabaseDiagram.Api/Provedores/
- [X] T014 [P] Create frontend types in frontend/src/modelos/tipos.ts mirroring data-model.md (ConexaoDeBanco, EsquemaDeBanco, TabelaDeBanco, ColunaDeBanco, RelacionamentoDeBanco)
- [X] T015 Create frontend API client in frontend/src/servicos/ApiDiagrama.ts with `testarConexao()` (POST /api/conexoes/teste) and `obterEsquema()` (POST /api/esquema) per contracts/api.md
- [X] T016 Wire Program.cs minimal pipeline in backend/DatabaseDiagram.Api/Program.cs (AddControllers, JSON camelCase pt-BR, CORS de desenvolvimento, registro da fábrica de provedores na DI)

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Conectar e visualizar o banco (Priority: P1) 🎯 MVP

**Goal**: Usuario informa conexao MySQL, testa, a aplicacao carrega o schema e renderiza o diagrama com tabelas, colunas, PKs, FKs e relacionamentos.

**Independent Test**: Conectar a um MySQL com database de exemplo e verificar (quickstart cenários 1-4 e 9) que o teste de conexão funciona, o JSON do schema é retornado conforme contracts/api.md e o diagrama renderiza tabelas/colunas/relacionamentos — e que nenhuma operação de escrita é executada.

### Tests for User Story 1 ⚠️

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [X] T017 [P] [US1] Write unit tests for MySqlProvedorDeSchema (montagem de EsquemaDeBanco a partir de metadados) in backend/DatabaseDiagram.Testes/Provedores/MySqlProvedorDeSchemaTestes.cs
- [X] T018 [P] [US1] Write unit tests for ServicoDeConexao and ServicoDeSchema in backend/DatabaseDiagram.Testes/Servicos/

### Implementation for User Story 1

- [X] T019 [US1] Implement ServicoDeConexao in backend/DatabaseDiagram.Api/Servicos/ServicoDeConexao.cs (abre MySqlConnection em memória via ConfiguradorDeConexao e retorna sucesso/falha sem expor segredos)
- [X] T020 [US1] Implement MySqlProvedorDeSchema in backend/DatabaseDiagram.Api/Provedores/MySql/MySqlProvedorDeSchema.cs with information_schema queries (schemata, tables WHERE TABLE_TYPE='BASE TABLE', columns, table_constraints + key_column_usage para PKs, key_column_usage + referential_constraints para FKs com regras ON UPDATE/ON DELETE) → EsquemaDeBanco
- [X] T021 [US1] Implement ServicoDeSchema in backend/DatabaseDiagram.Api/Servicos/ServicoDeSchema.cs (resolve provider na fábrica e retorna EsquemaDeBanco por conexão)
- [X] T022 [P] [US1] Implement ConexoesControlador (POST /api/conexoes/teste) in backend/DatabaseDiagram.Api/Controladores/ConexoesControlador.cs per contracts/api.md
- [X] T023 [US1] Implement EsquemaControlador (POST /api/esquema) in backend/DatabaseDiagram.Api/Controladores/EsquemaControlador.cs per contracts/api.md
- [X] T024 [US1] Add validation of RequisicaoConexao (host/bancoDeDados/usuario/senha obrigatorios, porta > 0, provedor suportado) with pt-BR messages and safe error handling (400/502 sem expor senha ou connection string)
- [X] T025 [P] [US1] Implement FormularioDeConexao in frontend/src/componentes/FormularioDeConexao.tsx (campos provider/host/porta/bancoDeDados/usuario/senha e botao "Testar conexao"), mensagens em pt-BR
- [X] T026 [US1] Implement application states (desconectado/conectando/carregando schema/sucesso/erro) in frontend/src/App.tsx with pt-BR messages per spec.md
- [X] T027 [P] [US1] Implement TabelaNoDiagrama custom node in frontend/src/componentes/TabelaNoDiagrama.tsx (nome, schema, colunas, indicadores PK/FK/auto incremento, handles)
- [X] T028 [US1] Implement PaginaDoDiagrama in frontend/src/paginas/PaginaDoDiagrama.tsx generating nodes and edges from EsquemaDeBanco and rendering React Flow
- [X] T029 [US1] Integrate full flow in frontend/src/App.tsx: conectar → testar → carregar → renderizar diagrama

**Checkpoint**: At this point, User Story 1 should be fully functional and testable independently (MVP completo)

---

## Phase 4: User Story 2 - Navegar e pesquisar no diagrama (Priority: P2)

**Goal**: Zoom/pan, mover tabelas, pesquisar por nome e reorganizar automaticamente o layout.

**Independent Test**: Navegar (zoom in/out/fit view), mover uma tabela, buscar por nome parcial (< 1s) e aplicar "Organizar automaticamente" sem romper relacionamentos (quickstart cenários 5-6).

### Implementation for User Story 2

- [X] T030 [US2] Enable zoom controls (in/out/fit view), pan and selection in frontend/src/paginas/PaginaDoDiagrama.tsx
- [X] T031 [US2] Allow dragging tables (React Flow node drag) in frontend/src/paginas/PaginaDoDiagrama.tsx
- [X] T032 [US2] Implement BarraDePesquisa in frontend/src/componentes/BarraDePesquisa.tsx filtering tables by name (case-insensitive substring)
- [X] T033 [US2] Implement automatic layout with Dagre in frontend/src/servicos/LayoutDoDiagrama.ts (respecting edge direction, isolated tables, no excessive overlap)
- [X] T034 [US2] Add "Organizar automaticamente" action and preserve positions when moving/hiding tables in frontend/src/paginas/PaginaDoDiagrama.tsx

**Checkpoint**: At this point, User Stories 1 AND 2 should both work independently

---

## Phase 5: User Story 3 - Focar nos relacionamentos de uma tabela (Priority: P2)

**Goal**: Selecionar uma tabela e visualizar apenas ela e suas tabelas diretamente relacionadas, com retorno ao diagrama completo.

**Independent Test**: Selecionar uma tabela, executar "Mostrar relacionamentos" → somente tabela e vizinhas diretas visíveis; "Voltar ao diagrama completo" restaura a exibição (quickstart cenário 8).

### Implementation for User Story 3

- [X] T035 [US3] Capture table selection (React Flow onNodeClick) in frontend/src/paginas/PaginaDoDiagrama.tsx
- [X] T036 [US3] Implement direct-relationship subgraph filter in frontend/src/servicos/FiltroDeRelacionamentos.ts (selected table + directly related tables + their edges)
- [X] T037 [US3] Add "Mostrar relacionamentos" and "Voltar ao diagrama completo" actions to the interface in frontend/src/paginas/PaginaDoDiagrama.tsx

**Checkpoint**: All user stories should now be independently functional

---

## Phase 6: User Story 4 - Controlar a visibilidade do diagrama (Priority: P3)

**Goal**: Ocultar e restaurar tabelas, e filtrar por schema/database quando a conexão expõe mais de um.

**Independent Test**: Ocultar uma tabela (some junto com seus edges), restaurá-la, e filtrar por schema quando disponível (quickstart cenário 7).

### Implementation for User Story 4

- [X] T038 [US4] Implement "Ocultar tabela" action that hides the table and its edges in frontend/src/paginas/PaginaDoDiagrama.tsx
- [X] T039 [US4] Implement "Restaurar tabelas ocultas" panel listing and restoring hidden tables in frontend/src/paginas/PaginaDoDiagrama.tsx
- [X] T040 [US4] Implement filter by schema/database in frontend/src/paginas/PaginaDoDiagrama.tsx when the loaded schema exposes multiple schemas

**Checkpoint**: User stories 1-4 complete and independently functional

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [X] T041 [P] Create root README.md with run instructions (backend/frontend) and project structure
- [X] T042 Review all interface and error messages are in pt-BR across backend and frontend
- [X] T043 Run quickstart.md validation end-to-end (cenários 1-9) and confirm all expected outcomes
- [X] T044 [P] Code cleanup and lint/format configuration (editorconfig, eslint) in backend/ and frontend/
- [X] T045 Confirm "somente leitura": a thorough review and quickstart cenário 9 show no write operations to the connected database (constituição G3)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3+)**: All depend on Foundational phase completion
  - US1 → US2 (US2 usa o diagrama do US1), US2 e US3 usam o diagrama, US4 idem
  - Ordem sugerida sequencial: US1 → US2 → US3 → US4
- **Polish (Final Phase)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - No dependencies on other stories
- **User Story 2 (P2)**: Depends on US1 (diagrama renderizado); independente na navegação/pesquisa
- **User Story 3 (P2)**: Depends on US1 (seleção e edges); can be after US2
- **User Story 4 (P3)**: Depends on US1; benefits from US2/US3 but stands alone

### Within Each User Story

- Tests (included in US1) MUST be written and FAIL before implementation
- Models before services
- Services before endpoints
- Core implementation before integration
- Story complete before moving to next priority

### Parallel Opportunities

- All Setup tasks marked [P] can run in parallel
- All Foundational tasks marked [P] can run in parallel (within Phase 2)
- Once Foundational phase completes, US1 backend (T019-T024), frontend (T025-T029) and US2 (T030-T034) offer parallelization within capacity
- All tests for US1 marked [P] can run in parallel
- Models/DTOs within a phase marked [P] can run in parallel
- Different user stories can be worked on in parallel by different team members (after US1 provides the diagram render)

---

## Parallel Example: User Story 1

```bash
# Launch all tests for User Story 1 together:
Task: "Write unit tests for MySqlProvedorDeSchema in backend/DatabaseDiagram.Testes/Provedores/MySqlProvedorDeSchemaTestes.cs"
Task: "Write unit tests for ServicoDeConexao and ServicoDeSchema in backend/DatabaseDiagram.Testes/Servicos/"

# Launch backend and frontend pieces together (different files, no deps):
Task: "Implement ConexoesControlador in backend/DatabaseDiagram.Api/Controladores/ConexoesControlador.cs"
Task: "Implement FormularioDeConexao in frontend/src/componentes/FormularioDeConexao.tsx"
Task: "Implement TabelaNoDiagrama in frontend/src/componentes/TabelaNoDiagrama.tsx"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL - blocks all stories)
3. Complete Phase 3: User Story 1
4. **STOP and VALIDATE**: Test User Story 1 independently (quickstart cenários 1-4 e 9)
5. Deploy/demo if ready — **este é o MVP definido no milestone 1 do PRD**

### Incremental Delivery

1. Complete Setup + Foundational → Foundation ready
2. Add User Story 1 → Test independently → Deploy/Demo (MVP!)
3. Add User Story 2 → Test independently → Deploy/Demo
4. Add User Story 3 → Test independently → Deploy/Demo
5. Add User Story 4 → Test independently → Deploy/Demo
6. Each story adds value without breaking previous stories

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together
2. Once Foundational is done:
   - Developer A: User Story 1 backend (provider/controllers) + testes
   - Developer B: User Story 1 frontend (formulário/diagrama)
   - Developer C: pode preparar US2 (pesquisa/layout) assim que o diagrama existir
3. Stories complete and integrate independently

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Verify tests fail before implementing
- Commit after each task or logical group (mensagens de commit em pt-BR — constituição II)
- Stop at any checkpoint to validate story independently
- Avoid: vague tasks, same file conflicts, cross-story dependencies that break independence
- Identificadores, mensagens e commits em pt-BR (constituição); exceções técnicas conforme research.md