# Data Model: Filtro de Relevância de Tabelas na Introspecção MySQL

## Entidades

### Tabela candidata (interna à consulta, não persistida em modelo)

Representa uma linha retornada por `information_schema.TABLES` durante a
descoberta de tabelas, antes da aplicação do limite de segurança. É um conceito
puramente local ao método de descoberta — **não** vira um novo tipo/classe.

| Campo (origem SQL) | Uso |
|---|---|
| `TABLE_NAME` | Nome da tabela (já existente, mapeado para `TabelaDeBanco.Nome`) |
| `TABLE_TYPE` | Tipo da tabela (já existente, mapeado para `TabelaDeBanco.TipoDaTabela`) |
| `ENGINE` | Motor de armazenamento (já existente, mapeado para `TabelaDeBanco.Motor`) |
| `TABLE_COMMENT` | Comentário (já existente, mapeado para `TabelaDeBanco.Comentario`) |
| `TABLE_ROWS` | **Novo uso**: quantidade estimada de registros; usado apenas para o filtro (`> 10`) e a ordenação (`DESC`) na própria consulta SQL. Lido do `MySqlDataReader` mas **descartado** após a seleção — não é atribuído a nenhuma propriedade de `TabelaDeBanco`. |

### `TabelaDeBanco` (modelo existente, inalterado)

Nenhum campo novo é adicionado. Continua representando exatamente o que já
representa hoje: nome, esquema, tipo, motor, comentário e colunas. A
"relevância" (quantidade estimada de registros) não é uma característica da
tabela que precise ser exposta ao restante da aplicação — é apenas um critério
de seleção usado no momento da descoberta.

### `EsquemaDeBanco` (modelo existente, inalterado)

Nenhuma mudança. O schema final continua contendo somente as tabelas
selecionadas como relevantes (já filtradas antes de chegar a este ponto do
fluxo), sem qualquer indicação explícita de "por que" foram selecionadas.

## Validação / Regras

- `TABLE_ROWS > 10` é aplicado no `WHERE` da consulta SQL de descoberta de
  tabelas (não em memória), evitando trazer tabelas irrelevantes para a
  aplicação.
- Tabelas com `TABLE_ROWS` nulo (estatística indisponível) não satisfazem
  `TABLE_ROWS > 10` em SQL padrão (comparação com `NULL` é falsa), portanto são
  automaticamente excluídas — consistente com o edge case da spec ("tratada
  como não relevante").
- `ORDER BY TABLE_ROWS DESC` é aplicado antes do `LIMIT 501`, garantindo que,
  havendo mais de 500 tabelas relevantes, as descartadas sejam as de menor
  quantidade estimada de registros.
- O corte para `LimiteDeTabelasParaIntrospeccao` (500) e a marcação de
  `introspeccaoTruncada`/`Aviso` em `EsquemaDeBanco` permanecem exatamente como
  hoje (nenhuma mudança de contrato).

## Relacionamentos

Sem mudança: `RelacionamentoDeBanco` (chaves estrangeiras) continua sendo
coletado apenas para as tabelas selecionadas, exatamente como no fluxo atual.
Uma FK que aponte para uma tabela fora do conjunto selecionado (por ter
`TABLE_ROWS <= 10`) simplesmente não aparece no resultado — comportamento já
coberto como edge case na especificação, sem necessidade de tratamento
especial no código (a consulta de FKs já é escopada por `TABLE_NAME =
@tabela`, então não referencia tabelas fora do conjunto selecionado como
origem).
