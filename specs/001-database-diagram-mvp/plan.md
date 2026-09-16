# Implementation Plan: Database Diagram MVP

**Branch**: `001-database-diagram-mvp` | **Date**: 2026-09-15 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/001-database-diagram-mvp/spec.md`

## Summary

Construir o MVP do Database Diagram: uma aplicação web que se conecta a um banco
MySQL com credenciais de somente leitura, faz a introspecção do schema via
`information_schema` e renderiza um diagrama interativo com tabelas, colunas,
PKs, FKs e relacionamentos. A arquitetura é preparada para novos providers por
meio de um provider isolado (MySQL) e um modelo de domínio comum. Identificadores,
mensagens e documentação em pt-BR, priorizando simplicidade (YAGNI) e manutenibilidade.

Fluxo principal (milestone 1 do PRD): informar conexão → testar conexão →
resolver provider MySQL → consultar metadados → construir modelo comum →
retornar contrato → frontend gera nodes/edges → aplicar layout → diagrama pronto.

## Technical Context

**Language/Version**: Backend: C# 12 com .NET 8 (LTS). Frontend: TypeScript 5
com React 18.

**Primary Dependencies**:

- Backend: ASP.NET Core 8 (Web API), MySqlConnector (conexão e leitura do
  `information_schema`). Swagger/OpenAPI apenas em desenvolvimento, se útil.
- Frontend: React, Vite, React Flow, Dagre (layout automático).
- EF Core / Pomelo deliberadamente não utilizados no MVP: não há entidades
  mapeadas previamente e a introspecção é dinâmica via metadados (ver
  `research.md`, decisão D3).

**Storage**: N/A — o produto não persiste dados. As conexões são efêmeras:
criadas em memória por requisição e descartadas ao fim.

**Testing**: Backend: xUnit (unitários para provider/fábrica/serviços; teste de
integração opcional contra um MySQL real para o provider). Frontend: Vitest +
Testing Library.

**Target Platform**: Navegador desktop (conectando a uma API ASP.NET Core local).

**Project Type**: aplicação web — frontend + backend REST.

**Performance Goals**: carregar schema com centenas de tabelas e manter o
diagrama navegável; busca por nome de tabela retorna em menos de 1 segundo.

**Constraints**: somente leitura (consultas de metadados predefinidas);
credenciais apenas em memória; sem autenticação; sem persistência; interface
100% em pt-BR; mensagens de erro sem dados sensíveis.

**Scale/Scope**: MVP de uso local, single-user; provider único (MySQL);
estrutura simples de backend e frontend, sem monólitos modulares.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **G1 — Simplicidade Primeiro (YAGNI)**: manutenção apenas dos contratos
  mínimos (provider de schema + modelo comum). Sem repositório, CQRS,
  mensageria, autenticação ou camadas adicionais. → **Cumpre**.
- **G2 — Código em pt-BR**: identificadores, DTOs, pastas, endpoints, mensagens,
  commits e documentação em português do Brasil. → **Cumpre** (valores técnicos
  de banco, ex.: `InnoDB`, `information_schema`, conservam o nome).
- **G3 — Somente Leitura**: sem INSERT/UPDATE/DELETE/DDL; consultas predefinidas;
  credenciais apenas em memória; erros não expõem senha/connection string →
  **Cumpre**.
- **G4 — Provedor Isolado, Contrato Comum**: MySQL isolado em um provider;
  controlador/serviço/frontend dependem somente do modelo comum; adicionar um
  banco não exige alterar renderização. → **Cumpre**.
- **G5 — Visual First + Performance**: o diagrama é o produto principal; layout
  automático; banco com centenas de tabelas permanece navegável → **Cumpre**.
- **G6 — Segurança**: acesso restrito a metadados; mensagens de erro genéricas
  e orientadas à ação → **Cumpre**.

*Resultado dos gates no design (Phase 1): todos passando, sem violações a
justificar.*

## Project Structure

### Documentation (this feature)

```text
specs/001-database-diagram-mvp/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
│   └── api.md
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
backend/
├── DatabaseDiagram.Api/
│   ├── Controladores/
│   │   ├── ConexoesControlador.cs
│   │   └── EsquemaControlador.cs
│   ├── Dtos/
│   │   ├── RequisicaoConexao.cs
│   │   ├── RespostaTesteConexao.cs
│   │   └── RespostaEsquema.cs
│   ├── Infraestrutura/
│   │   ├── ConfiguradorDeConexao.cs
│   │   └── TipoDeProvedor.cs
│   ├── Modelos/
│   │   ├── ConexaoDeBanco.cs
│   │   ├── EsquemaDeBanco.cs
│   │   ├── TabelaDeBanco.cs
│   │   ├── ColunaDeBanco.cs
│   │   └── RelacionamentoDeBanco.cs
│   ├── Provedores/
│   │   ├── InterfaceProvedorDeSchema.cs
│   │   ├── FabricaDeProvedoresDeSchema.cs
│   │   └── MySql/
│   │       └── MySqlProvedorDeSchema.cs
│   ├── Servicos/
│   │   ├── ServicoDeConexao.cs
│   │   └── ServicoDeSchema.cs
│   └── Program.cs
└── DatabaseDiagram.Testes/
    ├── Provedores/
    │   └── MySqlProvedorDeSchemaTestes.cs
    └── Servicos/
        └── ServicoDeSchemaTestes.cs

frontend/
├── src/
│   ├── componentes/
│   │   ├── FormularioDeConexao.tsx
│   │   ├── TabelaNoDiagrama.tsx
│   │   └── BarraDePesquisa.tsx
│   ├── paginas/
│   │   └── PaginaDoDiagrama.tsx
│   ├── servicos/
│   │   └── ApiDiagrama.ts
│   ├── modelos/
│   │   └── tipos.ts
│   └── App.tsx
├── testes/
└── package.json

README.md
docs/
└── PRD.md
specs/
└── 001-database-diagram-mvp/
```

**Structure Decision**: aplicação web com backend (`backend/`) e frontend
(`frontend/`) separados, conforme o PRD. A estrutura interna do backend segue o
diagrama do PRD (Controllers, Models, DTOs, Services, Providers, Infrastructure)
com nomes de pastas e de identificadores em pt-BR, conforme a constituição
(princípio II) e a decisão D2 em `research.md`. O modelo de domínio em
`Modelos/` é o contrato comum compartilhado entre backend e frontend.

## Complexity Tracking

> Nenhuma violação de Constitutional Check — tabela não aplicável neste plano.
> Conforme G1 (YAGNI), qualquer nova abstração futura deve passar por esta
> tabela antes de entrar no código.