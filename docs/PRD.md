# PRD — Database Diagram

## 1. Visão geral

**Database Diagram** é uma aplicação web para visualizar graficamente a estrutura de bancos de dados.

A aplicação conecta-se a um banco de dados utilizando credenciais de **somente leitura**, realiza a introspecção do schema e apresenta tabelas, colunas e relacionamentos em um diagrama interativo.

O objetivo principal é facilitar a compreensão de bancos de dados existentes, principalmente bancos grandes ou desconhecidos.

O MVP será desenvolvido inicialmente para **MySQL**, mas a arquitetura deverá oferecer suporte à adição de outros bancos de dados no futuro.

A aplicação **não deve permitir alterações nos dados ou na estrutura do banco**.

---

# 2. Objetivo

Permitir que um desenvolvedor consiga:

* conectar a aplicação a um banco de dados MySQL;
* visualizar todas as tabelas;
* visualizar as colunas de cada tabela;
* identificar chaves primárias;
* identificar chaves estrangeiras;
* visualizar relacionamentos entre tabelas;
* pesquisar tabelas;
* navegar pelo diagrama;
* filtrar a visualização;
* visualizar apenas uma parte relevante do banco;
* utilizar futuramente diferentes provedores de banco de dados por meio de uma arquitetura extensível.

---

# 3. Problema

Ao trabalhar com um banco de dados existente, especialmente um banco legado ou desenvolvido por várias pessoas ao longo dos anos, é difícil entender rapidamente:

* quais tabelas existem;
* quais colunas cada tabela possui;
* quais tabelas estão relacionadas;
* quais tabelas dependem de determinada tabela;
* como uma determinada entidade se relaciona com o restante do banco.

Ferramentas tradicionais de administração de banco são excelentes para consultar e administrar dados, mas nem sempre oferecem uma experiência adequada para exploração visual do schema.

O Database Diagram terá como foco principal essa exploração, começando pelo MySQL e permitindo a expansão para outros bancos posteriormente.

---

# 4. Escopo do MVP

## 4.1 Banco suportado inicialmente

O MVP terá suporte inicial a:

**MySQL**

A arquitetura deverá ser preparada para suportar outros bancos de dados futuramente, como:

* PostgreSQL;
* SQL Server;
* SQLite;
* MariaDB;
* Oracle, se necessário.

O suporte a outros bancos não fará parte da primeira implementação, mas deverá ser considerado nas abstrações e contratos do backend.

---

# 5. Stack tecnológica

## Backend

* .NET;
* C#;
* ASP.NET Core Web API;
* Entity Framework Core;
* Pomelo.EntityFrameworkCore.MySql;
* MySqlConnector;
* REST API.

O EF Core será utilizado como parte da infraestrutura de acesso ao banco e gerenciamento da conexão.

A aplicação não dependerá de classes de entidades previamente conhecidas, pois o schema será descoberto dinamicamente.

A introspecção do MySQL deverá utilizar principalmente as tabelas de metadados do banco `information_schema`, incluindo:

* `information_schema.schemata`;
* `information_schema.tables`;
* `information_schema.columns`;
* `information_schema.table_constraints`;
* `information_schema.key_column_usage`;
* `information_schema.referential_constraints`;
* `information_schema.statistics`, quando necessário.

A camada de introspecção deverá ser isolada por provedor, permitindo que cada banco utilize suas próprias consultas e regras de metadados.

---

## Frontend

* React;
* TypeScript;
* React Flow;
* Vite.

Responsabilidades do frontend:

* renderizar o diagrama;
* permitir zoom;
* permitir pan;
* selecionar tabelas;
* pesquisar tabelas;
* filtrar tabelas;
* exibir informações das tabelas;
* renderizar relacionamentos;
* aplicar layouts automáticos;
* adaptar a visualização ao modelo de schema retornado pela API, independentemente do banco de origem.

---

# 6. Arquitetura

Arquitetura inicial simplificada:

