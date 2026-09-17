# Implementation Plan: Premium Developer-Tool UI/UX Redesign

**Branch**: `004-premium-ui-redesign` | **Date**: 2026-09-16 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/004-premium-ui-redesign/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command; its definition describes the execution workflow.

## Summary

Revisão completa da UI/UX do frontend (React + Vite + React Flow, CSS puro) para uma estética "developer tool premium": dark theme sofisticado orientado a design tokens (níveis de superfície, tipografia, accent moderado), sidebar recolhível, painel de detalhes que vira drawer em telas menores, toolbar compacta, e microinterações discretas para seleção/foco de tabela, destaque de relacionamentos, abertura de menus/painéis e feedback de sucesso/erro. Abordagem técnica: introduzir um sistema de design tokens em CSS custom properties (preparado para tema claro futuro), refatorar os componentes/páginas existentes (`PaginaDoDiagrama`, `PaginaDoAssistente`, `TabelaNoDiagrama`, `ListaDeConversas`, `FormularioDeConexao`, `ConfiguracaoDoAssistente`, `BarraDePesquisa`, `CartaoDeRespostaDeConsulta`) para consumir os tokens, e adicionar transições CSS/estado React para as microinterações — sem alterar contratos de API nem lógica de negócio do backend.

## Technical Context

**Language/Version**: TypeScript 5.6 (frontend), React 18.3

**Primary Dependencies**: Vite 5, React Flow (`reactflow` 11), `dagre` (layout automático), CSS puro via `src/estilos.css` (sem framework de UI/CSS-in-JS)

**Storage**: N/A (feature é somente de apresentação; nenhuma mudança de storage ou API)

**Testing**: Vitest + Testing Library (`@testing-library/react`, `@testing-library/user-event`), suíte já existente em `frontend/testes/`

**Target Platform**: Navegador desktop/notebook moderno (Chrome/Edge/Firefox atuais), aplicação web servida pelo Vite/build estático

**Project Type**: Web application (frontend React consumindo API backend .NET já existente) — mudança restrita ao `frontend/`

**Performance Goals**: Transições/microinterações com duração perceptível como "rápida" (~150–250ms), sem jank perceptível em diagramas com centenas de tabelas (alinhado ao princípio V da constituição)

**Constraints**: Sem preto absoluto no tema escuro; contraste de texto conforme WCAG AA; nenhuma alteração em contratos de API, endpoints ou modelos de dados do backend; nenhuma dependência nova de UI framework/CSS-in-JS sem justificativa (Princípio I — Simplicidade Primeiro)

**Scale/Scope**: Todo o frontend existente (2 páginas, 6 componentes, ~600 linhas de CSS) recebe o novo sistema visual; foco em desktop/notebook (mínimo 1280px), sem experiência mobile completa

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **I. Simplicidade Primeiro**: PASS. Nenhuma nova abstração de arquitetura é introduzida; o plano reutiliza CSS puro já em uso (`estilos.css`) com custom properties para tokens de tema, em vez de adotar uma biblioteca de UI/CSS-in-JS. Não há novos providers, camadas ou serviços de backend.
- **II. Código em Português (pt-BR)**: PASS. Novos nomes de classes CSS, variáveis de tema, componentes e arquivos seguem pt-BR, consistente com o código existente (`TabelaNoDiagrama`, `PaginaDoDiagrama`, etc.).
- **III. Somente Leitura**: PASS. Feature é puramente visual/interativa no frontend; não introduz nenhuma escrita no banco nem manuseio adicional de credenciais.
- **IV. Provedores Isolados, Contrato Comum**: N/A para esta feature — nenhuma mudança em providers de banco de dados ou no backend.
- **V. Visual First**: PASS (reforça o princípio). A feature existe justamente para melhorar a exploração visual do diagrama; requisitos de performance com schemas grandes (FR-009, SC-001) foram herdados do princípio.

Nenhuma violação identificada. Seção "Complexity Tracking" não é necessária.

## Project Structure

### Documentation (this feature)

```text
specs/[###-feature]/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)
<!--
  ACTION REQUIRED: Replace the placeholder tree below with the concrete layout
  for this feature. Delete unused options and expand the chosen structure with
  real paths (e.g., apps/admin, packages/something). The delivered plan must
  not include Option labels.
-->

```text
backend/                         # inalterado nesta feature
frontend/
├── src/
│   ├── estilos.css              # tokens de tema (custom properties) + estilos globais
│   ├── modelos/                 # tipos TS existentes (inalterado)
│   ├── servicos/                # chamadas de API existentes (inalterado)
│   ├── componentes/
│   │   ├── BarraDePesquisa.tsx
│   │   ├── CartaoDeRespostaDeConsulta.tsx
│   │   ├── ConfiguracaoDoAssistente.tsx
│   │   ├── FormularioDeConexao.tsx
│   │   ├── ListaDeConversas.tsx
│   │   ├── TabelaNoDiagrama.tsx
│   │   └── [novos, se necessário] PainelLateral.tsx, GavetaDeDetalhes.tsx
│   └── paginas/
│       ├── PaginaDoDiagrama.tsx
│       └── PaginaDoAssistente.tsx
└── testes/                      # Vitest + Testing Library (existente, estendido)
```

**Structure Decision**: Projeto web existente com `backend/` (.NET, inalterado) e `frontend/` (React + Vite). Esta feature altera exclusivamente `frontend/src/estilos.css` (novo sistema de tokens), os componentes e páginas listados acima, e adiciona/ajusta testes em `frontend/testes/`. Nenhum diretório novo de nível superior é criado; novos componentes de UI (se necessários para sidebar/drawer) entram em `frontend/src/componentes/` seguindo a convenção pt-BR já estabelecida.

## Complexity Tracking

*Sem violações da constituição — seção não aplicável.*
