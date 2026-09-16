# Modelo de Dados — Database Diagram MVP

Modelo de domínio **independente de banco** (contrato comum) usado como ligação
entre as camadas do backend e entre backend e frontend. Os nomes seguem a
convenção pt-BR da constituição (decisão D1 em `research.md`).

Regras transversais:

- Tabelas são identificadas por **esquema + nome** para evitar conflito quando
  existirem tabelas homônimas em schemas diferentes.
- Campos opcionais são provenientes de metadados e DEVEM ser `null` quando o
  banco não informar o valor.
- Nenhum campo deste modelo contém dados sensíveis; a `ConexaoDeBanco` nunca é
  retornada em resposta de API.

## Entidades

### ConexaoDeBanco

Parâmetros de conexão informados pelo usuário. **Transitória**: criada em
memória por requisição e descartada ao fim. Nunca persistida, nunca logada.

| Campo            | Tipo     | Validacao                                    |
|------------------|----------|----------------------------------------------|
| provedor         | string   | Obrigatório. MVP: `"mysql"`                  |
| host             | string   | Obrigatório                                  |
| porta            | int      | Default `3306` para MySQL; deve ser > 0      |
| bancoDeDados     | string   | Obrigatório                                  |
| usuario          | string   | Obrigatório                                  |
| senha            | string   | Obrigatória; nunca retornada nem logada      |

### EsquemaDeBanco

Raiz do schema descoberto em um database.

| Campo             | Tipo                    | Descricao                                   |
|-------------------|-------------------------|---------------------------------------------|
| provedor          | string                  | Provider que gerou o schema                 |
| nomeDoBanco       | string                  | Nome do database                            |
| charset           | string?                 | Charset default, quando disponível          |
| collation         | string?                 | Collation default, quando disponível        |
| tabelas           | `List<TabelaDeBanco>`   | Tabelas do database                         |
| relacionamentos   | `List<RelacionamentoDeBanco>` | Relações por chave estrangeira        |

### TabelaDeBanco

Representa uma tabela (MVP prioriza `BASE TABLE`).

| Campo            | Tipo                  | Descricao                                        |
|------------------|-----------------------|--------------------------------------------------|
| esquema          | string                | Nome do schema/database                          |
| nome             | string                | Nome da tabela                                   |
| tipoDaTabela     | string?               | Ex.: `BASE TABLE`                                |
| motor            | string?               | Engine, ex.: `InnoDB`                            |
| comentario       | string?               | Comentário da tabela, quando disponível          |
| colunas          | `List<ColunaDeBanco>` | Colunas em ordem de posição                      |

### ColunaDeBanco

Representa uma coluna de uma tabela.

| Campo                 | Tipo    | Descricao                                        |
|-----------------------|---------|--------------------------------------------------|
| nome                  | string  | Nome da coluna                                   |
| tipoDoDado            | string  | Tipo base, ex.: `int`, `varchar`                 |
| tipoCompletoDoDado    | string? | Tipo completo, ex.: `int unsigned`, `varchar(255)` |
| valorPadrao           | string? | Default da coluna, quando disponível             |
| comentario            | string? | Comentário da coluna, quando disponível          |
| posicaoOrdinal        | int     | Posição da coluna na tabela (1-based)            |
| podeSerNula           | bool    | Se a coluna aceita NULL                          |
| chavePrimaria         | bool    | Se participa da chave primária                   |
| chaveEstrangeira      | bool    | Se é origem de uma FK                             |
| autoIncremento        | bool    | Se possui auto incremento, quando disponível     |

### RelacionamentoDeBanco

Representa uma chave estrangeira (origem → destino).

| Campo                  | Tipo   | Descricao                                        |
|------------------------|--------|--------------------------------------------------|
| esquemaOrigem          | string | Schema da tabela origem                          |
| tabelaOrigem           | string | Tabela que possui a FK                           |
| colunaOrigem           | string | Coluna da FK                                     |
| esquemaDestino         | string | Schema da tabela destino                         |
| tabelaDestino          | string | Tabela referenciada                              |
| colunaDestino          | string | Coluna referenciada (normalmente a PK)           |
| nomeDaRestricao        | string?| Nome da constraint, quando disponível            |
| regraDeAtualizacao     | string?| Regra `ON UPDATE`, quando disponível             |
| regraDeExclusao        | string?| Regra `ON DELETE`, quando disponível             |

## Relações

- `EsquemaDeBanco` 1 → N `TabelaDeBanco` (via `tabelas`).
- `TabelaDeBanco` 1 → N `ColunaDeBanco` (via `colunas`, ordenadas por
  `posicaoOrdinal`).
- `RelacionamentoDeBanco` referencia tabelas de origem e destino por
  `esquema + nome (+ coluna)`, sem objeto aninhado — mantém o modelo simples
  e serializável em linha.

## Regras de validacao aplicadas no contrato de entrada

- `ConexaoDeBanco`: campos obrigatórios preenchidos; `porta > 0`; `provedor`
  com valor suportado (`mysql`). Falhas retornam mensagem em pt-BR.
- Nenhuma validacao adicional no modelo de saída (o schema reflete o banco tal
  qual).

## Detalhes especificos do provider (MySQL)

- Schemas disponíveis vêm de `information_schema.schemata`.
- Tabelas de `information_schema.tables` com `TABLE_TYPE = 'BASE TABLE'`.
- Colunas de `information_schema.columns`.
- PKs de `information_schema.table_constraints` +
  `information_schema.key_column_usage`.
- FKs de `information_schema.key_column_usage` +
  `information_schema.referential_constraints` (regras `ON UPDATE`/`ON DELETE`).
- Valores técnicos (ex.: `InnoDB`, `BASE TABLE`, tipos) são preservados como
  vêm do banco, conforme exceção de nomenclatura da constituição.