```text
┌──────────────────────────────┐
│           React              │
│       TypeScript + Flow      │
└──────────────┬───────────────┘
               │
               │ HTTP
               ▼
┌──────────────────────────────┐
│       ASP.NET Core API       │
│                              │
│ Controllers                  │
│ Services                     │
│ Schema Providers             │
│ Provider Factory/Resolver    │
└──────────────┬───────────────┘
               │
               │ EF Core / DbConnection
               ▼
┌──────────────────────────────┐
│             MySQL            │
│                              │
│ information_schema           │
│ metadata tables              │
└──────────────────────────────┘
```

A arquitetura deverá separar:

* contratos comuns de schema;
* lógica de conexão;
* lógica de introspecção;
* implementação específica de cada banco;
* transformação dos dados para o frontend.

O frontend deverá consumir um modelo de schema comum, sem depender de detalhes específicos do MySQL.

---

# 7. Estrutura do backend

A primeira versão deve evitar overengineering, mas já deve possuir uma separação clara para permitir novos provedores.

Estrutura sugerida:

```text
DatabaseDiagram.Api/
│
├── Controllers/
│   ├── ConnectionsController.cs
│   └── SchemaController.cs
│
├── Models/
│   ├── DatabaseConnection.cs
│   ├── DatabaseSchema.cs
│   ├── DatabaseTable.cs
│   ├── DatabaseColumn.cs
│   ├── DatabaseRelationship.cs
│   └── DatabaseIndex.cs
│
├── DTOs/
│   ├── ConnectionRequest.cs
│   ├── ConnectionResponse.cs
│   ├── DatabaseSchemaResponse.cs
│   └── ...
│
├── Services/
│   ├── DatabaseConnectionService.cs
│   └── DatabaseSchemaService.cs
│
├── Providers/
│   ├── IDatabaseSchemaProvider.cs
│   ├── DatabaseSchemaProviderFactory.cs
│   └── MySql/
│       └── MySqlSchemaProvider.cs
│
├── Infrastructure/
│   ├── ConnectionStringBuilder.cs
│   └── DatabaseProviderType.cs
│
└── Program.cs
```

A estrutura deverá permitir a inclusão futura de novos providers sem alterar significativamente os controllers, serviços ou modelos compartilhados.

---

# 8. Introspecção do banco

A aplicação deverá descobrir automaticamente:

## Schemas

* nome do schema, correspondente ao database MySQL;
* charset padrão, quando disponível;
* collation padrão, quando disponível.

## Tabelas

* nome;
* schema/database;
* tipo da tabela;
* engine;
* comentário, quando disponível.

O MVP deverá priorizar tabelas do tipo `BASE TABLE`. Views poderão ser adicionadas posteriormente.

## Colunas

* nome;
* tipo;
* tipo completo;
* nullable;
* posição;
* default;
* comentário;
* primary key;
* auto incremento, quando disponível;
* collation, quando disponível;
* charset, quando disponível.

## Relacionamentos

* tabela origem;
* coluna origem;
* tabela destino;
* coluna destino;
* nome da constraint;
* regras de atualização e exclusão, quando disponíveis.

## Índices

Índices poderão ser coletados posteriormente, mas a arquitetura deverá permitir sua inclusão.

---

# 9. Modelo de domínio

Os modelos de domínio deverão ser independentes do banco de dados utilizado.

## DatabaseSchema

```csharp
public sealed class DatabaseSchema
{
    public required string Provider { get; init; }
    public required string DatabaseName { get; init; }

    public List<DatabaseTable> Tables { get; init; } = [];
    public List<DatabaseRelationship> Relationships { get; init; } = [];
}
```

## DatabaseTable

```csharp
public sealed class DatabaseTable
{
    public required string Schema { get; init; }
    public required string Name { get; init; }

    public string? TableType { get; init; }
    public string? Engine { get; init; }
    public string? Comment { get; init; }

    public List<DatabaseColumn> Columns { get; init; } = [];
}
```

## DatabaseColumn

```csharp
public sealed class DatabaseColumn
{
    public required string Name { get; init; }
    public required string DataType { get; init; }

    public string? FullDataType { get; init; }
    public string? DefaultValue { get; init; }
    public string? Comment { get; init; }

    public int OrdinalPosition { get; init; }

    public bool Nullable { get; init; }
    public bool PrimaryKey { get; init; }
    public bool ForeignKey { get; init; }
    public bool AutoIncrement { get; init; }
}
```

## DatabaseRelationship

