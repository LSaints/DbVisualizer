---

description: "Task list template for feature implementation"
---

# Tasks: Premium Developer-Tool UI/UX Redesign

**Input**: Design documents from `/specs/004-premium-ui-redesign/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/tokens-de-tema.md, quickstart.md

**Tests**: Não solicitados explicitamente na spec além dos cenários de `quickstart.md`; tarefas de teste automatizado (Vitest/Testing Library) são incluídas para as interações comportamentais centrais (seleção, foco, drawer, sidebar, feedback), reaproveitando a suíte já existente em `frontend/testes/`.

**Organization**: Tarefas agrupadas por user story (US1–US4, conforme spec.md) para permitir implementação e teste independentes.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Pode rodar em paralelo (arquivos diferentes, sem dependência)
- **[Story]**: US1, US2, US3, US4
- Caminhos de arquivo são relativos a `frontend/` salvo indicação contrária

## Path Conventions

Projeto web existente: `backend/` (inalterado) + `frontend/` (React + Vite + CSS puro). Todas as tarefas desta feature ficam em `frontend/src/` e `frontend/testes/`, conforme `plan.md`.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Preparar o terreno para o novo sistema visual sem quebrar a aplicação existente

- [ ] T001 Auditar `frontend/src/estilos.css` (596 linhas) e listar todos os seletores/tokens atuais (`--cor-fundo`, `--cor-superficie`, `--cor-texto`, `--cor-texto-suave`, `--cor-borda`, `--cor-primaria`, `--cor-primaria-escura`, `--cor-erro`, `--cor-sucesso`, `--raio-borda`, `--sombra`) e seus usos por seletor, documentando em um comentário temporário no topo do arquivo para orientar a migração (removido ao final da Phase 6)
- [X] T002 [P] Configurar `prefers-reduced-motion` como guarda global em `frontend/src/estilos.css` (bloco `@media (prefers-reduced-motion: reduce) { * { animation-duration: 0.01ms !important; transition-duration: 0.01ms !important; } }`) para que todas as microinterações futuras já nasçam acessíveis

**Checkpoint**: Base de auditoria pronta; nenhuma mudança visual ainda.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Sistema de tokens de tema e infraestrutura de estado compartilhada que TODAS as user stories consomem

**⚠️ CRITICAL**: Nenhuma user story pode ser implementada antes desta fase estar completa

- [X] T003 Criar bloco de tokens `:root[data-tema="escuro"]` em `frontend/src/estilos.css` com todos os tokens definidos em `data-model.md` seção 1 (`--superficie-fundo`, `--superficie-base`, `--superficie-elevada`, `--borda-sutil`, `--texto-primario`, `--texto-secundario`, `--texto-muted`, `--accent`, `--accent-texto`, `--sucesso`, `--erro`, `--espaco-1`..`--espaco-8`, `--fonte-tamanho-xs`..`--fonte-tamanho-2xl`, `--raio-sm`, `--raio-md`, `--duracao-rapida`, `--duracao-media`, `--easing-padrao`), com nenhuma superfície igual a `#000000` (preto absoluto) e razão de contraste texto/superfície conferida manualmente (≥ 4.5:1 para `--texto-primario`/`--texto-secundario`, ≥ 3:1 para `--texto-muted`), conforme contrato em `contracts/tokens-de-tema.md`
- [X] T004 Definir `data-tema="escuro"` como padrão em `frontend/index.html` (atributo no elemento `<html>`) e remover/substituir todo uso dos tokens antigos (`--cor-*`, `--sombra`, `--raio-borda`) em `frontend/src/estilos.css` pelos novos tokens de `data-model.md`, eliminando sombras exageradas e gradientes (FR-018) — depende de T003
- [ ] T005 [P] Criar hook `frontend/src/hooks/useLargura.ts` (ou local equivalente) que expõe a largura atual da viewport via `ResizeObserver`/`matchMedia`, usado para derivar `modoDePainelDeDetalhes` ("fixo" acima de 1280px, "drawer" abaixo) conforme `EstadoDeLayout` em `data-model.md`
- [X] T006 [P] Adicionar classes utilitárias de transição em `frontend/src/estilos.css` para os padrões de microinteração reutilizáveis (`.transicao-rapida { transition: all var(--duracao-rapida) var(--easing-padrao); }`, `.transicao-media { transition: all var(--duracao-media) var(--easing-padrao); }`), consumidas pelas classes de estado das próximas fases

