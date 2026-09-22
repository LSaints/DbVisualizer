# Research: Filtro de Relevância de Tabelas na Introspecção MySQL

## 1. Critério de relevância e fonte do dado

**Decision**: Usar `information_schema.TABLES.TABLE_ROWS` como estimativa de
quantidade de registros e filtrar `TABLE_ROWS > 10` diretamente na consulta SQL
existente de descoberta de tabelas.

**Rationale**: `TABLE_ROWS` já é coletado por estatísticas internas do MySQL
(via `ANALYZE TABLE`/`information_schema` cache) sem custo de consulta adicional.
Aplicar o filtro no próprio `WHERE` da consulta evita trazer tabelas irrelevantes
para a aplicação e mantém a decisão de relevância no banco, onde os dados de
estatística já existem.

**Alternatives considered**:
- `SELECT COUNT(*) FROM tabela` por tabela — rejeitado explicitamente pelo
  requisito do usuário (N+1, carga desnecessária no banco).
- Consultar `information_schema.INNODB_TABLESTATS` — mais preciso para InnoDB,
  mas adiciona uma segunda fonte de dados/joins para um ganho marginal frente ao
  objetivo (filtro simples "vazio vs. com dados"); native `TABLE_ROWS` já é
  suficiente para o critério pedido (>10).
- Filtrar em memória (C#) após trazer todas as tabelas — rejeitado por trazer
  tabelas irrelevantes da rede/banco desnecessariamente e por não permitir usar
  `ORDER BY ... LIMIT` do próprio SQL para já cortar no tamanho certo.

## 2. Ordenação e limite de segurança

**Decision**: Ordenar por `TABLE_ROWS DESC` na mesma consulta, mantendo
`LIMIT 501` (= `LimiteDeConsultaDeTabelas`) e a lógica atual em C# que corta para
`LimiteDeTabelasParaIntrospeccao` (500) e marca `introspeccaoTruncada` quando o
resultado retorna 501 linhas.

**Rationale**: Reaproveita a técnica já existente de "pedir um a mais que o
limite para detectar truncamento sem contagem extra". Como a consulta agora
ordena por relevância (linhas estimadas) em vez de nome, o truncamento passa a
descartar as tabelas menos relevantes em vez das últimas em ordem alfabética —
exatamente o comportamento pedido (US2).

**Alternatives considered**:
- Manter `ORDER BY TABLE_NAME` e ordenar por `TABLE_ROWS` apenas em memória —
  rejeitado porque, combinado com `LIMIT 501` no SQL, poderia cortar tabelas
  relevantes antes de chegarem à aplicação (o corte por nome aconteceria antes
  do corte por relevância).
- Remover o `LIMIT` do SQL e paginar/limitar somente em C# — rejeitado por
  aumentar a quantidade de dados trafegados do banco sem necessidade e por ir
  contra a diretriz explícita de manter os limites e a query com `LIMIT 501`.

## 3. Onde `TABLE_ROWS` vive no código

**Decision**: `TABLE_ROWS` é lido pelo leitor de dados (`MySqlDataReader`) dentro
do método privado de descoberta de tabelas (`ObterTabelasAsync`) e usado
somente para a ordenação/seleção local antes de ser descartado. Não é adicionado
como propriedade em `TabelaDeBanco` nem em `EsquemaDeBanco`.

**Rationale**: Nenhuma outra parte do sistema (frontend, DTOs de resposta,
outros providers) consome quantidade de linhas hoje. Adicionar o campo ao
modelo compartilhado violaria a constituição I (simplicidade — nenhuma
abstração/campo sem necessidade demonstrada) e o requisito explícito do usuário
de não alterar modelos apenas para guardar valor não utilizado.

**Alternatives considered**:
- Adicionar `QuantidadeEstimadaDeRegistros` em `TabelaDeBanco` para uso futuro —
  rejeitado por YAGNI; pode ser adicionado depois se um caso de uso real
  aparecer (ex.: exibir a estimativa na UI).

## 4. Comentário de documentação da limitação

**Decision**: Adicionar um comentário XML/inline junto à constante de consulta
e/ou ao ponto onde `TABLE_ROWS` é lido, explicando que é uma estimativa (não uma
contagem exata, especialmente para tabelas InnoDB) e não deve ser confundido com
`COUNT(*)`.

**Rationale**: Requisito explícito do usuário para evitar interpretação futura
incorreta do campo.

**Alternatives considered**: N/A — requisito direto, sem alternativas a avaliar.

## Resumo

Todos os itens do Technical Context do plano já estavam resolvidos a partir da
especificação e do código existente; nenhum `NEEDS CLARIFICATION` permaneceu.
Este documento registra as decisões de design de baixo nível (fonte do dado,
posição do filtro/ordenação na query, escopo do campo `TABLE_ROWS` e
documentação da limitação) que orientam a Fase 1.
