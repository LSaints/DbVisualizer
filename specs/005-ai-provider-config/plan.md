# Implementation Plan: Configuração e Seleção de Provedores de IA no Assistente

**Branch**: `005-ai-provider-config` | **Date**: 2026-09-17 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/005-ai-provider-config/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command; its definition describes the execution workflow.

## Summary

A página do assistente ganha (1) uma barra lateral de conversas anteriores
(já existente via `ListaDeConversas`), (2) uma área de configurações que abre
um **modal** para gerenciar os provedores de IA e seus tokens, e (3) um
**componente de seleção de provedor por badges** na barra inferior do chat, ao
lado do `textarea` e do botão Enviar, permitindo trocar o provedor sem abrir
configurações. O provedor selecionado é associado à conversa e restaurado ao
reabri-la. Os tokens são **somente em memória durante a sessão** (constituição
III — decisão da pesquisa que resolve o marcador da FR-015).

Enfoque técnico: a persistência de conversas por arquivo (feature 003) é
estendida com um campo `provedorDeIaSelecionado` na `Conversa`, devolvido pelo
`GET /api/conversas/{id}`; a gestão de tokens é **estado de sessão no
frontend** (mapa provedor → token, provendo o token no corpo de cada consulta,
exatamente como hoje) — não há novo endpoint nem persistência de segredos. O
`ConfiguracaoDoAssistente` inline vira um modal; o `select` de provedor vira um
seletor de badges no rodapé do chat.

## Technical Context

**Language/Version**: Backend C# (.NET 8, ASP.NET Core); Frontend TypeScript (React 18 + Vite)

**Primary Dependencies**: Backend — ASP.NET Core MVC, `System.Text.Json`, rate limiting existente. Frontend — Vite, React, Vitest + Testing Library (existente). Nenhuma dependência externa nova.

**Storage**: Arquivos JSON `{id}.json` por conversa (`ServicoDeConversas`). Tokens **não** são persistidos — somente memória da sessão (constituição III).

**Testing**: Backend — xUnit (`cd backend && dotnet test`); Frontend — Vitest + Testing Library (`cd frontend && npm test`), lint + build (`npm run lint && npm run build`).

**Target Platform**: Web (API em `http://localhost:5000`; frontend em `http://localhost:5173`, Vite serve `/api` proxied).

**Project Type**: Web application (frontend React + backend API REST)

**Performance Goals**: Troca de provedor em ≤1 clique; badges de até 8 provedores sem quebrar o layout; listagem de conversas mantém a ordenação existente (sem nova latência).

**Constraints**: Constituição do projeto — `NÃO` persistir credenciais, `SIM` identificadores/interface em pt-BR, isolamento de provedores (novo provedor = só registrar na fábrica), simplificação (sem camadas novas). Sem alteração no contrato estruturado da resposta do assistente. Atualização do teste e da UI existentes que dependem do `ConfiguracaoDoAssistente`.

**Scale/Scope**: Usuário único, sem autenticação; MVP com provedores já implementados na fábrica (openai, google-ai-studio, claude).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **GATE I — Simplidade Primeiro**: não criar abstrações novas; reutilizar `FabricaDeProvedoresDeIa`, `ServicoDeConversas` e o fluxo atual do chat. O modal é estado de sessão no frontend (sem repositório/token-store no backend). **PASSA.**
- **GATE II — Código em pt-BR**: todos os novos identificadores, mensagens, testes, commits e docs em pt-BR. **PASSA.**
- **GATE III — Somente Leitura / Credenciais efêmeras**: tokens em memória (estado React da sessão), nunca persistidos; a consulta gerada continua somente exibição; nenhum segredo em logs (mantém `FalhaAoGerarConsulta` genérica). **PASSA** — resolve o marcador da FR-015.
- **GATE IV — Provedores isolados, contrato comum**: o seletor de badges lista o que a fábrica oferece; adicionar um provedor não altera página/badges/modal (o cadastro é genérico por `provedor` + `rotulo`). **PASSA.**
- **GATE V — Visual First**: badges discretos no rodapé do chat, sem disputar atenção com o diagrama/consulta; US não afeta o carregamento do schema. **PASSA.**

## Project Structure

### Documentation (this feature)

```text
specs/005-ai-provider-config/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
backend/
├── DatabaseDiagram.Api/
│   ├── Controladores/           # AssistenteControlador, ConversasControlador
│   ├── Dtos/                    # ConversaDetalhada, ConversaResumo, ProvedorDeIaDisponivel
│   ├── Modelos/                 # Conversa (+ provedorDeIaSelecionado)
│   ├── Provedores/              # FabricaDeProvedoresDeIa + provedores isolados
│   ├── Servicos/                # ServicoDeConversas, ServicoDoAssistente
│   └── Program.cs               # DI (sem mudança estrutural)
├── DatabaseDiagram.Testes/
│   ├── Controladores/           # AssistenteControladorTestes, ConversasControladorTestes
│   └── Servicos/                # ServicoDeConversasTestes

frontend/
└── src/
    ├── componentes/
    │   ├── SeletorDeProvedor.tsx            # NOVO: badges do rodapé do chat
    │   ├── ModalDeConfiguracaoDeProvedores.tsx  # NOVO: modal (substitui ConfiguracaoDoAssistente)
    │   ├── ConfiguracaoDoAssistente.tsx     # REMOVIDO/substituído pelo modal
    │   └── ListaDeConversas.tsx             # Sidebar existente (inalterada)
    ├── paginas/PaginaDoAssistente.tsx       # Orquestra badges + modal + provedor por conversa
    ├── modelos/tiposDoAssistente.ts         # + `provedorDeIa?` em ConversaDetalhada; tipo da config de sessão
    ├── servicos/ApiAssistente.ts            # Sem mudança de rotas (campos novos chegam pelo JSON)
    └── estilos.css                          # + badges, modal, botão de configurações

frontend/testes/
├── SeletorDeProvedor.test.tsx               # NOVO
├── ModalDeConfiguracaoDeProvedores.test.tsx # NOVO
└── PaginaDoAssistente.test.tsx              # ajustado (chave via modal/badges)
```