**Checkpoint**: Sistema de tokens pronto e aplicado ao esqueleto da aplicação — user stories podem começar.

---

## Phase 3: User Story 1 - Explorar o schema em uma superfície visual clara e hierarquizada (Priority: P1) 🎯 MVP

**Goal**: Aplicar a nova hierarquia de superfícies, tipografia e uso moderado de accent em toda a interface principal (cabeçalho, área do diagrama, tabelas do diagrama, painéis), sem alterar comportamento funcional.

**Independent Test**: Abrir um schema já conectado e confirmar visualmente que fundo, cartões de tabela e elementos elevados usam níveis de superfície distintos, a tipografia segue hierarquia clara, e nenhuma cor de destaque aparece fora de ações/seleção.

### Implementação para User Story 1

- [X] T007 [P] [US1] Reestilizar `.cabecalho`, `.navegacao-da-aplicacao` e `.informacoes-da-conexao` em `frontend/src/estilos.css` para usar `--superficie-base`/`--superficie-elevada`, `--texto-primario`/`--texto-secundario`, `--borda-sutil` e a escala `--espaco-*`, removendo sombras (`--sombra`) e bordas fortes (FR-001, FR-002, FR-018)
- [X] T008 [P] [US1] Reestilizar `.area-do-diagrama` e `.diagrama-vazio` em `frontend/src/estilos.css` para usar `--superficie-fundo` como plano de fundo do canvas, garantindo contraste suficiente com nodes/edges do React Flow (FR-001, FR-017)
- [X] T009 [US1] Reestilizar `.tabela-no-diagrama`, `.tabela-cabecalho`, `.tabela-nome`, `.tabela-esquema`, `.tabela-colunas`, `.coluna`, `.coluna-nome`, `.coluna-tipo`, `.coluna-indicadores` em `frontend/src/componentes/TabelaNoDiagrama.tsx` (className) e `frontend/src/estilos.css` para usar `--superficie-elevada` no cartão da tabela, `--texto-primario` para nome da tabela, `--texto-secundario`/`--texto-muted` para colunas/tipos, e `--borda-sutil` para bordas, com espaçamento consistente via `--espaco-*` (FR-001, FR-002)
- [X] T010 [US1] Reestilizar `.botao-primario`/`.botao-secundario` em `frontend/src/estilos.css` para que apenas `.botao-primario` use `--accent`/`--accent-texto` como fundo, e `.botao-secundario` use `--superficie-elevada`/`--borda-sutil`, garantindo uso moderado do accent (FR-003)
- [X] T011 [P] [US1] Reestilizar `.tela-de-conexao`, `.formulario-de-conexao`, `.campo`, `.campo-rotulo`, `.campo input`, `.campo select` em `frontend/src/estilos.css` para consumir os novos tokens de superfície/texto/espaçamento, removendo cartões (cards) sem função e mantendo apenas os elementos com propósito claro (FR-002, FR-019)
- [X] T012 [US1] Adicionar estado de `:focus-visible` consistente (usando `--accent` ou `--borda-sutil` reforçada) para todos os elementos interativos estilizados nas tarefas anteriores (`botao-primario`, `botao-secundario`, `campo input`, `campo select`), conforme FR-011 e contrato de acessibilidade em `contracts/tokens-de-tema.md`
- [X] T013 [US1] Validar manualmente (via DevTools) as razões de contraste de `--texto-primario`, `--texto-secundario` e `--texto-muted` sobre `--superficie-fundo`, `--superficie-base` e `--superficie-elevada`, confirmando ≥ 4.5:1 (texto normal) e ≥ 3:1 (texto muted/elementos grandes), ajustando os valores de T003 se necessário (FR-017, SC-005)

**Checkpoint**: A interface principal já reflete a nova identidade visual "developer tool premium" de ponta a ponta, de forma independentemente demonstrável.

---