```csharp
public sealed class DatabaseRelationship
{
    public required string SourceSchema { get; init; }
    public required string SourceTable { get; init; }
    public required string SourceColumn { get; init; }

    public required string TargetSchema { get; init; }
    public required string TargetTable { get; init; }
    public required string TargetColumn { get; init; }

    public string? ConstraintName { get; init; }
    public string? OnUpdate { get; init; }
    public string? OnDelete { get; init; }
}
```

Os modelos deverão representar conceitos comuns entre diferentes bancos. Informações específicas de um provedor poderão ser adicionadas como propriedades opcionais ou metadados, sem comprometer o contrato principal.

---

# 10. Provider de banco

A introspecção deverá utilizar uma abstração:

```csharp
public interface IDatabaseSchemaProvider
{
    string ProviderName { get; }

    Task<DatabaseSchema> GetSchemaAsync(
        DatabaseConnection connection,
        CancellationToken cancellationToken);
}
```

Implementação inicial:

```text
IDatabaseSchemaProvider
        │
        └── MySqlSchemaProvider
```

Estrutura futura:

```text
IDatabaseSchemaProvider
        │
        ├── MySqlSchemaProvider
        ├── PostgreSqlSchemaProvider
        ├── SqlServerSchemaProvider
        ├── SqliteSchemaProvider
        └── MariaDbSchemaProvider
```

A seleção do provider deverá ser feita com base no campo `provider` informado na conexão.

Exemplo:

```csharp
public interface IDatabaseSchemaProviderFactory
{
    IDatabaseSchemaProvider GetProvider(string provider);
}
```

A adição de um novo banco deverá exigir principalmente:

1. uma nova implementação de `IDatabaseSchemaProvider`;
2. o registro do provider na factory ou no container de dependências;
3. os ajustes necessários na tela de conexão;
4. testes específicos de introspecção.

A camada de API e o frontend deverão continuar utilizando os contratos comuns.

---

# 11. Segurança

A aplicação deverá trabalhar com o princípio de **somente leitura**.

## Requisitos

A conexão utilizada pelo usuário deve possuir apenas permissões necessárias para leitura do schema.

Para o MVP, o usuário deverá utilizar um usuário MySQL com permissões de leitura de metadados, preferencialmente com acesso a:

* `information_schema`;
* metadados do database selecionado;
* tabelas necessárias para leitura do schema.

A aplicação não deverá possuir funcionalidades para:

* INSERT;
* UPDATE;
* DELETE;
* CREATE TABLE;
* ALTER TABLE;
* DROP TABLE;
* CREATE DATABASE;
* DROP DATABASE;
* TRUNCATE TABLE;
* execução arbitrária de comandos SQL.

A aplicação também não será um cliente SQL.

Cada provider deverá executar apenas consultas de leitura previamente definidas para obter metadados do banco.

---

# 12. Gerenciamento de conexões

O usuário deverá conseguir informar:

```text
Provider
Host
Port
Database
Username
Password
```

Exemplo:

```json
{
  "provider": "mysql",
  "host": "localhost",
  "port": 3306,
  "database": "erp",
  "username": "readonly",
  "password": "********"
}
```

Após informar os dados, deverá existir uma opção:

**Testar conexão**

Resultado:

```text
✓ Conexão realizada com sucesso
```

ou:

```text
✕ Não foi possível conectar ao banco
```

A porta padrão do MySQL deverá ser:

```text
3306
```

Para outros providers, a aplicação deverá utilizar a porta padrão correspondente quando aplicável.

A tela deverá permitir selecionar o provider, mas no MVP apenas MySQL estará disponível.

---

# 13. Segurança das credenciais

No MVP, as credenciais deverão ser utilizadas apenas durante a sessão da aplicação.

Não armazenar senhas em:

* banco da aplicação;
* localStorage;
* logs;
* arquivos de configuração;
* respostas da API.

A API não deverá retornar a senha ao frontend.

A connection string deverá ser criada em memória e não deverá ser persistida.

Mensagens de erro não deverão expor:

* senha;
* connection string completa;
* tokens;
* informações sensíveis desnecessárias;
* detalhes internos que possam facilitar ataques.

---

# 14. API

## POST /api/connections/test

Testa uma conexão sem armazená-la.

Request:

```json
{
  "provider": "mysql",
  "host": "localhost",
  "port": 3306,
  "database": "erp",
  "username": "readonly",
  "password": "secret"
}
```

Response:

```json
{
  "success": true,
  "provider": "mysql"
}
```

Em caso de erro:

```json
{
  "success": false,
  "provider": "mysql",
  "message": "Não foi possível conectar ao banco."
}
```

---

## POST /api/schema

Realiza a introspecção do banco utilizando o provider informado.

Request:

```json
{
  "provider": "mysql",
  "host": "localhost",
  "port": 3306,
  "database": "erp",
  "username": "readonly",
  "password": "secret"
}
```

Response:

```json
{
  "provider": "mysql",
  "databaseName": "erp",
  "tables": [
    {
      "schema": "erp",
      "name": "clientes",
      "tableType": "BASE TABLE",
      "engine": "InnoDB",
      "columns": [
        {
          "name": "id",
          "dataType": "int",
          "fullDataType": "int unsigned",
          "nullable": false,
          "primaryKey": true,
          "foreignKey": false,
          "autoIncrement": true
        },
        {
          "name": "nome",
          "dataType": "varchar",
          "fullDataType": "varchar(255)",
          "nullable": false,
          "primaryKey": false,
          "foreignKey": false,
          "autoIncrement": false
        }
      ]
    }
  ],
  "relationships": []
}
```

O frontend deverá consumir esse contrato sem depender de consultas ou estruturas específicas do MySQL.

---

# 15. Frontend

## Tela inicial

A aplicação deverá apresentar uma tela para conexão:

```text
┌────────────────────────────────────────┐
│          Database Diagram              │
│                                        │
│ Provider                               │
│ [ MySQL                           ▼ ]  │
│                                        │
│ Host                                   │
│ [ localhost                          ] │
│                                        │
│ Port                                   │
│ [ 3306                               ] │
│                                        │
│ Database                               │
│ [ erp                                ] │
│                                        │
│ Username                               │
│ [ readonly                            ] │
│                                        │
│ Password                               │
│ [ ********                           ] │
│                                        │
│ [ Testar conexão ]                     │
│                                        │
└────────────────────────────────────────┘
```

O campo `Provider` deverá existir desde o MVP para preparar a interface para outros bancos. Inicialmente, apenas a opção MySQL estará habilitada.

Após a conexão:

```text
┌──────────────────────────────────────────────────────┐
│ Database Diagram                         🔍 Search   │
├──────────────────────────────────────────────────────┤
│                                                      │
│                                                      │
│        ┌───────────────┐                             │
│        │ clientes      │                             │
│        ├───────────────┤                             │
│        │ 🔑 id int     │                             │
│        │    nome varchar│                            │
│        └───────┬───────┘                             │
│                │                                      │
│                │                                      │
│        ┌───────▼────────┐                            │
│        │ pedidos        │                            │
│        ├────────────────┤                            │
│        │ 🔑 id int      │                            │
│        │ 🔗 cliente_id  │                            │
│        │    total decimal│                           │
│        └────────────────┘                            │
│                                                      │
└──────────────────────────────────────────────────────┘
```

---

# 16. Node de tabela

Cada tabela será representada por um React Flow custom node.

Exemplo:

```text
┌──────────────────────────┐
│ erp.customers            │
├──────────────────────────┤
│ 🔑 id       bigint       │
│    name     varchar      │
│    email    varchar      │
│ 🔗 company_id bigint     │
└──────────────────────────┘
```

Requisitos:

* nome da tabela;
* schema/database;
* lista de colunas;
* tipo da coluna;
* indicador de PK;
* indicador de FK;
* indicador de auto incremento;
* handles para relacionamentos;
* possibilidade de exibir comentários posteriormente;
* identificação visual do provider ou database quando necessário.

O componente deverá receber um modelo genérico de tabela, sem depender diretamente de propriedades exclusivas do MySQL.

---

# 17. Relacionamentos

Foreign Keys deverão ser representadas através de edges do React Flow.

Exemplo:

```text
customers
    │
    │ company_id → id
    ▼
companies
```

O edge deverá indicar visualmente que existe um relacionamento.

Quando disponível, a interface poderá exibir também:

* nome da constraint;
* regra `ON DELETE`;
* regra `ON UPDATE`;
* schema de origem;
* schema de destino.

Relacionamentos entre tabelas deverão utilizar identificadores compostos por schema e nome da tabela, evitando conflitos quando existirem tabelas com o mesmo nome em schemas diferentes.

---

# 18. Interações

O usuário deverá conseguir:

### Zoom

* zoom in;
* zoom out;
* fit view.

### Pan

Mover o diagrama livremente.

### Seleção

Selecionar uma tabela.

### Pesquisa

Pesquisar pelo nome da tabela.

Exemplo:

```text
Search: customer
```

Resultado:

```text
customers
customer_addresses
customer_orders
```

### Ocultar tabela

Permitir esconder uma tabela do diagrama.

### Mostrar tabela

Permitir restaurar uma tabela escondida.

### Filtrar por schema/database

Permitir filtrar tabelas por schema ou database quando a conexão disponibilizar mais de um schema.

### Filtrar por provider

O provider deverá ser exibido no contexto da conexão, mas não será necessário permitir múltiplos providers no mesmo diagrama no MVP.

---

# 19. Foco em uma tabela

Uma das principais funcionalidades deverá ser:

**"Explorar relacionamentos"**

Ao selecionar uma tabela, o usuário poderá solicitar:

```text
Mostrar relacionamentos
```

A aplicação deverá mostrar:

* tabela selecionada;
* tabelas diretamente relacionadas;
* relacionamentos dessas tabelas;
* opcionalmente níveis adicionais.

Exemplo:

```text
              addresses
                   │
                   ▼
companies ───── customers ───── orders
                                  │
                                  ▼
                              payments
```

Isso será especialmente importante em bancos grandes.

A funcionalidade deverá funcionar de forma independente do provider, utilizando o modelo comum de relacionamentos.

---

# 20. Layout automático

O diagrama deverá possuir uma opção:

**Organizar automaticamente**

O frontend deverá utilizar uma biblioteca de layout automático, como:

* ELK.js;
* Dagre.

O objetivo é reduzir sobreposição de nodes e melhorar a visualização dos relacionamentos.

O layout deverá considerar:

* direção dos relacionamentos;
* quantidade de colunas;
* quantidade de conexões;
* tabelas isoladas;
* agrupamento por schema, quando aplicável.

---

# 21. Estados da aplicação

A interface deverá possuir estados claros:

### Desconectado

```text
Nenhum banco conectado.
```

### Conectando

```text
Conectando ao banco MySQL...
```

### Carregando schema

```text
Lendo estrutura do banco MySQL...
```

### Sucesso

```text
Schema MySQL carregado.
```

### Erro

```text
Não foi possível carregar o schema.

Verifique:
- conexão;
- host e porta;
- credenciais;
- permissões de leitura;
- nome do database;
- disponibilidade do banco.
```

Caso um provider futuro seja utilizado, as mensagens deverão ser geradas dinamicamente com base no provider selecionado.

---

# 22. Requisitos funcionais

## RF01

O sistema deve permitir selecionar o provider do banco de dados.

## RF02

O sistema deve permitir informar uma conexão MySQL.

## RF03

O sistema deve utilizar a porta padrão `3306` quando nenhuma porta for informada para MySQL.

## RF04

O sistema deve testar a conexão antes de carregar o schema.

## RF05

O sistema deve identificar os schemas ou databases acessíveis conforme o provider utilizado.

## RF06

O sistema deve identificar todas as tabelas acessíveis no database selecionado.

## RF07

O sistema deve identificar as colunas das tabelas.

## RF08

O sistema deve identificar chaves primárias.

## RF09

O sistema deve identificar chaves estrangeiras.

## RF10

O sistema deve identificar relacionamentos entre tabelas.

## RF11

O sistema deve apresentar tabelas visualmente.

## RF12

O sistema deve apresentar relacionamentos visualmente.

## RF13

O usuário deve conseguir mover as tabelas.

## RF14

O usuário deve conseguir aplicar zoom.

## RF15

O usuário deve conseguir pesquisar tabelas.

## RF16

O usuário deve conseguir ocultar tabelas.

## RF17

O usuário deve conseguir reorganizar automaticamente o diagrama.

