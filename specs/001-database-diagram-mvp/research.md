# Pesquisa e Decisões — Database Diagram MVP

Fase 0 do `/speckit.plan`. Consolida as decisões técnicas e resolve as
ambiguidades deixadas pela especificação (spec.md + PRD), em conformidade com a
constituição do projeto.

## Decisões

### D1 — Identificadores e contrato de API em pt-BR

- **Decision**: todos os identificadores de código (classes, propriedades,
  métodos, variáveis, DTOs, pastas, arquivos e endpoints) em português do
  Brasil. O contrato JSON da API também usa chaves em pt-BR (camelCase).
- **Rationale**: princípio II da constituição e pedido explícito do usuário
  ("escrito em pt-BR nomes de variáveis, funções e etc."). Mantém consistência
  total entre código, mensagens de erro, commit e documentação, reduzindo o
  custo de manutenção para o time brasileiro.
- **Alternatives considered**:
  - Manter os nomes em inglês do PRD (`DatabaseSchema`, `databaseName`,
    `tableType` etc.): rejeitado por contrariar a constituição.
  - Misto (inglês no código, pt-BR na UI): rejeitado por criar inconsistência
    e ambiguidade na nomenclatura.
- **Exceções aplicáveis** (constituição II): palavras reservadas e convenções
  impostas por linguagem/bibliotecas, e valores técnicos de banco que
  conservam o nome original (ex.: `InnoDB`, `information_schema`, `BASE TABLE`,
  `varchar(255)`).

### D2 — Estrutura de pastas do backend em pt-BR

- **Decision**: a estrutura do PRD é mantida, mas renomeada para pt-BR:
  `Controladores/`, `Modelos/`, `Dtos/`, `Servicos/`, `Provedores/`,
  `Infraestrutura/`.
- **Rationale**: o PRD descreve a estrutura como "sugerida" e não imposta por
  nenhuma ferramenta; `Controllers` é convenção, não exigência do ASP.NET Core.
  A consistência de idioma é um princípio da constituição.
- **Alternatives considered**:
  - Manter `Controllers/Models/DTOs/...` em inglês: rejeitado por consistência
    com D1.
  - Estrutura sem pasta de serviços (controller → provider direto): rejeitado
    porque o fluxo teste de conexão também precisa do serviço e o contrato
    comum no serviço facilita futuros providers sem alterar controllers.

### D3 — Acesso ao `information_schema` sem EF Core

- **Decision**: usar MySqlConnector diretamente (`MySqlConnection` +
  `DbCommand`) com consultas de leitura predefinidas contra as tabelas de
  metadados do `information_schema` (schemata, tables, columns,
  table_constraints, key_column_usage, referential_constraints).
- **Rationale**: o schema é descoberto dinamicamente e não há entidades a
  mapear; portanto o EF Core não agregaria valor no MVP e só adicionaria uma
  dependência sem necessidade — exatamente o que o princípio I (YAGNI) proíbe.
  Cada provider mantém suas consultas isoladas (princípio IV).
- **Alternatives considered**:
  - EF Core + Pomelo para gerenciar a conexão: rejeitado, adiciona peso sem
    ganho no MVP (sem mapeamento de entidades).
  - Introspecção fora do provider (compartilhada): rejeitado — viola o
    isolamento de providers da constituição.
- **Nota**: EF Core/Pomelo pode ser avaliado no futuro caso algum provider
  tenha ganho real com mapeamento. Não é decisão desta fase.

### D4 — Biblioteca de layout automático

- **Decision**: Dagre para o layout automático do diagrama.
- **Rationale**: leve e suficiente para o MVP (grafo direcionado acíclico de
  tabelas com hierarquia de relacionamentos). Alinha-se à simplicidade.
- **Alternatives considered**:
  - ELK.js: mais recursos e qualidade de layout, porém maior peso e
    complexidade; não necessário para o MVP.
  - Layout manual/estático: rejeitado — o PRD exige reorganização automática.

### D5 — Testes

- **Decision**: xUnit no backend (unitários para provider, fábrica e serviços;
  validação da montagem do modelo comum e das consultas predefinidas por
  provider). Vitest + Testing Library no frontend (componentes e fluxos de
  conexão/diagrama).
- **Rationale**: padrão maduro do ecossistema e cobertura mínima exigida pela
  constituição (workflow de desenvolvimento) para novos providers.
- **Alternatives considered**: MSTest/NUnit (equivalentes, sem vantagem
  específica); Jest (equivalente ao Vitest em ambiente Vite, porém Vitest
  integra-se melhor ao Vite usado no projeto).

### D6 — Provedor MySQL isolado com registro em fábrica

- **Decision**: uma abstração mínima `InterfaceProvedorDeSchema` com
  `ObterSchemaAsync(ConexaoDeBanco, CancellationToken)` e uma
  `FabricaDeProvedoresDeSchema` que resolve o provider pelo campo `provedor`.
  O MVP registra somente o `MySqlProvedorDeSchema`.
- **Rationale**: atende ao princípio IV (provedor isolado, contrato comum) com
  o menor número de abstrações — é o contrato mínimo exigido pela constituição.
- **Alternatives considered**: registro por injeção de dependência multiclasse
  sem fábrica (indireção desnecessária no MVP); uma única classe com `if` por
  banco (violaria o princípio IV).

## Decisões herdadas do PRD (sem alteração)

- Stack backend: .NET / ASP.NET Core Web API / REST.
- Stack frontend: React + TypeScript + Vite + React Flow.
- Introspecção via `information_schema` para MySQL.
- Somente leitura; credenciais em memória; sem persistência; sem autenticação.
- Porta padrão MySQL: `3306`.
- Contrato comum consumido pelo frontend, independente do banco.

## Registro de conformidade

Todas as decisões acima passaram a reavaliação da Constitutional Check:

- G1 (YAGNI): nenhuma abstração além da fábrica + interface de provider e do
  modelo comum.
- G2 (pt-BR): todas as decisões adotam pt-BR para identificadores e contratos.
- G3 (somente leitura): consultas predefinidas de metadados; nada de escrita.
- G4 (provider isolado/contrato comum): MySQL isolado; fábrica pronta para
  novos providers sem tocar em controllers/serviços/frontend.
- G5 (visual first): Dagre + React Flow garantem diagrama navegável.
- G6 (segurança): acesso restrito a metadados; mensagens de erro genéricas.