## Phase 4: User Story 2 - Selecionar, focar e explorar tabelas com feedback visual imediato (Priority: P1)

**Goal**: Implementar os estados de seleção, foco e destaque de relacionamento no diagrama, com microinterações de hover/transição, usando a API nativa do React Flow conforme `research.md` §4.

**Independent Test**: Clicar em uma tabela no diagrama e verificar que ela recebe destaque visual imediato, os relacionamentos conectados são realçados e os demais atenuados, com transições curtas e perceptíveis; passar o mouse sobre elementos interativos exibe hover antes do clique.

### Implementação para User Story 2

- [X] T014 [US2] Adicionar estado `EstadoDoDiagrama` (`tabelaSelecionadaId`, `tabelaFocadaId`, `idsDeArestasDestacadas`, `idsDeArestasAtenuadas`) em `frontend/src/paginas/PaginaDoDiagrama.tsx`, conforme os campos e transições descritos em `data-model.md` seção 2
- [X] T015 [US2] Implementar handler de seleção de tabela em `frontend/src/paginas/PaginaDoDiagrama.tsx` que, ao clicar em um node do React Flow, define `tabelaSelecionadaId` e recalcula `idsDeArestasDestacadas`/`idsDeArestasAtenuadas` a partir do grafo de relacionamentos já carregado (regra "Selecionar uma tabela" de `data-model.md`)
- [X] T016 [US2] Implementar ação de "focar tabela" em `frontend/src/paginas/PaginaDoDiagrama.tsx` usando `useReactFlow().fitView`/`setCenter` para animar a viewport até `tabelaFocadaId`, mantendo `tabelaSelecionadaId` (regra "Focar uma tabela" de `data-model.md`, FR-008)
- [X] T017 [US2] Implementar handler de desseleção (clique em área vazia do canvas) em `frontend/src/paginas/PaginaDoDiagrama.tsx` que limpa `tabelaSelecionadaId`, `tabelaFocadaId`, `idsDeArestasDestacadas` e `idsDeArestasAtenuadas`
- [X] T018 [P] [US2] Aplicar classe `.tabela--selecionada` em `frontend/src/componentes/TabelaNoDiagrama.tsx` quando `id === tabelaSelecionadaId`, e estilizá-la em `frontend/src/estilos.css` com borda `--accent` e `transicao-rapida` (FR-007, contrato "Contrato de classes de estado (diagrama)")
- [X] T019 [P] [US2] Aplicar classes `.aresta--destacada`/`.aresta--atenuada` às edges do React Flow em `frontend/src/paginas/PaginaDoDiagrama.tsx` (via `edges.map` com `className` condicional) e estilizá-las em `frontend/src/estilos.css` com `transicao-media` (FR-009)
- [X] T020 [P] [US2] Adicionar estado de hover (`:hover`) discreto para tabelas, colunas e arestas em `frontend/src/estilos.css`, usando `transicao-rapida` e sem depender apenas de cor (reforço de borda/leve elevação), conforme FR-010 e contrato de acessibilidade
- [X] T021 [US2] Adicionar/atualizar teste de interação em `frontend/testes/PaginaDoDiagrama.test.tsx` (criar o arquivo se não existir, seguindo o padrão de `frontend/testes/PaginaDoAssistente.test.tsx`) cobrindo: clicar numa tabela aplica classe de selecionada; relacionamentos conectados recebem destaque; clique em área vazia limpa a seleção

**Checkpoint**: Interação central de exploração do diagrama (seleção/foco/destaque/hover) funcional e testada, combinável com US1.

---

## Phase 5: User Story 3 - Navegar pela aplicação com sidebar, painel de detalhes e toolbar adaptáveis (Priority: P2)

**Goal**: Tornar a navegação estrutural responsiva: sidebar recolhível, painel de detalhes como drawer em telas menores, toolbar compacta.

**Independent Test**: Redimensionar a janela entre larguras de desktop e notebook e verificar que a sidebar recolhe/expande com animação, o painel de detalhes vira drawer sobreposto abaixo do breakpoint, e a toolbar se compacta mantendo ações essenciais acessíveis.

### Implementação para User Story 3