## RF18

O usuário deve conseguir focar uma tabela e visualizar seus relacionamentos.

## RF19

O sistema deve identificar colunas com auto incremento quando essa informação estiver disponível.

## RF20

O sistema deve exibir tipos de dados compatíveis com o MySQL.

## RF21

O backend deve utilizar um provider específico para cada tipo de banco suportado.

## RF22

A adição de um novo provider não deve exigir alterações na lógica principal de renderização do frontend.

## RF23

O sistema deve impedir operações de escrita no banco conectado.

---

# 23. Requisitos não funcionais

## RNF01 — Somente leitura

A aplicação não deve alterar o banco conectado.

## RNF02 — Segurança

Credenciais não devem ser armazenadas ou exibidas em logs.

## RNF03 — Performance

A aplicação deve conseguir carregar schemas de bancos com centenas de tabelas.

## RNF04 — Responsividade

O diagrama deverá continuar navegável mesmo com grande quantidade de nodes.

## RNF05 — Extensibilidade

A arquitetura deverá permitir adicionar outros bancos futuramente por meio de novos providers.

## RNF06 — Compatibilidade

O MVP deverá ser compatível com versões suportadas do MySQL e deverá utilizar um conector compatível com o protocolo MySQL.

## RNF07 — Isolamento de providers

Consultas, regras e particularidades de cada banco deverão permanecer isoladas em seus respectivos providers.

## RNF08 — Contrato comum

O frontend deverá consumir um modelo de dados comum, independente do banco utilizado.

## RNF09 — Manutenibilidade

A inclusão de um novo banco deverá exigir o mínimo possível de alterações nas camadas existentes.

---

# 24. Critérios de aceite do MVP

O MVP será considerado concluído quando for possível:

1. iniciar o backend;
2. iniciar o frontend;
3. selecionar o provider MySQL;
4. informar uma conexão MySQL;
5. utilizar a porta padrão `3306`;
6. testar a conexão;
7. carregar o schema;
8. visualizar todas as tabelas;
9. visualizar colunas;
10. identificar PKs;
11. identificar FKs;
12. visualizar relacionamentos;
13. mover tabelas;
14. aplicar zoom;
15. pesquisar tabelas;
16. organizar automaticamente o diagrama;
17. selecionar uma tabela;
18. visualizar somente os relacionamentos relevantes;
19. identificar colunas auto incremento;
20. confirmar que nenhuma operação de escrita é realizada no banco;
21. confirmar que a lógica de introspecção está isolada no `MySqlSchemaProvider`;
22. confirmar que o contrato retornado pela API não depende de estruturas específicas do frontend;
23. confirmar que a arquitetura permite adicionar um novo provider futuramente.

---

# 25. Fora do escopo do MVP

Não implementar inicialmente:

* edição de tabelas;
* criação de tabelas;
* alteração de colunas;
* execução de SQL;
* INSERT;
* UPDATE;
* DELETE;
* migrations;
* gerenciamento de usuários;
* autenticação;
* compartilhamento de diagramas;
* persistência de conexões;
* suporte a múltiplos providers no mesmo diagrama;
* geração de documentação;
* comparação de schemas;
* visualização de views;
* visualização de procedures;
* visualização de triggers;
* coleta detalhada de índices;
* implementação de providers adicionais além do MySQL.

Essas funcionalidades poderão ser avaliadas posteriormente.

---

# 26. Roadmap

## V1 — MVP

* arquitetura baseada em providers;
* MySQL;
* conexão;
* introspecção;
* tabelas;
* colunas;
* PK;
* FK;
* relacionamentos;
* React Flow;
* pesquisa;
* zoom/pan;
* layout automático;
* identificação de auto incremento;
* contrato de schema independente do banco.

## V1.1

* índices;
* views;
* filtros por schema/database;
* quantidade de registros aproximada;
* painel de detalhes da tabela;
* comentários de tabelas e colunas;
* exportação SVG/PNG;
* melhorias na visualização de metadados específicos do MySQL.

## V1.2

* níveis de relacionamento;
* foco em tabela;
* agrupamento por schema/database;
* minimap;
* atalhos de teclado;
* visualização de regras `ON DELETE` e `ON UPDATE`;
* melhorias na seleção de providers;
* suporte a MariaDB, caso necessário.

