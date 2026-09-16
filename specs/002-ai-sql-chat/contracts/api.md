# Contratos da API — Assistente IA de Consultas SQL

Interface externa do assistente: API REST consumida pelo frontend (e por
scripts/curl para validação local). Formato JSON; o contrato da resposta
estruturada usa as chaves exatas do exemplo fornecido pelo usuário
(`consulta`, `explicacao`, `tabelas`, `relacionamentos` com `tabela_origem` etc.).
O `POST /api/esquema` existente ganha um **campo aditivo** `versao` (não
quebra consumidores atuais).

Convenções (mesmas do MVP 001):

- Erros de validação de entrada retornam `400` com `mensagem` em pt-BR.
- Erros do provedor retornam `502` com `mensagem` genérica, sem expor chave.
- Resposta recebida mas não interpretável retorna `422` com mensagem orientada
  à nova tentativa.
- Limites de requisições retornam `429`.
- Nenhuma resposta contém a `chaveDeApi`, a senha do banco ou o contexto em claro;
  nenhuma consulta gerada é executada (somente exibida/copiada).

## GET /api/assistente/provedores

Lista os provedores de IA suportados (somente os implementados na fábrica).

**Resposta 200**:

```json
[
  { "provedor": "openai", "rotulo": "OpenAI" },
  { "provedor": "claude", "rotulo": "Claude (Anthropic)" },
  { "provedor": "gemini", "rotulo": "Google Gemini" }
]
```

| Campo     | Tipo   | Descricao                             |
|-----------|--------|---------------------------------------|
| provedor  | string | Identificador usado no corpo do POST  |
| rotulo    | string | Rótulo em pt-BR exibido ao usuário    |

## POST /api/assistente/consultas

Gera uma consulta SQL (somente leitura) a partir de um pedido em linguagem
natural, usando o contexto do banco enviado pelo frontend (provider + versão +
tabelas/colunas/relacionamentos). **Limitada por janela de tempo por cliente**
(`AssistentePorCliente`) e com timeout de 90 s.

**Corpo da requisição** (`RequisicaoDeConsultaDoAssistente`):

```json
{
  "provedorDeIa": "openai",
  "chaveDeApi": "sk-proj-...",
  "mensagem": "quero consultar todos os contratos com cliente com status X",
  "contextoDeBanco": {
    "provedor": "mysql",
    "versao": "8.0.35",
    "nomeDoBanco": "erp",
    "tabelas": [
      {
        "esquema": "erp",
        "nome": "contratos",
        "colunas": [
          { "nome": "id", "tipoDoDado": "int", "posicaoOrdinal": 1, "chavePrimaria": true },
          { "nome": "cliente_id", "tipoDoDado": "int", "posicaoOrdinal": 2, "chaveEstrangeira": true },
          { "nome": "status", "tipoDoDado": "varchar", "posicaoOrdinal": 3 }
        ]
      }
    ],
    "relacionamentos": [
      {
        "esquemaOrigem": "erp",
        "tabelaOrigem": "contratos",
        "colunaOrigem": "cliente_id",
        "esquemaDestino": "erp",
        "tabelaDestino": "clientes",
        "colunaDestino": "id"
      }
    ]
  }
}
```

> O `contextoDeBanco` é o mesmo `EsquemaDeBanco` retornado por
> `POST /api/esquema` (agora incluindo `versao`), enviado pelo frontend
> (decisão D2 em `research.md`).

**Resposta 200 — contrato estruturado** (corpo = `RespostaDeConsultaDoAssistente`,
chaves conforme o exemplo do usuário):