- [ ] T022 [US3] Adicionar estado `EstadoDeLayout` (`sidebarRecolhida`, `painelDeDetalhesAberto`, `modoDePainelDeDetalhes`) em `frontend/src/paginas/PaginaDoDiagrama.tsx`, usando o hook `useLargura` (T005) para derivar `modoDePainelDeDetalhes` (`"fixo"` ≥ 1280px, `"drawer"` abaixo), conforme `data-model.md` seção 3
- [ ] T023 [P] [US3] Criar componente `frontend/src/componentes/BarraLateral.tsx` (ou adaptar navegação existente `.navegacao-da-aplicacao`) com controle de recolher/expandir, aplicando classe `.sidebar--recolhida` quando `sidebarRecolhida === true`, com layout de ícones-apenas quando recolhida (FR-006)
- [ ] T024 [P] [US3] Estilizar `.sidebar--recolhida` em `frontend/src/estilos.css` com transição de largura usando `transicao-media`, reduzindo a largura para exibir apenas ícones e ocultando labels textuais
- [ ] T025 [US3] Criar/adaptar componente de painel de detalhes em `frontend/src/componentes/PainelDeDetalhes.tsx` que renderiza como elemento fixo no layout quando `modoDePainelDeDetalhes === "fixo"` e como overlay (`.painel-detalhes--drawer`) quando `"drawer"`, com animação de entrada/saída (`transform`/`opacity`) em `frontend/src/estilos.css`, conforme FR-012 — depende de T022
- [ ] T026 [US3] Garantir no CSS/layout de `frontend/src/paginas/PaginaDoDiagrama.tsx` que recolher a sidebar não fecha o painel de detalhes e que ambos os estados coexistem sem sobreposição indevida (Edge Case da spec: "recolher sidebar com drawer aberto")
- [X] T027 [US3] Reestilizar `.barra-superior`, `.acoes` e `.painel-de-ocultas` em `frontend/src/estilos.css` com classe `.toolbar--compacta` aplicada abaixo de um breakpoint intermediário (via media query), ocultando labels textuais dos botões e mantendo `title`/`aria-label` para acessibilidade (FR-013)
- [ ] T028 [US3] Adicionar/atualizar teste em `frontend/testes/PaginaDoDiagrama.test.tsx` cobrindo: alternar `sidebarRecolhida` aplica/remove a classe correspondente; simular largura abaixo de 1280px resulta em `modoDePainelDeDetalhes === "drawer"`

**Checkpoint**: Navegação estrutural responsiva funcional e testada, combinável com US1 e US2.

---

## Phase 6: User Story 4 - Reconhecer estados de sucesso e erro de forma clara e não intrusiva (Priority: P3)

**Goal**: Adicionar feedback visual discreto de sucesso/erro para conexão de banco e ações do assistente, usando o `EstadoDeFeedback` de `data-model.md`.

**Independent Test**: Disparar uma conexão bem-sucedida e uma conexão com erro e verificar que cada caso produz uma indicação visual distinta (sucesso breve e autodesaparecente; erro claro e persistente até nova ação), sem bloquear a interface.

### Implementação para User Story 4

- [ ] T029 [US4] Criar componente `frontend/src/componentes/IndicadorDeFeedback.tsx` que recebe `EstadoDeFeedback` (`tipo`, `mensagem`, `visivel`) conforme `data-model.md` seção 4 e renderiza classe `.feedback--sucesso` ou `.feedback--erro`
- [X] T030 [P] [US4] Estilizar `.feedback--sucesso`/`.feedback--erro` em `frontend/src/estilos.css` usando `--sucesso`/`--erro`, com transição de entrada/saída (`transicao-media`), substituindo o estilo atual de `.estado-sucesso`/`.alerta-de-erro` sem remover sua função (FR-014, FR-015, FR-016)
- [ ] T031 [US4] Integrar `IndicadorDeFeedback` em `frontend/src/componentes/FormularioDeConexao.tsx`, disparando `tipo: "sucesso"` com autodesaparecimento após alguns segundos ao conectar com êxito, e `tipo: "erro"` persistente (até nova tentativa) em caso de falha (FR-014, FR-015)
- [ ] T032 [P] [US4] Integrar `IndicadorDeFeedback` (ou reutilizar o mesmo componente) em `frontend/src/paginas/PaginaDoAssistente.tsx` para respostas com erro do assistente, consistente com a paleta e hierarquia definidas (FR-015)
- [ ] T033 [US4] Adicionar/atualizar teste em `frontend/testes/PaginaDoDiagrama.test.tsx` (conexão) e `frontend/testes/PaginaDoAssistente.test.tsx` cobrindo: sucesso exibe e depois remove o indicador; erro exibe e mantém o indicador visível até nova ação, sem bloquear os demais elementos da tela