## V2

* PostgreSQL;
* SQL Server;
* SQLite;
* múltiplas conexões;
* salvar diagramas;
* compartilhar diagramas;
* comparação entre schemas;
* suporte a múltiplos bancos no mesmo projeto;
* gerenciamento de providers configuráveis.

---

# 27. Possíveis funcionalidades futuras

### Documentação automática

Gerar documentação:

```text
customers

Descrição:
Tabela responsável pelos clientes.

Relacionamentos:
- companies
- addresses
- orders
```

### Comparação de schemas

```text
Database A                 Database B

customers.id         →     customers.id
customers.name       →     customers.name
customers.phone      →     customers.phone

                           + customers.status
```

### Detecção de problemas

A ferramenta poderá futuramente identificar:

* tabelas sem PK;
* tabelas sem relacionamentos;
* FKs sem índice;
* nomes inconsistentes;
* tabelas muito acopladas;
* ciclos de relacionamento;
* colunas potencialmente redundantes;
* tabelas sem engine compatível;
* relacionamentos com regras de exclusão potencialmente perigosas;
* incompatibilidades entre providers;
* diferenças de tipos de dados entre bancos;
* recursos disponíveis em um banco, mas não suportados por outro.

### Suporte a novos bancos

Cada novo banco deverá possuir:

* provider próprio;
* consultas específicas de introspecção;
* testes de compatibilidade;
* mapeamento para o modelo comum;
* tratamento de tipos de dados específicos;
* documentação das limitações conhecidas.

---

# 28. Métrica principal do produto

A principal métrica do MVP será:

> **Tempo necessário para entender os relacionamentos de um banco de dados existente.**

O produto será considerado bem-sucedido se permitir que um desenvolvedor consiga responder rapidamente:

> "O que se relaciona com esta tabela?"

sem precisar navegar manualmente pelo banco ou procurar dezenas de tabelas.

A expansão para outros bancos deverá preservar essa mesma experiência, independentemente do provider utilizado.

---

# 29. Princípios do projeto

### Simplicidade

Não adicionar abstrações antes de existir necessidade real, mas manter os contratos necessários para suportar novos bancos.

### Read-only

O produto é uma ferramenta de exploração, não de administração.

### Visual first

O diagrama é o principal produto da aplicação.

### MySQL first

O MVP será desenvolvido e validado inicialmente com MySQL.

### Provider-based

Cada banco deverá possuir uma implementação isolada de introspecção.

### Contrato comum

O frontend e as camadas principais não devem depender de detalhes específicos de um banco.

### Extensível

O suporte inicial será MySQL, mas a arquitetura deve permitir outros providers.

### Performance

Um banco grande não deve tornar o diagrama inutilizável.

### Segurança

Credenciais e dados do banco devem ser tratados como informações sensíveis.

---

# 30. Primeiro milestone

O primeiro milestone será:

> **Conectar em um MySQL e renderizar automaticamente um diagrama com tabelas, colunas, PKs e FKs utilizando uma arquitetura preparada para outros bancos.**

Fluxo:

```text
[Selecionar provider MySQL]
        ↓
[Informar conexão MySQL]
        ↓
[Testar conexão]
        ↓
[Resolver MySqlSchemaProvider]
        ↓
[Consultar information_schema]
        ↓
[Construir DatabaseSchema comum]
        ↓
[Retornar JSON]
        ↓
[React recebe schema]
        ↓
[Gerar React Flow Nodes]
        ↓
[Gerar React Flow Edges]
        ↓
[Aplicar layout automático]
        ↓
[Diagrama pronto]
```

Esse fluxo deve funcionar antes de qualquer funcionalidade adicional ser implementada.

A arquitetura deverá permitir que, futuramente, o fluxo seja alterado apenas na etapa de resolução do provider:

```text
[Selecionar provider]
        ↓
[Resolver provider correspondente]
        ↓
[Executar introspecção específica]
        ↓
[Construir DatabaseSchema comum]
        ↓
[Renderizar diagrama]
```

Assim, a inclusão de PostgreSQL, SQL Server, SQLite ou outros bancos não deverá exigir alterações significativas na lógica de visualização do produto.
