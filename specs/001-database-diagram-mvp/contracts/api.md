# Contratos da API — Database Diagram MVP

Interface externa da aplicação: uma API REST consumida pelo frontend (e por
scripts/curl para validação local). Formato JSON, chaves em pt-BR (camelCase),
conforme a decisão D1 em `research.md`.

Convenções:

- Erros de validação de entrada retornam `400` com `mensagem` em pt-BR.
- Erros de conexão/introspecção retornam `400`/`502` com `mensagem` genérica e
  **sem** expor senha, connection string ou detalhes internos.
- O modelo de resposta de schema segue `data-model.md`
  (`EsquemaDeBanco` e agregados).

## POST /api/conexoes/teste

Testa uma conexão **sem armazená-la**. Não retorna nem persiste a senha.

**Corpo da requisição** (`RequisicaoConexao`):

```json
{
  "provedor": "mysql",
  "host": "localhost",
  "porta": 3306,
  "bancoDeDados": "erp",
  "usuario": "readonly",
  "senha": "secret"
}
```

**Resposta 200 — sucesso**:

```json
{
  "sucesso": true,
  "provedor": "mysql"
}
```

**Resposta 400 — erro de conexão ou validação**:

```json
{
  "sucesso": false,
  "provedor": "mysql",
  "mensagem": "Não foi possível conectar ao banco."
}
```

Campos:

| Campo      | Descricao                                            |
|-----------|------------------------------------------------------|
| sucesso   | `true` se conectou; `false` caso contrário           |
| provedor  | Provider da conexão testada                          |
| mensagem  | Presente somente em erro; texto em pt-BR             |

## POST /api/esquema

Realiza a introspecção (somente leitura) do banco informado e retorna o schema.

**Corpo da requisição**: igual a `RequisicaoConexao` acima.

**Resposta 200 — sintetizado** (esquema completo em `data-model.md`):

```json
{
  "provedor": "mysql",
  "nomeDoBanco": "erp",
  "tabelas": [
    {
      "esquema": "erp",
      "nome": "clientes",
      "tipoDaTabela": "BASE TABLE",
      "motor": "InnoDB",
      "comentario": null,
      "colunas": [
        {
          "nome": "id",
          "tipoDoDado": "int",
          "tipoCompletoDoDado": "int unsigned",
          "valorPadrao": null,
          "comentario": null,
          "posicaoOrdinal": 1,
          "podeSerNula": false,
          "chavePrimaria": true,
          "chaveEstrangeira": false,
          "autoIncremento": true
        }
      ]
    }
  ],
  "relacionamentos": [
    {
      "esquemaOrigem": "erp",
      "tabelaOrigem": "pedidos",
      "colunaOrigem": "cliente_id",
      "esquemaDestino": "erp",
      "tabelaDestino": "clientes",
      "colunaDestino": "id",
      "nomeDaRestricao": "fk_pedidos_clientes",
      "regraDeAtualizacao": "CASCADE",
      "regraDeExclusao": "RESTRICT"
    }
  ]
}
```

**Resposta 502 — erro de conexão/introspecção**:

```json
{
  "mensagem": "Não foi possível carregar o schema."
}
```

Campos principais (`EsquemaDeBanco`):

| Campo            | Tipo       | Descricao                              |
|------------------|------------|----------------------------------------|
| provedor         | string     | Provider que gerou o schema            |
| nomeDoBanco      | string     | Nome do database                       |
| tabelas          | array      | Lista de `TabelaDeBanco`               |
| relacionamentos  | array      | Lista de `RelacionamentoDeBanco`       |

Validação de entrada (ambos os endpoints):

| Condicao                         | Resultado                                   |
|----------------------------------|---------------------------------------------|
| `provedor` nulo/inválido         | `400` com mensagem em pt-BR                 |
| `host`, `bancoDeDados`, `usuario`, `senha` vazios | `400` com mensagem em pt-BR   |
| `porta <= 0`                     | `400` com mensagem em pt-BR                 |
| `porta` ausente                  | Usado o padrão do provider (`3306` MySQL)   |

## Consideracoes de seguranca (constituição G6)

- A API nunca retorna a `senha`.
- A connection string completa nunca aparece em resposta ou em log.
- Mensagens de erro não revelam detalhes internos que possam facilitar ataques.
- Todos os comandos executados contra o banco são consultas de leitura
  predefinidas de metadados (constituição G3).