---

description: "Task list for filtro de relevância de tabelas na introspecção MySQL"
---

# Tasks: Filtro de Relevância de Tabelas na Introspecção MySQL

**Input**: Design documents from `/specs/006-mysql-table-relevance-filter/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, quickstart.md

**Tests**: A spec já possui suíte de testes do provider (`MySqlProvedorDeSchemaTestes.cs`); tarefas de teste estão incluídas porque a spec pede explicitamente cobertura para filtro, ordenação, limite e preservação de colunas/relacionamentos.

**Organization**: Tarefas agrupadas por user story (spec.md). Como a mudança é inteiramente confinada a um único arquivo de produção (`MySqlProvedorDeSchema.cs`) e um único arquivo de teste (`MySqlProvedorDeSchemaTestes.cs`), a maioria das tarefas dentro de uma mesma fase **não** é paralelizável entre si (mesmo arquivo); a paralelização real acontece apenas entre o arquivo de produção e o arquivo de teste quando não há dependência de código ainda não escrito.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Pode rodar em paralelo (arquivos diferentes, sem dependência)
- **[Story]**: A qual user story a tarefa pertence (US1, US2, US3)
- Caminhos de arquivo exatos em cada descrição

## Path Conventions

Aplicação web existente: `backend/DatabaseDiagram.Api/` (produção) e `backend/DatabaseDiagram.Testes/` (testes), conforme `plan.md` → Project Structure.

---

## Phase 1: Setup

**Purpose**: Nenhuma inicialização de projeto é necessária — provider, projeto e testes já existem. Apenas confirmar baseline antes de alterar.

- [X] T001 Rodar `dotnet build` e `dotnet test DatabaseDiagram.Testes --filter MySqlProvedorDeSchemaTestes` a partir de `backend/` para confirmar que a suíte atual passa antes de qualquer alteração (baseline).

**Checkpoint**: Build e testes existentes passam antes da mudança.

---

## Phase 2: Foundational

**Purpose**: Não há infraestrutura bloqueante nova (constituição I — nenhuma abstração nova). Esta fase é um no-op formal; a Fase 1 já cobre o baseline necessário para todas as user stories começarem.

**Checkpoint**: Nenhuma tarefa adicional — segue direto para as user stories.

---

## Phase 3: User Story 1 - Diagrama sem ruído de tabelas vazias (Priority: P1) 🎯 MVP

**Goal**: Tabelas com `TABLE_ROWS <= 10` (estimativa) deixam de aparecer no schema retornado; tabelas com `TABLE_ROWS > 10` continuam aparecendo normalmente.

**Independent Test**: Conectar a um banco com tabelas de ambos os grupos e verificar, via `ConsultaDeTabelas` e/ou teste de introspecção, que somente as com `TABLE_ROWS > 10` aparecem no resultado.

### Tests for User Story 1 ⚠️

> Escrever estes testes primeiro; devem falhar contra a consulta atual (sem filtro por `TABLE_ROWS`) antes da implementação.

- [X] T002 [P] [US1] Em `backend/DatabaseDiagram.Testes/Provedores/MySqlProvedorDeSchemaTestes.cs`, adicionar teste `ConsultaDeTabelas_FiltraPorQuantidadeDeRegistros` que verifica que `MySqlProvedorDeSchema.ConsultaDeTabelas` contém a condição `TABLE_ROWS > 10` (equivalente ao FR-001/FR-002: relevância baseada na estimativa de registros já disponível no metadado, sem `COUNT(*)`).
- [X] T003 [P] [US1] No mesmo arquivo, adicionar teste garantindo que nenhuma das constantes de consulta (`ConsultaDeTabelas`, `ConsultaDeColunas`, `ConsultaDeChavesPrimarias`, `ConsultaDeChavesEstrangeiras`) contém `COUNT(` (reforça FR-002/FR-007: nenhuma contagem exata por tabela é introduzida) — implementado como novo teste parametrizado `Consultas_NaoExecutamContagemPorTabela`, ao lado do `Consultas_SaoDeLeituraDeMetadados` existente.

### Implementation for User Story 1

- [X] T004 [US1] Em `backend/DatabaseDiagram.Api/Provedores/MySql/MySqlProvedorDeSchema.cs`, alterar a constante `ConsultaDeTabelas` para adicionar a condição `AND TABLE_ROWS > 10` no `WHERE`, mantendo `TABLE_SCHEMA = @bancoDeDados AND TABLE_TYPE = 'BASE TABLE'` e o `LIMIT 501` (`LimiteDeConsultaDeTabelas`) existentes. **Desvio deliberado da descrição original**: `TABLE_ROWS` NÃO foi adicionado ao `SELECT`/mapeado no `MySqlDataReader` (ver nota em T006) — usado apenas em `WHERE`/`ORDER BY` no próprio SQL.
- [X] T005 [US1] Acima da constante `ConsultaDeTabelas` alterada, adicionar comentário documentando que `TABLE_ROWS` é uma **estimativa** (especialmente para tabelas InnoDB) e não deve ser tratado como `COUNT(*)` exato (requisito explícito da spec, seção "Documentar a limitação").
- [X] T006 [US1] **Não aplicável / não implementado por simplicidade**: como `TABLE_ROWS` só é usado no `WHERE`/`ORDER BY` da própria consulta SQL (T004/T011), não há necessidade de lê-lo no `MySqlDataReader` nem de mantê-lo como variável local em `ObterTabelasAsync` — a ordenação já vem pronta do banco. Ler e descartar o valor em C# seria uma variável sem uso real, o que viola a constituição I (nenhuma abstração/código sem necessidade demonstrada). `TabelaDeBanco` permanece inalterado, conforme data-model.md.
- [X] T007 [US1] Rodar `dotnet test DatabaseDiagram.Testes --filter MySqlProvedorDeSchemaTestes` a partir de `backend/` e confirmar que T002 e T003 agora passam, e que nenhum teste existente quebrou. Resultado: 24/24 testes do provider passando (18 pré-existentes + 6 novos/estendidos).

**Checkpoint**: Tabelas irrelevantes (`TABLE_ROWS <= 10`) já não são selecionadas pela consulta; User Story 1 funcional e testável isoladamente.

---

## Phase 4: User Story 2 - Priorização das tabelas mais relevantes quando há muitas tabelas com dados (Priority: P2)

**Goal**: Quando o número de tabelas relevantes excede o limite de segurança (500), as tabelas com maior `TABLE_ROWS` são priorizadas e o aviso de truncamento continua sendo emitido.

**Independent Test**: Simular/verificar que a consulta ordena por `TABLE_ROWS DESC` antes do `LIMIT`, e que o corte para 500 + `EsquemaDeBanco.Aviso` continuam funcionando exatamente como hoje (apenas com critério de corte diferente).

### Tests for User Story 2 ⚠️

- [X] T008 [P] [US2] Em `MySqlProvedorDeSchemaTestes.cs`, adicionar teste `ConsultaDeTabelas_OrdenaPorQuantidadeDeRegistrosDecrescente` verificando que `MySqlProvedorDeSchema.ConsultaDeTabelas` contém `ORDER BY TABLE_ROWS DESC` (substituindo/atualizando a antiga ordenação por nome, se houver teste equivalente).
- [X] T009 [P] [US2] No mesmo arquivo, o teste existente `ConsultaDeTabelas_LimitaQuantidadeDeTabelas` (linha ~182) já valida `Assert.Contains("LIMIT", ...)`, continuando a passar sem alteração após a mudança de filtro/ordenação (não-regressão do limite de segurança, FR-003/FR-004).
- [X] T010 [US2] O teste existente `CriarAvisoDeTruncamento_DescreveOLimiteDeTabelas` já cobre a geração do aviso de truncamento (500 tabelas / `LimiteDeTabelasParaIntrospeccao`); atualizado para refletir a nova redação da mensagem ("quantidade estimada de registros" em vez de "ordem alfabética"), já que o corte em `ObterEsquemaAsync` (`tabelas.Take(LimiteDeTabelasParaIntrospeccao)`) opera sobre a lista já ordenada por `TABLE_ROWS DESC` retornada pelo SQL, sem necessidade de lógica nova em C#.

### Implementation for User Story 2

- [X] T011 [US2] Em `ConsultaDeTabelas` (mesma constante alterada em T004), trocado `ORDER BY TABLE_NAME` por `ORDER BY TABLE_ROWS DESC`, mantendo `LIMIT 501` logo em seguida.
- [X] T012 [US2] Revisado `ObterEsquemaAsync` e o corte por `LimiteDeTabelasParaIntrospeccao`/`introspeccaoTruncada`: nenhuma alteração de lógica foi necessária (o corte já opera sobre a lista ordenada por relevância vinda do SQL). Ajustada a mensagem de `CriarAvisoDeTruncamento` para não mais mencionar "ordem alfabética" e sim "quantidade estimada de registros".
- [X] T013 [US2] Rodar `dotnet test DatabaseDiagram.Testes --filter MySqlProvedorDeSchemaTestes` a partir de `backend/` e confirmar que T008–T010 passam e nada quebrou. Resultado: 24/24 passando.

**Checkpoint**: Ordenação por relevância e limite de segurança funcionando juntos; User Stories 1 e 2 funcionais.

---

## Phase 5: User Story 3 - Relacionamentos continuam corretos após o filtro (Priority: P1)

**Goal**: Colunas, chave primária e chaves estrangeiras das tabelas relevantes continuam corretamente detectadas após o filtro/ordenação aplicados nas US1/US2 (não-regressão).

**Independent Test**: Para tabelas relevantes com PK e FK entre si, verificar que `EsquemaDeBanco` montado (via `MontarEsquema`) continua marcando `ChavePrimaria`/`ChaveEstrangeira` e listando `Relacionamentos` corretamente.

### Tests for User Story 3 ⚠️

- [X] T014 [P] [US3] Revisados os testes existentes `MontarEsquema_ComMetadados_ConstroiSchemaComFlagsDeChaves` (linha ~49) e `Chaves_SaoCompostasPorEsquemaENome` (linha ~216): confirmado que cobrem PK e FK sem depender de `TABLE_ROWS` (essas funções recebem apenas tabelas/colunas/chaves já selecionadas) — nenhuma alteração necessária, testes continuam válidos e passando após a mudança.
- [X] T015 [P] [US3] Confirmado (via os testes existentes `ConsultaDeColunas_FiltraPorTabela`, `ConsultaDeChavesPrimarias_FiltraPorTabela` e `ConsultaDeChavesEstrangeiras_FiltraPorTabela`, mais o novo `Consultas_NaoExecutamContagemPorTabela` de T003) que `ConsultaDeColunas`, `ConsultaDeChavesPrimarias` e `ConsultaDeChavesEstrangeiras` permanecem com o mesmo texto/estrutura de antes — o filtro de relevância não interferiu nessas consultas por-tabela (FR-005).

### Implementation for User Story 3

- [X] T016 [US3] Revisado `ObterColunasDeTabelaAsync`, `ObterChavesPrimariasDeTabelaAsync`, `ObterRelacionamentosDeTabelaAsync` e `MontarEsquema` em `MySqlProvedorDeSchema.cs`: nenhuma alteração de código foi necessária — continuam recebendo a lista já filtrada/ordenada/cortada de `tabelas` sem qualquer ajuste (não-regressão de FR-005/FR-006).
- [X] T017 [US3] Rodar `dotnet test DatabaseDiagram.Testes --filter MySqlProvedorDeSchemaTestes` a partir de `backend/` e confirmar que T014–T015 passam e que a suíte completa (incluindo US1/US2) continua verde. Resultado: 24/24 passando.

**Checkpoint**: Todas as três user stories funcionando em conjunto; PKs/FKs preservadas para as tabelas relevantes selecionadas.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Validação final de ponta a ponta e conformidade com a constituição.

- [X] T018 Rodar `dotnet build` a partir de `backend/` e confirmar compilação sem warnings novos. Resultado: Build succeeded, 0 Warning(s), 0 Error(s).
- [X] T019 Rodar `dotnet test DatabaseDiagram.Testes` (suíte completa, sem filtro) a partir de `backend/` e confirmar que nada fora do provider MySQL foi afetado. Resultado: 133/133 passando.
- [X] T020 Validado o roteiro de `specs/006-mysql-table-relevance-filter/quickstart.md` na parte automatizada (testes de unidade cobrindo filtro/ordenação/limite/PK-FK, itens 1–4); a parte manual (conexão a um MySQL real) fica documentada como próximo passo do usuário, já que não há banco MySQL disponível neste ambiente de execução dos testes.
- [X] T021 [P] Confirmado via `grep` que nenhum campo `TABLE_ROWS`/`DATA_LENGTH`/`INDEX_LENGTH` foi adicionado a `TabelaDeBanco`, `EsquemaDeBanco` ou qualquer outro arquivo fora de `Provedores/MySql` (conformidade com data-model.md e constituição I).

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: Sem dependências — roda imediatamente.
- **Foundational (Phase 2)**: No-op; não bloqueia nada além do que Setup já cobre.
- **User Story 1 (Phase 3)**: Depende de Setup. É a base — introduz o filtro `TABLE_ROWS > 10` na mesma constante de consulta que as demais stories tocam.
- **User Story 2 (Phase 4)**: Depende de T004 (a constante `ConsultaDeTabelas` já alterada pela US1) — não pode ser paralelizada com a implementação da US1 porque edita o mesmo trecho de código (a ordenação é adicionada à mesma query).
- **User Story 3 (Phase 5)**: Depende logicamente de US1+US2 estarem implementadas (para validar não-regressão sobre o resultado já filtrado/ordenado), mas não edita código de produção novo — pode ser feita em paralelo com a Fase 4 do ponto de vista de arquivos, exceto pelo arquivo de teste compartilhado.
- **Polish (Phase 6)**: Depende de todas as user stories completas.

### User Story Dependencies

- **US1 (P1)**: Sem dependência de outra story; é o MVP.
- **US2 (P2)**: Depende da alteração de `ConsultaDeTabelas` feita em US1 (mesma constante/mesmo arquivo) — sequencial na prática, mesmo sendo conceitualmente independente.
- **US3 (P1 — não-regressão)**: Não depende de código novo de US1/US2, mas sua validação faz mais sentido depois que o filtro e a ordenação já estão implementados.

### Within Each User Story

- Testes (T002/T003, T008–T010, T014/T015) devem ser escritos e falhar antes da implementação correspondente.
- Implementação de consulta (T004–T006, T011–T012) antes de rodar os testes de confirmação (T007, T013).
- Story completa (testes verdes) antes de avançar para a próxima prioridade.

### Parallel Opportunities

- T002 e T003 (US1, mesmo arquivo de teste) podem ser escritas em sequência rápida pela mesma pessoa, mas tecnicamente são independentes entre si — marcadas `[P]` por não dependerem uma da outra, embora estejam no mesmo arquivo (aplicar com cautela se for automação multi-agente: preferir sequencial no mesmo arquivo para evitar conflito de merge).
- T008 e T009 (US2) e T014/T015 (US3): mesma observação acima.
- T021 (Polish) pode rodar em paralelo com T020, pois é apenas revisão de código sem dependência de execução.
- Não há paralelismo real entre US1 e US2 na implementação (T004 vs. T011), pois ambas alteram a mesma constante `ConsultaDeTabelas` no mesmo arquivo.

---

## Parallel Example: User Story 1

```bash
# Testes da User Story 1 podem ser redigidos em paralelo (mesmo arquivo, mas sem dependência entre si):
Task: "Adicionar teste ConsultaDeTabelas_FiltraPorQuantidadeDeRegistros em backend/DatabaseDiagram.Testes/Provedores/MySqlProvedorDeSchemaTestes.cs"
Task: "Adicionar/estender teste garantindo ausência de COUNT( nas consultas em backend/DatabaseDiagram.Testes/Provedores/MySqlProvedorDeSchemaTestes.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Completar Phase 1: Setup (baseline).
2. Phase 2: Foundational é no-op.
3. Completar Phase 3: User Story 1 (filtro `TABLE_ROWS > 10`).
4. **PARAR e VALIDAR**: rodar os testes do provider e confirmar que tabelas com `TABLE_ROWS <= 10` somem do resultado, sem quebrar nada existente.
5. Este já é um incremento entregável: resolve o problema central (poluição visual) mesmo sem a priorização por volume (US2).

### Incremental Delivery

1. Setup → baseline confirmado.
2. US1 → filtro de relevância aplicado → testar isoladamente → MVP entregável.
3. US2 → ordenação por relevância antes do limite de segurança → testar isoladamente → entrega incremental.
4. US3 → confirmação de não-regressão de PK/FK/colunas → testar → entrega final.
5. Polish → build completo, suíte completa, quickstart validado.

---

## Notes

- [P] tasks = arquivos diferentes ou sem dependência de código ainda não escrito; usar com cautela quando o arquivo for o mesmo (risco de conflito de merge em execução manual).
- [Story] label mapeia cada tarefa à user story correspondente da spec.
- Toda a mudança de produção está confinada a `backend/DatabaseDiagram.Api/Provedores/MySql/MySqlProvedorDeSchema.cs` (uma única constante de consulta + comentário de documentação); nenhum novo arquivo, serviço, modelo ou abstração é criado, conforme constituição I e IV.
- Rodar os testes após cada tarefa de implementação, não só no final de cada fase.