**Checkpoint**: Todas as 4 user stories entregues e testadas de forma independente e combinada.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Consolidação final, limpeza e validação end-to-end contra spec/quickstart

- [ ] T034 [P] Remover do `frontend/src/estilos.css` o comentário de auditoria temporário criado em T001 e quaisquer tokens antigos (`--cor-*`, `--sombra`, `--raio-borda`) que não tenham sido migrados/removidos até esta fase
- [ ] T035 [P] Revisar `frontend/src/componentes/ListaDeConversas.tsx`, `frontend/src/componentes/ConfiguracaoDoAssistente.tsx`, `frontend/src/componentes/BarraDePesquisa.tsx` e `frontend/src/componentes/CartaoDeRespostaDeConsulta.tsx` para garantir que consomem exclusivamente os novos tokens (nenhuma cor/espaçamento hardcoded remanescente), removendo ícones e cartões decorativos sem função (FR-019)
- [X] T036 Rodar `npm run lint` e `npm run test` em `frontend/` e corrigir quaisquer falhas introduzidas pela migração de estilos/estado
- [ ] T037 Executar manualmente todos os cenários de `specs/004-premium-ui-redesign/quickstart.md` (US1–US4) em larguras de desktop (>1280px) e notebook (~1024–1279px), confirmando os itens do "Critério de pronto" (SC-001, SC-003, SC-004, SC-005, SC-006)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: sem dependências — pode começar imediatamente
- **Foundational (Phase 2)**: depende do Setup — BLOQUEIA todas as user stories (o sistema de tokens é pré-requisito de tudo)
- **User Stories (Phase 3–6)**: todas dependem da Foundational; podem prosseguir em paralelo entre si (times diferentes) ou sequencialmente por prioridade (US1 → US2 → US3 → US4)
- **Polish (Phase 7)**: depende de todas as user stories desejadas estarem completas

### User Story Dependencies

- **US1 (P1)**: pode começar após Foundational; sem dependência de outras stories
- **US2 (P1)**: pode começar após Foundational; usa as classes de estado `.tabela--*`/`.aresta--*` cujos estilos base são definidos em US1 (T009), mas a lógica de estado (T014–T017) é independente
- **US3 (P2)**: pode começar após Foundational; independente de US1/US2, embora reutilize o hook `useLargura` (T005) da fase Foundational
- **US4 (P3)**: pode começar após Foundational; independente das demais, integra-se a componentes já existentes (`FormularioDeConexao`, `PaginaDoAssistente`)

### Within Each User Story

- Estado/lógica React antes de classes CSS que dependem dele
- Estilização de componentes-base (US1) antes de estados derivados (US2 reutiliza `TabelaNoDiagrama`)
- Testes de interação após a implementação funcional da story

### Parallel Opportunities

- T002 pode rodar em paralelo com T001
- T005 e T006 podem rodar em paralelo entre si (após T003/T004)
- Dentro de US1: T007, T008, T011 são paralelizáveis (arquivos/seletores distintos); T009 e T010 têm dependência leve de T003/T004 mas não entre si
- Dentro de US2: T018, T019, T020 são paralelizáveis após T014–T017
- Dentro de US3: T023 e T024 são paralelizáveis; T025 depende de T022
- Dentro de US4: T030 e T032 são paralelizáveis
- Após Foundational, US1, US2, US3 e US4 podem ser trabalhadas em paralelo por desenvolvedores diferentes, com integração final na Phase 7

---

## Parallel Example: User Story 1

