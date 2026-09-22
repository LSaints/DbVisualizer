# Implementation Plan: Filtro de Relevância de Tabelas na Introspecção MySQL

**Branch**: `006-mysql-table-relevance-filter` | **Date**: 2026-09-22 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/006-mysql-table-relevance-filter/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command; its definition describes the execution workflow.

## Summary

Alterar a consulta de descoberta de tabelas do `MySqlProvedorDeSchema` para filtrar
por `TABLE_ROWS > 10` e ordenar por `TABLE_ROWS DESC` antes de aplicar o limite de
segurança já existente (500 tabelas / `LIMIT 501`), evitando que o diagrama seja
poluído por tabelas vazias ou quase vazias. `TABLE_ROWS` é lido apenas durante a
consulta de descoberta (não é persistido no modelo `TabelaDeBanco` nem no schema
retornado) e é tratado como estimativa, não contagem exata — nenhuma consulta
`COUNT(*)` por tabela é introduzida. O restante do fluxo (colunas, PK, FK, aviso de
truncamento, timeout, cancelamento) permanece inalterado.

## Technical Context

**Language/Version**: C# / .NET 8 (API) e .NET 10 (projeto de testes)

**Primary Dependencies**: MySqlConnector 2.4.0 (ADO.NET para MySQL); xUnit 2.9.3 para testes

**Storage**: MySQL (somente leitura via `information_schema`) — nenhum armazenamento próprio da aplicação

**Testing**: xUnit (`DatabaseDiagram.Testes`), com testes existentes em `MySqlProvedorDeSchemaTestes.cs` cobrindo consultas, montagem de schema e truncamento

**Target Platform**: Backend ASP.NET Core (Linux/containers)

**Project Type**: Web application (backend `DatabaseDiagram.Api` + frontend separado, não afetado por esta mudança)

**Performance Goals**: Nenhuma consulta adicional por tabela (sem N+1 `COUNT(*)`); tempo de introspecção não deve piorar em relação ao comportamento atual (constituição V — bancos com centenas de tabelas devem continuar navegáveis)

**Constraints**: Somente leitura (constituição III); nenhuma nova abstração/serviço/pattern (constituição I); código, comentários e testes em pt-BR (constituição II); mudança confinada ao `MySqlProvedorDeSchema` (constituição IV — particularidades do MySQL ficam no provider)

**Scale/Scope**: Alteração pontual em uma consulta SQL, no laço de seleção de tabelas e nos testes do provider MySQL existente; sem mudança de contrato de API, modelo de schema exposto ou frontend

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **I. Simplicidade Primeiro**: PASS. Nenhuma abstração nova; `TABLE_ROWS` é lido e usado apenas dentro do método privado de descoberta de tabelas do provider MySQL existente, sem novo serviço, repositório ou pattern.
- **II. Código em Português (pt-BR)**: PASS. Nomes de método/variável seguem convenção pt-BR já usada no provider (ex.: `ObterTabelasAsync`); `TABLE_ROWS`/`TABLE_TYPE` etc. são valores técnicos do banco, preservados conforme a exceção da constituição.
- **III. Somente Leitura**: PASS. A mudança usa apenas `SELECT` sobre `information_schema.TABLES`, sem `COUNT(*)` por tabela nem qualquer escrita.
- **IV. Provedores Isolados, Contrato Comum**: PASS. Toda a mudança fica dentro de `MySqlProvedorDeSchema` (consulta, ordenação, seleção); a interface `InterfaceProvedorDeSchema` e o modelo comum `EsquemaDeBanco`/`TabelaDeBanco` não são alterados.
- **V. Visual First**: PASS. O objetivo direto é melhorar a navegabilidade do diagrama em bancos grandes, priorizando tabelas com dados reais; nenhum comportamento de carregamento/priorização existente (limite de 500, aviso de truncamento) é removido.

Nenhuma violação identificada. Seção "Complexity Tracking" não se aplica.

## Project Structure

### Documentation (this feature)

```text
specs/006-mysql-table-relevance-filter/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command) — N/A, ver nota abaixo
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
backend/
├── DatabaseDiagram.Api/
│   └── Provedores/
│       └── MySql/
│           └── MySqlProvedorDeSchema.cs   # Consulta de tabelas, seleção por TABLE_ROWS, laço de introspecção (arquivo alterado)
└── DatabaseDiagram.Testes/
    └── Provedores/
        └── MySqlProvedorDeSchemaTestes.cs  # Testes do provider MySQL (arquivo alterado/estendido)

frontend/   # Não afetado por esta mudança
```

**Structure Decision**: Aplicação web existente (`backend/` ASP.NET Core + `frontend/`). A mudança é
inteiramente interna ao provider `backend/DatabaseDiagram.Api/Provedores/MySql/MySqlProvedorDeSchema.cs`
e seus testes em `backend/DatabaseDiagram.Testes/Provedores/MySqlProvedorDeSchemaTestes.cs`, conforme
constituição IV (particularidades de banco ficam isoladas no provider). Nenhum diretório novo é criado.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

Nenhuma violação — seção não aplicável.