**Structure Decision**: Aplicação web já organizada em `backend/` (API REST) e
`frontend/` (React). O plano estende essa estrutura com dois componentes novos,
uma representação de estado de sessão no frontend e um campo novo no modelo de
conversa — sem novas pastas, camadas ou projetos (GATE I).

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

Sem violações — tabela não aplicável.

## Phase 0 — Research (resultado em [research.md](./research.md))

Desconhecidos e dependências identificados na especificação:

1. **Armazenamento dos tokens (FR-015, marcador)** → **resolvido**: somente em
   memória durante a sessão, conforme a constituição III. O frontend guarda o
   mapa `provedor → token` em estado React; o token segue viajando no corpo de
   `POST /api/assistente/consultas` (como hoje) e é descartado ao fechar.
2. **Semântica da seleção de provedor** → **resolvido**: o provedor é
   propriedade da conversa; persistido com ela no campo novo `provedorDeIaSelecionado`;
   a troca vale para as próximas mensagens mantendo o histórico.
3. **Provedor padrão** → **resolvido**: o primeiro provedor configurado na
   sessão vira o padrão; alterável no modal; usado em conversas novas.
4. **Escalabilidade dos badges** → **resolvido**: flex-wrap/overflow-x no
   rodapé, mantendo textarea e botão Enviar; até 8 provedores medidos em teste.
5. **Dependências** → fábrica de provedores existente, persistência de
   conversas 003, contrato estruturado 002 (intocado), frontend atual
   (`ConfiguracaoDoAssistente` inline → substituído por modal).

## Phase 1 — Design (resultado em [data-model.md](./data-model.md), [contracts/api.md](./contracts/api.md), [quickstart.md](./quickstart.md))

**Backend**

- `Modelos/Conversa.cs`: novo campo opcional `string? ProvedorDeIaSelecionado`
  (camelCase no arquivo `provedorDeIaSelecionado`). Backward-compatible com
  `Versao = 1` (campo nulo em conversas antigas).
- `Servicos/ServicoDeConversas.cs`: `PersistirTrocaAsync` ganha o provedor e
  grava o campo; `ParaDetalhada` expõe `ProvedorDeIaSelecionado`.
- `Controladores/AssistenteControlador.cs`: repassa `requisicao.ProvedorDeIa`
  na persistência da troca (já validado como suportado).
- `Dtos/ConversaDetalhada.cs`: novo campo `ProvedorDeIa` (string?).

**Frontend**

- `modelos/tiposDoAssistente.ts`: `provedorDeIa?: string` em `ConversaDetalhada`;
  tipo `ConfiguracaoDeSessao` (mapa `provedor → token` + `provedorPadrao`).
- `componentes/ModalDeConfiguracaoDeProvedores.tsx`: modal com lista de
  provedores (da fábrica), adicionar/editar/remover token (campo `password`,
  valor mascarado/`configurado`), definir padrão; estado de sessão.
- `componentes/SeletorDeProvedor.tsx`: badges clicáveis (um por provedor
  configurado), estado visual do selecionado (`aria-pressed`), wrap/overflow.
- `paginas/PaginaDoAssistente.tsx`: estado `chavesPorProvedor` +
  `provedorPadrao`; botão "Configurações" na barra superior abre o modal;
  rodapé do chat = `SeletorDeProvedor` + `textarea` + botão Enviar; `podeEnviar`
  exige token do provedor selecionado; ao abrir conversa, restaura o provedor; ao
  criar nova, usa o padrão; provedor removido na conversa aberta → fallback
  para o padrão com aviso.
- `estilos.css`: classes `.seletor-de-provedor`, `.badge-de-provedor`,
  `.badge-de-provedor-ativa`, `.modal-de-configuracao`, overlay, botão de
  configurações; ajuste de `.entrada-do-chat` para linha com badges + textarea +
  enviar.

**Testes**

- Backend (xUnit): `ServicoDeConversasTestes` (persistência/restauração do
  provedor, compatibilidade com conversa antiga sem o campo), `ConversasControladorTestes`
  e `AssistenteControladorTestes` (provedor gravado na troca; contrato intacto).
- Frontend (Vitest): `SeletorDeProvedor.test.tsx` (seleção, estado do badge,
  escala), `ModalDeConfiguracaoDeProvedores.test.tsx` (adicionar/editar/remover,
  mascaramento, padrão), `PaginaDoAssistente.test.tsx` ajustado (token via modal,
  restauração de provedor por conversa, fluxo de envio mantido).

## Phase 2 — Tasks

Gerado pelo `/speckit.tasks` (não cria aqui) a partir deste plano, com as
decisões acima.