```json
{
  "consulta": "SELECT c.*, cl.* FROM contratos c INNER JOIN clientes cl ON cl.id = c.cliente_id WHERE c.status = 'X';",
  "explicacao": "A query consulta todos os contratos vinculados a clientes e filtra apenas os contratos cujo status seja igual a 'X'.",
  "objetivo": "Encontrar contratos de clientes que estejam com o status 'X'.",
  "tabelas": [
    { "nome": "contratos", "apelido": "c", "funcao": "Tabela principal da consulta, contendo os contratos." },
    { "nome": "clientes", "apelido": "cl", "funcao": "Tabela relacionada que contém os dados dos clientes." }
  ],
  "relacionamentos": [
    {
      "tabela_origem": "contratos",
      "campo_origem": "cliente_id",
      "tabela_destino": "clientes",
      "campo_destino": "id",
      "tipo": "INNER JOIN",
      "explicacao": "Relaciona cada contrato ao seu respectivo cliente."
    }
  ],
  "filtros": [
    {
      "campo": "contratos.status",
      "operador": "=",
      "valor": "X",
      "explicacao": "Retorna somente contratos cujo status seja 'X'."
    }
  ],
  "campos_retorno": {
    "contratos": "*",
    "clientes": "*",
    "explicacao": "Retorna todos os campos das tabelas contratos e clientes."
  },
  "tipo_consulta": "SELECT",
  "resultado_esperado": "Uma lista contendo os contratos com seus respectivos clientes, limitada aos contratos com status 'X'.",
  "parametros": [
    { "nome": "status", "valor": "X", "descricao": "Status utilizado para filtrar os contratos." }
  ]
}
```

> Chaves compostas em snake_case (`tabela_origem`, `campo_origem`,
> `tabela_destino`, `campo_destino`, `campos_retorno`, `tipo_consulta`,
> `resultado_esperado`) seguem o exemplo informado pelo usuário (D1). No C#,
> são mapeadas com `[JsonPropertyName]`; esta é a única parte do contrato que
> diverge da convenção camelCase do MVP 001.

**Resposta 400 — validação de entrada**:

```json
{ "mensagem": "Informe uma mensagem para gerar a consulta." }
```

**Resposta 422 — resposta não interpretável**:

```json
{ "mensagem": "A resposta do assistente não pôde ser interpretada. Tente novamente." }
```

**Resposta 429 — limite de requisições**:

```json
{ "mensagem": "Muitas requisições. Aguarde um instante e tente novamente." }
```

**Resposta 502 — falha do provedor (rede, chave, serviço, timeout)**:

```json
{ "mensagem": "Não foi possível gerar a consulta pelo provedor selecionado." }
```

Validação de entrada (`POST /api/assistente/consultas`):

| Condicao                                          | Resultado                        |
|---------------------------------------------------|----------------------------------|
| `provedorDeIa` vazio/ausente ou não suportado     | `400` com mensagem em pt-BR      |
| `chaveDeApi` vazia                                | `400` com mensagem em pt-BR      |
| `mensagem` vazia, menor que o mínimo ou maior que o máximo | `400` em pt-BR      |
| `contextoDeBanco` ausente ou sem `tabelas`        | `400` com mensagem em pt-BR      |
| corpo acima do tamanho máximo (ex.: 1 MB)         | `400`/`413` com mensagem em pt-BR|
| limite por cliente excedido                       | `429` em pt-BR                   |

## POST /api/esquema (ajuste aditivo)

Resposta passa a incluir `versao` (opcional), preenchido pelo provider:

```json
{
  "provedor": "mysql",
  "versao": "8.0.35",
  "nomeDoBanco": "erp",
  "tabelas": [],
  "relacionamentos": []
}
```

Demais campos e códigos permanecem conforme `contracts/api.md` do MVP 001.

## Consideracoes de seguranca (constituição G3/G6)

- A API nunca retorna `chaveDeApi` nem `senha`; a chave é usada apenas na
  requisição que a origina (memória) e nunca é logada.
- Mensagens de erro não revelam detalhes dos provedores nem do contexto.
- O SQL gerado é apenas exibido/copiado; nenhum comando é executado contra o
  banco (a aplicação mantém-se somente leitura).
- Logs registram somente o provedor utilizado (ex.: evento com
  `ProvedorDeIa`), nunca o corpo do pedido, a chave ou a senha.