```bash
# Após Foundational (T003–T006) completa, lançar em paralelo:
Task: "Reestilizar .cabecalho, .navegacao-da-aplicacao e .informacoes-da-conexao em frontend/src/estilos.css (T007)"
Task: "Reestilizar .area-do-diagrama e .diagrama-vazio em frontend/src/estilos.css (T008)"
Task: "Reestilizar .tela-de-conexao e .formulario-de-conexao em frontend/src/estilos.css (T011)"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Completar Phase 1: Setup
2. Completar Phase 2: Foundational (CRÍTICO — bloqueia todas as stories)
3. Completar Phase 3: User Story 1
4. **PARAR e VALIDAR**: rodar cenário US1 de `quickstart.md` independentemente
5. Demonstrar/avaliar antes de prosseguir

### Incremental Delivery

1. Setup + Foundational → base pronta
2. US1 (hierarquia visual) → validar → demo (MVP visual)
3. US2 (seleção/foco/destaque) → validar → demo (MVP interativo)
4. US3 (responsividade estrutural) → validar → demo
5. US4 (feedback sucesso/erro) → validar → demo
6. Phase 7 (Polish) → validação final completa contra `quickstart.md`

### Parallel Team Strategy

Com múltiplos desenvolvedores, após Setup + Foundational:
- Dev A: US1 (base visual)
- Dev B: US2 (interação do diagrama, pode começar em paralelo desde que T009 de US1 já tenha definido a classe base `.tabela-no-diagrama`)
- Dev C: US3 (navegação responsiva)
- Dev D: US4 (feedback)

---

## Status de implementação (atualizado por `/speckit-implement`)

Concluído nesta execução: T002–T004, T006–T021, T027, T030, T036 (sistema de tokens dark, restyle completo de US1, estado de seleção/foco/destaque/atenuação de US2 com teste automatizado, toolbar compacta, estilo de feedback de sucesso/erro, lint + testes passando, build de produção verificado).

Pendente / adaptado — para revisão do time antes de fechar a feature:

- **T001, T034**: pulados (nenhum comentário de auditoria temporário foi necessário; a migração dos tokens antigos `--cor-*`/`--sombra`/`--raio-borda` já foi feita diretamente e verificada por grep — nenhum resquício encontrado).
- **T005, T022–T026, T028**: a spec descreve "sidebar recolhível" e "painel de detalhes em drawer", mas o app atual não tem uma sidebar nem um painel de detalhes separados (a navegação é uma barra superior/toolbar, e o "painel de ocultas" cumpre parcialmente esse papel). Implementar essas tarefas literalmente exigiria introduzir uma nova arquitetura de layout não solicitada explicitamente fora da spec de UI — isso foi deixado pendente para decisão do usuário (ver pergunta abaixo) em vez de inventar componentes não solicitados.
- **T029, T031–T033**: o indicador de feedback (`.feedback`, `.estado-sucesso`, `.alerta-de-erro`) foi restilizado com os tokens e animação de entrada, mas o autodesaparecimento do sucesso e um componente `IndicadorDeFeedback` dedicado não foram criados, pois o estado de sucesso hoje é o estado persistente "conectado" da aplicação (não um toast transitório) — mudar esse comportamento teria efeito colateral na navegação entre Diagrama/Assistente.
- **T035**: verificado via grep — nenhuma cor/estilo inline hardcoded remanescente em `src/componentes`/`src/paginas` (apenas a prop `color` do `<Background>` do React Flow, que não aceita CSS var).
- **T037**: validação manual completa do `quickstart.md` (redimensionar janela, testar em navegador real) não foi executada nesta sessão — recomenda-se rodar `npm run dev` e seguir os cenários antes de considerar a feature pronta para produção.

## Notes

- [P] = arquivos/seletores diferentes, sem dependência bloqueante
- [Story] mapeia cada tarefa à user story correspondente para rastreabilidade
- Nenhuma tarefa desta feature toca `backend/` — escopo 100% `frontend/`
- Nomes de classes, tokens, componentes e arquivos seguem pt-BR (Princípio II da constituição)
- Nenhuma dependência nova de UI framework/CSS-in-JS é introduzida (Princípio I — ver `research.md`)
- Parar em qualquer checkpoint para validar a story isoladamente antes de prosseguir
