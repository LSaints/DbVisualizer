---

description: "Lista de tarefas para configuração e seleção de provedores de IA no assistente"

---

# Tasks: Configuração e Seleção de Provedores de IA no Assistente

**Input**: Documentos de design em `/specs/005-ai-provider-config/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: Testes incluídos — o projeto mantém cobertura obrigatória (xUnit no
backend, Vitest + Testing Library no frontend) e o plan.md define as atualizações
nos testes existentes (a troca do `ConfiguracaoDoAssistente` pelo modal + badges
quebra a suíte atual se não for acompanhada). Provedor real de IA nunca é
chamado nos testes (substituto injetado / mock de `ApiAssistente`).

**Organization**: Tarefas agrupadas por user story para permitir implementação e
teste independentes de cada história.

## Formato: `[ID] [P?] [Story] Descrição`

- **[P]**: Pode rodar em paralelo (arquivos diferentes, sem dependências)
- **[Story]**: User story à qual a tarefa pertence (US1 a US4)
- Caminhos exatos de arquivos incluídos na descrição de cada tarefa

## Convenções de caminho

- Backend: `backend/DatabaseDiagram.Api/` e testes em `backend/DatabaseDiagram.Testes/`
- Frontend: `frontend/src/` e testes em `frontend/testes/`
- Sem projetos/camadas novos (GATE I — decisão de estrutura do plan.md: estado
  de tokens em memória no frontend, sem serviço de segredos no backend)

---

## Phase 1: Setup (Infraestrutura Compartilhada)

**Propósito**: Linha de base do repositório existente antes das mudanças

- [X] T001 [P] Verificar que a solução backend compila: executar `dotnet build` em `backend/`
- [X] T002 [P] Verificar que o frontend compila: executar `npm run build` em `frontend/`
- [X] T003 [P] Confirmar que a suíte de testes existente passa antes das mudanças: `dotnet test` em `backend/` e `npm test` em `frontend/`

---

## Phase 2: Foundational (Pré-requisitos Bloqueadores)

**Propósito**: Persistência do provedor de IA por conversa no backend (FR-012) —
modelo, serviço, DTO e controlador, com testes. DEVE estar completo antes das
user stories US2 (troca por badges) e US3 (restaurar o provedor ao abrir a
conversa), e dá significado à configuração da US1. Retorna nulo em conversas
antigas (retrocompatível com `versao` 1).

**⚠️ CRITICAL**: Nenhum trabalho de user story pode começar até esta fase terminar

- [X] T004 [P] Ajustar `backend/DatabaseDiagram.Api/Modelos/Conversa.cs` — adicionar o campo `ProvedorDeIaSelecionado` (`string?`), com comentário pt-BR: "Identificador do provedor de IA da conversa (`openai`, `google-ai-studio`, `claude`…); setado na 1ª troca persistida que usar o provedor; nulo em conversas antigas → frontend usa o padrão da sessão"; `Versao` permanece 1 (campo opcional)
- [X] T005 [P] Ajustar `backend/DatabaseDiagram.Api/Dtos/ConversaDetalhada.cs` — adicionar `public string? ProvedorDeIa { get; set; }` (contracts/api.md: único campo novo, opcional, em `GET /api/conversas/{id}`)
- [X] T006 [P] Ajustar `backend/DatabaseDiagram.Api/Servicos/ServicoDeConversas.cs` — `PersistirTrocaAsync` ganha o parâmetro `string? provedorDeIaSelecionado` que grava `conversa.ProvedorDeIaSelecionado` apenas quando não-vazio (sucesso da geração, D5); `ParaDetalhada` expõe `ProvedorDeIa = conversa.ProvedorDeIaSelecionado` (depende de T004, T005)
- [X] T007 Ajustar `backend/DatabaseDiagram.Api/Controladores/AssistenteControlador.cs` — na persistência da troca (`servicoDeConversas.PersistirTrocaAsync`), repassar `requisicao.ProvedorDeIa` (já validado como suportado em `ValidarEntrada`); sem mudança no contrato do `200` (depende de T006)
- [X] T008 [P] Ajustar `backend/DatabaseDiagram.Testes/Servicos/ServicoDeConversasTestes.cs` — `PersistirTrocaAsync` com provedor grava e `ParaDetalhada`/`ObterConversaAsync` restauram o `provedorDeIa`; trocar de provedor reescreve o valor preservando o histórico; conversa legada sem o campo persiste e restaura com `null` (retrocompatível)
- [X] T009 [P] Ajustar `backend/DatabaseDiagram.Testes/Controladores/AssistenteControladorTestes.cs` — sucesso com `conversaId` grava o `provedorDeIa` da requisição (verificável via `GET /api/conversas/{id}`); e o contrato estruturado do `200` permanece intacto
- [X] T010 [P] Ajustar `backend/DatabaseDiagram.Testes/Controladores/ConversasControladorTestes.cs` — `GET /api/conversas/{id}` devolve `provedorDeIa` quando presente e `null`/ausente em conversas sem o campo

**Checkpoint**: Backend pronto — cada conversa guarda o provedor de IA e o
expõe na abertura; testável por curl (cenários C4/C7 do quickstart.md)

---

## Phase 3: User Story 1 - Configurar os provedores de IA em um modal (Priority: P1) 🎯 MVP

**Goal**: A página do assistente expõe uma área de configurações que abre um modal onde o usuário consulta os provedores suportados, adiciona um provedor informando o token, edita o token, remove um provedor e define o padrão. Os tokens vivem somente em memória da sessão (constituição III/FR-015) — mapa `chavesPorProvedor` + `provedorPadrao` em estado React.

**Independent Test**: Abrir o Assistente, clicar em "Configurações", adicionar um provedor com token e salvar; confirmar que o token aparece apenas como "configurado" (mascarado), que o badge do provedor passa a ser oferecido no chat e que o primeiro configurado virou o padrão — sem recarregar a página e sem nenhum segredo em tela (US1/SC-002/FR-003/FR-007).

### Testes para User Story 1 ⚠️

> **NOTA**: Escrever estes testes PRIMEIRO, garantir que FALHAM antes da implementação

- [X] T011 [P] [US1] Criar `frontend/testes/ModalDeConfiguracaoDeProvedores.test.tsx` — renderizar o modal com provedores da fábrica; adicionar provedor com token salva e exibe "configurado" mascarado (nunca o valor por completo — SC-003/FR-007); editar exige digitar o novo valor (substitui); remover remove token/badge; definir padrão; token vazio bloqueia o salvar com aviso em pt-BR

### Implementação para User Story 1

- [X] T012 [P] [US1] Ajustar `frontend/src/modelos/tiposDoAssistente.ts` — adicionar os tipos de sessão: `ConfiguracaoDeSessao` (interface com `chavesPorProvedor: Record<string, string>` e `provedorPadrao: string | null`) e um tipo `ProvedorConfigurado` (`{ provedor: string; rotulo: string; tokenConfigurado: boolean }`, derivado da sessão para o modal/badges)
- [X] T013 [P] [US1] Criar `frontend/src/componentes/ModalDeConfiguracaoDeProvedores.tsx` — modal acessível (overlay + `role="dialog"`, fechar por Esc/foco no overlay) que recebe `provedores` (fábrica), `chavesPorProvedor`, `provedorPadrao` e callbacks `aoSalvarChave`, `aoRemover`, `aoDefinirPadrao`; lista provedores suportados com o token exibido como "Configurado"/"Não configurado" (nunca o valor — FR-007); formulário de adicionar/editar com `input type="password"`; regra: "o token não pode ser vazio" e "editar exige digitar o novo valor (substitui)"; avisar em pt-BR e impedir duplicar o mesmo provedor (edita o existente)
- [X] T014 [US1] Integrar o modal em `frontend/src/paginas/PaginaDoAssistente.tsx` — adicionar estado `chavesPorProvedor` (`Record<string,string>`) e `provedorPadrao` (`string | null`, inicia `null`); botão "Configurações" na `barra-superior` abre o `ModalDeConfiguracaoDeProvedores`; ao salvar a 1ª chave, definir `provedorPadrao` como esse provedor (research D3: "o primeiro provedor configurado assume o papel de padrão"); ao remover o padrão, promover o 1º restante (data-model: "remover o padrão promove o 1º restante (ou volta a nenhum padrão, orientando configuração)") (depende de T012, T013; mesmo arquivo dos próximos ajustes — executar em ordem)
- [X] T015 [P] [US1] Ajustar `frontend/testes/PaginaDoAssistente.test.tsx` — o envio de uma mensagem passa a exigir token configurado via modal: abrir "Configurações", adicionar o token de `openai`, fechar e então digitar a mensagem e enviar; substituir o `getByLabelText('Chave de API')` antigo (o componente inline deixa de existir); manter a chamada a `gerarConsulta` com `chaveDeApi` = token do provedor selecionado (depende de T014)

**Checkpoint**: User Story 1 completa e testável isoladamente — provedores configuráveis pela sessão via modal (MVP de configuração)

---

## Phase 4: User Story 2 - Selecionar e trocar o provedor no chat (Priority: P1)

**Goal**: A barra inferior do chat exibe, ao lado do `textarea` e do botão Enviar, o `SeletorDeProvedor` com badges (um por provedor configurado na sessão); o usuário troca o provedor com 1 clique, vê o selecionado destacado (FR-008/009/010); se não houver provedor configurado, envia é bloqueado e a interface orienta a configurar (FR-014).

**Independent Test**: Com 2+ provedores configurados, alternar pelo badge e ver o selecionado destacado; enviar uma mensagem com o badge `openai` ativo e confirmar que `gerarConsulta` recebe `provedorDeIa: 'openai'`; com nenhum configurado, enviar é bloqueado com aviso (SC-001/FR-008/FR-009/FR-013/FR-014).

### Testes para User Story 2 ⚠️

> **NOTA**: Escrever estes testes PRIMEIRO, garantir que FALHAM antes da implementação

- [X] T016 [P] [US2] Criar `frontend/testes/SeletorDeProvedor.test.tsx` — renderiza um badge por provedor; clique seleciona e marca `aria-pressed=true` no badge ativo; estado visual distinto; com 8 provedores todos permanecem clicáveis (SC-005); caso vazio exibe estado "configurar" (sem quebrar layout)

### Implementação para User Story 2

- [X] T017 [P] [US2] Criar `frontend/src/componentes/SeletorDeProvedor.tsx` — componente de badges de provedores (FR-008): recebe `provedoresConfigurados` (`ProvedorConfigurado[]`), `selecionado` (`string | null`) e `aoSelecionar`; cada provedor é um `<button type="button" aria-pressed>` com o rótulo; contêiner `role="group"` com flex-wrap/overflow-x ("com até 8 provedores, o textarea e o botão Enviar permanecem utilizáveis" — SC-005); sem selecionado, exibe dica "Configurar provedor" clicável (FR-014)
- [X] T018 Ajustar `frontend/src/paginas/PaginaDoAssistente.tsx` — dentro do `<form className="entrada-do-chat">`, renderizar `SeletorDeProvedor` (badges dos provedores com token salvo na sessão) posicionado junto ao `textarea` e ao botão Enviar; o badge selecionado destaca e `provedorDeIa` passa a ser a seleção do seletor; `podeEnviar` exige token do provedor selecionado ("enviar sem token → bloqueado com aviso orientado a Configurações" — FR-014, citado do data-model); `enviar` usa `chavesPorProvedor[provedorSelecionado]` como `chaveDeApi`; sem provedor configurado, exibir alerta com atalho para o modal (depende de T014 e T017; mesmo arquivo — executar em ordem)
- [X] T019 [P] [US2] Ajustar `frontend/src/estilos.css` — classes `.seletor-de-provedor`, `.badge-de-provedor`, `.badge-de-provedor-ativa` (estado selecionado com `accent`) e o layout de `.entrada-do-chat` com o seletor na parte inferior (FR-008/FR-011); sem overengineering visual (GATE V)
- [X] T020 [US2] Ajustar `frontend/testes/PaginaDoAssistente.test.tsx` — novo teste: alternar provedor pelo badge muda o `provedorDeIa` enviado em `gerarConsulta`; com provedor sem token, Enviar desabilitado + aviso orientado (depende de T018; mesmo arquivo — executar em ordem)

**Checkpoint**: User Story 2 completa — provedor trocável por badges no rodapé do chat, sem abrir configurações (MVP de seleção)

---

## Phase 5: User Story 3 - Consultar e abrir conversas anteriores na barra lateral (Priority: P1)

**Goal**: A barra lateral (`ListaDeConversas` — já existente) continua listando as conversas; o provedor da conversa é restaurado no seletor ao abrir e o padrão é usado em conversas novas; provedor removido na conversa aberta cai para o padrão com aviso (FR-001/FR-012/FR-017).

**Independent Test**: Criar conversa A com `openai` e conversa B com `claude`; alternar entre elas pela barra lateral e ver o badge ativo seguindo a conversa; abrir uma conversa legada (sem provedor) → badge no padrão (SC-004/SC-006).

### Testes para User Story 3 ⚠️

> **NOTA**: Escrever estes testes PRIMEIRO, garantir que FALHAM antes da implementação

- [X] T021 [P] [US3] Ajustar `frontend/testes/PaginaDoAssistente.test.tsx` — ao abrir uma conversa com `provedorDeIa` informado no mock de `obterConversa`, o badge selecionado reflete esse provedor; conversa sem o campo usa o `provedorPadrao`; conversa com provedor removido da sessão cai para o padrão com aviso (SC-004; dependem de T022 — mesmo arquivo, executar em ordem)

### Implementação para User Story 3

- [X] T022 [P] [US3] Ajustar `frontend/src/modelos/tiposDoAssistente.ts` — adicionar `provedorDeIa?: string` na interface `ConversaDetalhada` (contracts/api.md: campo opcional de `GET /api/conversas/{id}`)
- [X] T023 [US3] Ajustar `frontend/src/paginas/PaginaDoAssistente.tsx` — em `selecionarConversa`, restaurar `provedorDeIa` a partir de `detalhada.provedorDeIa` quando presente, senão usar `provedorPadrao`; em `iniciarNovaConversa` usar `provedorPadrao`; se o provedor restaurado não tiver token na sessão, selecionar o padrão (quando existir) e exibir aviso de configuração (FR-017, citado do data-model: "remoção de provedor selecionado na conversa → fallback para o padrão ou aviso de configuração, sem travar o chat"); manter `ListaDeConversas` e o fluxo de salvar a conversa atual ao trocar (depende de T018 e T022; mesmo arquivo — executar em ordem)

**Checkpoint**: User Story 3 completa — provedor reaplicado por conversa com fallback seguro

---

## Phase 6: User Story 4 - Gerenciar múltiplos provedores sem expor os tokens (Priority: P2)

**Goal**: Em nenhum momento o token é exibido por completo (mascarado / "configurado"); mensagens de erro e estado nunca revelam a chave (FR-007/SC-003); erro de envio com token inválido permanece genérico e orientado (painel de erro do chat).

**Independent Test**: Configurar um token, reabrir o modal e verificar que o valor nunca aparece íntegro; enviar com token inválido e confirmar mensagem genérica sem trecho da chave; conferir que `dados/conversas/*.json` não contém segredo em claro (SC-003/FR-007/FR-014).

### Testes para User Story 4 ⚠️

> **NOTA**: Escrever estes testes PRIMEIRO, garantir que FALHAM antes da implementação

- [X] T024 [P] [US4] Ajustar `frontend/testes/ModalDeConfiguracaoDeProvedores.test.tsx` — masquerade: o texto do token jamais aparece em `screen` (busca pelo valor informado não encontra o elemento); edição usa campo `type="password"` sem persistir valor antigo na tela (SC-003/FR-007)

### Implementação para User Story 4

- [X] T025 [US4] Ajustar `frontend/src/paginas/PaginaDoAssistente.tsx` — mensagem de erro de envio com token inválido permanece genérica ("Não foi possível gerar a consulta pelo provedor selecionado.") e, quando o erro for de autenticação/ausência de token, orienta a abrir Configurações sem exibir qualquer trecho da chave (FR-014/SC-007; reutiliza a mensagem já tratada pelo `ApiAssistente`); nenhum log/estado de erro recebe a chave (depende de T023 — mesmo arquivo, executar em ordem)
- [X] T026 [P] [US4] Ajustar `frontend/testes/PaginaDoAssistente.test.tsx` — envio com token inválido (mock de `gerarConsulta` rejeitando) mostra erro genérico sem nenhum trecho da chave e sem quebrar o chat (depende de T025; mesmo arquivo — executar em ordem)

**Checkpoint**: User Story 4 completa — tokens nunca expostos em tela, erro ou persistência

---

## Phase 7: Polish & Cross-Cutting Concerns

**Propósito**: Higiene, consistência e validação final da feature completa

- [X] T027 [P] Remover `frontend/src/componentes/ConfiguracaoDoAssistente.tsx` e suas classes CSS legadas em `frontend/src/estilos.css` (`.configuracao-do-assistente` etc.) — substituído pelo modal + seleção por badges (verificar importações após T018)
- [X] T028 [P] Ajustar `frontend/src/estilos.css` — classes do modal (`.modal-de-configuracao`, overlay, botão "Configurações" na `barra-superior` mantendo a hierarquia visual da feature 004) e reorganizar o rodapé `.entrada-do-chat` (badges + textarea + Enviar) sem quebras em telas de desktop/notebook
- [X] T029 [P] Atualizar `specs/005-ai-provider-config/contracts/api.md` — confirmar que a documentação do campo `provedorDeIa` e do efeito colateral de `POST /api/assistente/consultas` corresponde ao implementado (FR-018: documentação em pt-BR)
- [X] T030 Rodar validação completa: `cd backend && dotnet build && dotnet test` e `cd frontend && npm run lint && npm test && npm run build`; corrigir pendências dos cenários C1–C7 do `quickstart.md`; conferir com `rg -n "chaveDeApi|sk-" sites` em `frontend/src` e `backend/DatabaseDiagram.Api` que nenhum segredo/config exposta a chave
- [X] T031 [P] Registrar commits em pt-BR seguindo a convenção do repositório (ex.: `feat: configurar provedores de IA no assistente`), um por lote lógico (foundational / US1 / US2+US3 / US4 / polish)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: Sem dependências — pode começar imediatamente
- **Foundational (Phase 2)**: Depende do Setup — **BLOQUEIA** todas as user stories
- **User Stories (Phases 3–6)**: Dependem da Foundational; podem seguir em paralelo
  por capacidade de time, mas **compartilham arquivos** (`PaginaDoAssistente.tsx`
  e `frontend/testes/PaginaDoAssistente.test.tsx`), então no executor único a
  ordem recomendada é US1 → US2 → US3 → US4
- **Polish (Phase 7)**: Depende de todas as user stories desejadas

### User Story Dependencies

- **US1 (P1)**: Após Foundational — cria o modal e o estado de sessão; base para US2/US4
- **US2 (P1)**: Após Foundational; reusa o estado `chavesPorProvedor` da US1 para os badges
- **US3 (P1)**: Após Foundational (campo backend) e US2 (seletor); restaura o provedor da conversa
- **US4 (P2)**: Após US1; reforça mascaramento e erro orientado (sem novas features)

### Within Each User Story

- Testes PRIMEIRO (falham) → modelos/tipos → componente → integração na página → teste de página
- Cada story deixa a suíte verde ao terminar

### Parallel Opportunities

- Setup: T001, T002, T003 em paralelo
- Foundational: T004, T005, T008, T009, T010 em paralelo (T006 depende de T004/T005; T007 de T006)
- US1: T011, T012 em paralelo (T013/T014 depois; T015 ao final da story)
- US2: T016, T017, T019 em paralelo (T018 depois; T020 ao final da story)
- US3: T021, T022 em paralelo (T023 depois)
- US4: T024 em paralelo; T025 (página) em sequência; T026 ao final
- Polish: T027, T028, T029, T031 em paralelo; T030 (validação) por último

---

## Parallel Example: User Story 1

```bash
# Launch tests and components that touch different files together:
Task: "Criar frontend/testes/ModalDeConfiguracaoDeProvedores.test.tsx"
Task: "Ajustar frontend/src/modelos/tiposDoAssistente.ts"
Task: "Criar frontend/src/componentes/ModalDeConfiguracaoDeProvedores.tsx"
```

## Parallel Example: User Story 2

```bash
Task: "Criar frontend/testes/SeletorDeProvedor.test.tsx"
Task: "Criar frontend/src/componentes/SeletorDeProvedor.tsx"
Task: "Ajustar frontend/src/estilos.css (badges)"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Phase 1: Setup (baseline verde)
2. Phase 2: Foundational (backend do provedor por conversa) — CRÍTICO, bloqueia tudo
3. Phase 3: User Story 1 (modal de configuração) — delta entregável: configurar provedores pela sessão
4. **STOP e VALIDE** a US1 isolada (cenário C1/C6 do quickstart.md)
5. Fase 7 parcial (lint/build/test do que já existe) antes de avançar

> Observação do plano: a US1 exige também o estado de sessão que a US2 consome;
> como o modal e o rodapé dos badges vivem na mesma página, o delta "US1 + US2"
> é a fatia de valor completa do pedido — a US1 entrega a configuração, a US2 a
> troca no chat. Priorize US1→US2 antes de US3/US4 em entrega incremental.

### Incremental Delivery

1. Setup + Foundational → backbone com provedor por conversa (testável via curl: C4/C7)
2. US1 → configurar provedores (testável: C1/C6)
3. US2 → trocar provedor no rodapé (testável: C2/C3/C5)
4. US3 → restauração por conversa na barra lateral (testável: C4/C7)
5. US4 → mascaramento e erro orientado (testável: C6)
6. Polish → higiene e validação completa (T030)

### Parallel Team Strategy

1. Team completa Setup + Foundational junto
2. Após Foundational, por capacidade:
   - Dev A: US1 (modal) → US4 (mascaramento)
   - Dev B: US2 (badges) → US3 (restauração)
   - Integração na `PaginaDoAssistente.tsx` (arquivo compartilhado) feita pelo
     primeiro a chegar, seguindo a ordem US1 → US2 → US3 → US4

---

## Notes

- [P] tasks = arquivos diferentes, sem dependências
- [Story] label mapeia a tarefa para a user story da spec.md para rastreabilidade
- Tarefas da mesma story que editam `PaginaDoAssistente.tsx` ou seu teste são
  sequenciais (sem [P]) para evitar conflito de arquivo
- Testes falham antes da implementação (verifique antes de implementar)
- Commit após cada tarefa ou lote lógico (em pt-BR)
- Para em qualquer checkpoint para validar a story independente
- Evite: tarefas vagas, conflitos no mesmo arquivo, dependências cruzadas que
  quebrem a independência