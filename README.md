# Database Diagram

Aplicação web para visualizar graficamente a estrutura de bancos de dados.
Conecta-se a um banco MySQL com credenciais de **somente leitura**, realiza a
introspecção do schema e apresenta tabelas, colunas, chaves primárias, chaves
estrangeiras e relacionamentos em um diagrama interativo.

Documentação de especificação, planejamento e contratos em
[`specs/001-database-diagram-mvp/`](specs/001-database-diagram-mvp/) e PRD em
[`docs/PRD.md`](docs/PRD.md).

## Estrutura do projeto

```text
backend/
├── DatabaseDiagram.Api/        # API ASP.NET Core (.NET 8)
│   ├── Controladores/          # Endpoints REST (pt-BR)
│   ├── Dtos/                   # Contratos de entrada/saída (pt-BR camelCase)
│   ├── Infraestrutura/         # Configurador de conexão e tipo de provedor
│   ├── Modelos/                # Modelo de domínio comum (contrato compartilhado)
│   ├── Provedores/             # Providers isolados (MySql) e fábrica
│   └── Servicos/               # Teste de conexão e introspecção de schema
└── DatabaseDiagram.Testes/     # Testes unitários (xUnit)

frontend/
└── src/
    ├── componentes/            # Formulário de conexão, tabela no diagrama, busca
    ├── paginas/                # Página do diagrama (React Flow + Dagre)
    ├── servicos/               # Cliente da API, layout e filtros
    └── modelos/                # Tipos espelhando o modelo de domínio comum
```

## Pré-requisitos

- .NET 8 SDK (`dotnet --version`)
- Node.js 18+ com npm (`node --version`)
- Banco MySQL acessível com usuário de **somente leitura** e um database de
  exemplo com ao menos 2 tabelas e 1 chave estrangeira

## Execução

### Backend (API)

```sh
cd backend
dotnet restore
dotnet run --project DatabaseDiagram.Api
```

A API fica disponível em `http://localhost:5000` (porta de desenvolvimento).

### Frontend (Vite + React)

```sh
cd frontend
npm install
npm run dev
```

A aplicação fica disponível em `http://localhost:5173`. O Vite encaminha
`/api` para a API em `http://localhost:5000`.

## Validação

```sh
# Backend (xUnit)
cd backend && dotnet test

# Frontend (Vitest + Testing Library)
cd frontend && npm test

# Frontend (lint e build de produção)
cd frontend && npm run lint && npm run build
```

O guia de validação ponta a ponta (cenários 1–9) está em
[`specs/001-database-diagram-mvp/quickstart.md`](specs/001-database-diagram-mvp/quickstart.md).
Contratos da API em [`specs/001-database-diagram-mvp/contracts/api.md`](specs/001-database-diagram-mvp/contracts/api.md).

## Princípios

- **Somente leitura**: nenhuma operação de escrita (INSERT/UPDATE/DELETE/DDL);
  apenas consultas predefinidas de metadados no `information_schema`.
- **Credenciais efêmeras**: conexões criadas em memória por requisição; a senha
  nunca é persistida, retornada ou logada.
- **Interface e código em pt-BR**, com exceções técnicas de banco
  (ex.: `InnoDB`, `information_schema`) conforme a constituição do projeto.
- **Provedor isolado, contrato comum**: adicionar um novo banco não exige
  alterar controllers, serviços, modelo comum ou frontend.