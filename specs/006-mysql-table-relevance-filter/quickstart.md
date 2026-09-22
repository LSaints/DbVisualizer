# Quickstart: Validando o Filtro de Relevância de Tabelas (MySQL)

Esta mudança é interna ao `MySqlProvedorDeSchema` (ver [data-model.md](./data-model.md)
e [research.md](./research.md)). Não há contrato de API novo — o endpoint de
introspecção existente passa a retornar um subconjunto diferente de tabelas.

## Pré-requisitos

- Backend `DatabaseDiagram.Api` rodando localmente (ou testes de integração/unitários do provider).
- Um banco MySQL de teste acessível, com:
  - Ao menos uma tabela com `TABLE_ROWS <= 10` (ex.: tabela vazia recém-criada).
  - Ao menos uma tabela com `TABLE_ROWS > 10` e chave primária.
  - Duas tabelas com `TABLE_ROWS > 10` relacionadas por chave estrangeira.

## Validação via teste automatizado (recomendado)

```bash
cd backend
dotnet test DatabaseDiagram.Testes --filter MySqlProvedorDeSchemaTestes
```

Cenários que os testes do provider devem cobrir (ver spec, User Stories 1–3):

1. **Filtro `TABLE_ROWS > 10`**: consulta gerada/usada pelo provider contém a
   condição de filtro; tabelas com `TABLE_ROWS <= 10` não aparecem no resultado
   de `ObterTabelasAsync`/`ObterEsquemaAsync`.
2. **Ordenação por relevância**: dado um conjunto de tabelas com `TABLE_ROWS`
   variados, o resultado retorna na ordem decrescente de `TABLE_ROWS`.
3. **Limite máximo de tabelas**: com mais de 500 tabelas relevantes simuladas,
   apenas as 500 com maior `TABLE_ROWS` são introspeccionadas e
   `EsquemaDeBanco.Aviso` é preenchido (reutilizar `CriarAvisoDeTruncamento`).
4. **Preservação de PK/FK/colunas**: para tabelas relevantes selecionadas,
   colunas, chave primária e relacionamentos continuam presentes no
   `EsquemaDeBanco` montado, exatamente como antes da mudança.

## Validação manual (ponta a ponta, opcional)

1. Subir o backend: `dotnet run --project backend/DatabaseDiagram.Api`.
2. Conectar via API/frontend ao banco de teste descrito em Pré-requisitos.
3. Confirmar no diagrama resultante:
   - A tabela com `TABLE_ROWS <= 10` **não aparece**.
   - A tabela com `TABLE_ROWS > 10` aparece com suas colunas e, se tiver PK,
     a coluna correspondente está marcada como chave primária.
   - O relacionamento por FK entre as duas tabelas relevantes aparece no
     diagrama.

## Critério de sucesso

Corresponde às Success Criteria da spec (SC-001 a SC-004): tabelas com
`TABLE_ROWS <= 10` ficam 100% fora do resultado; em bancos com excesso de
tabelas relevantes, as exibidas são exatamente as de maior `TABLE_ROWS` até o
limite; PKs/FKs de tabelas exibidas continuam 100% corretas; nenhuma consulta
adicional por tabela é introduzida (sem regressão de performance).
