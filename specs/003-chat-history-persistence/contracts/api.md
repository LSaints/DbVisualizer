# Contratos da API — Persistência do Histórico do Chat do Assistente

Interface externa da persistência de conversas: API REST consumida pelo
frontend (e por scripts/curl para validação local). Estende os contratos da
feature 002 (`contracts/api.md` da 002), preservando o corpo do `200` de
`POST /api/assistente/consultas` (contrato estruturado intacto).

Convenções (mesmas da 002/MVP 001):

- Erros de validação de entrada: `400` com `mensagem` em pt-BR.
- Recurso inexistente: `404` com `mensagem` em pt-BR.
- Conflito de contexto de banco: `409` com `mensagem` em pt-BR.
- Limites de requisições: `429` (rate limit mantido da 002).
- Nenhuma resposta contém `chaveDeApi`, senha, segredo ou **conteúdo de
  mensagens de outras conversas**; o corpo do `200` de consultas permanece o
  contrato estruturado.
- Campos JSON seguem camelCase (mesmo do MVP 001 / `PropertyNamingPolicy`).

## GET /api/conversas

Lista as conversas persistidas, ordenadas por `atualizadaEm` decrescente.

**Resposta 200**:

```json
[
  {
    "id": "4f2c8a6e-81d2-4b5a-9c3f-000000000001",
    "titulo": "Contratos com status X",
    "criadaEm": "2026-09-16T10:15:00Z",
    "atualizadaEm": "2026-09-16T10:17:00Z",
    "resumo": "quero consultar todos os contratos com cliente..."
  }
]
```

| Campo         | Tipo                  | Descricao                                        |
|---------------|-----------------------|--------------------------------------------------|
| id            | string (Guid)         | Identificador usado no `conversaId` e nas rotas  |
| titulo        | string                | Título da conversa (renomeável, US4)             |
| criadaEm      | string (ISO-8601)     | Data/hora de criação                             |
| atualizadaEm  | string (ISO-8601)     | Data/hora da última troca persistida             |
| resumo        | string                | Prévia curta da última troca; "Conversa vazia" se nenhuma |

## POST /api/conversas

Cria uma nova conversa vazia.

**Corpo da requisição** (opcional):

```json
{ "titulo": "Contratos de março" }
```

**Resposta 201**:

```json
{
  "id": "4f2c8a6e-81d2-4b5a-9c3f-000000000002",
  "titulo": "Contratos de março",
  "criadaEm": "2026-09-16T11:00:00Z",
  "atualizadaEm": "2026-09-16T11:00:00Z"
}
```

> Sem `titulo` no corpo, a conversa nasce com "Nova conversa" e o título é
> derivado da primeira mensagem (D6). A criação é barata: o chat nunca precisa
> de um id antes de enviar a primeira mensagem.

**Resposta 400** — título inválido (se informado e fora de 1–100 caracteres):

```json
{ "mensagem": "O título deve ter entre 1 e 100 caracteres." }
```

## GET /api/conversas/{id}

Restaura a conversa completa (para abrir e continuar).

**Resposta 200**:

```json
{
  "id": "4f2c8a6e-81d2-4b5a-9c3f-000000000001",
  "titulo": "Contratos com status X",
  "criadaEm": "2026-09-16T10:15:00Z",
  "atualizadaEm": "2026-09-16T10:17:00Z",
  "contextoDeBanco": { "provedor": "mysql", "nomeDoBanco": "erp", "versao": "8.0.35" },
  "mensagens": [
    { "papel": "usuario", "conteudo": "quero consultar todos os contratos com cliente com status X", "criadaEm": "2026-09-16T10:15:00Z" },
    {
      "papel": "assistente",
      "conteudo": {
        "consulta": "SELECT c.*, cl.* FROM contratos c INNER JOIN clientes cl ON cl.id = c.cliente_id WHERE c.status = 'X';",
        "explicacao": "A query consulta todos os contratos vinculados a clientes e filtra apenas os de status igual a 'X'.",
        "objetivo": "Encontrar contratos de clientes com status 'X'.",
        "tabelas": [],
        "relacionamentos": [],
        "filtros": [],
        "campos_retorno": {},
        "tipo_consulta": "SELECT",
        "resultado_esperado": "Lista de contratos com seus clientes, filtrados por status 'X'.",
        "parametros": []
      },
      "criadaEm": "2026-09-16T10:17:00Z"
    }
  ]
}
```

| Campo           | Tipo                      | Descricao                                            |
|-----------------|---------------------------|------------------------------------------------------|
| contextoDeBanco | objeto `IdentidadeDeBanco`| `provedor`, `nomeDoBanco`, `versao` (FR-013/D7)      |
| mensagens       | array                     | Trocas na ordem; `conteudo` do assistente é o contrato estruturado |

> O `conteudo` do papel `"assistente"` é exatamente a resposta estruturada da
> feature 002 (chaves do exemplo do usuário, incluindo `campos_retorno`,
> `tipo_consulta`, `resultado_esperado`). O frontend renderiza com o mesmo
> `CartaoDeRespostaDeConsulta`.

**Resposta 404**:

```json
{ "mensagem": "Conversa não encontrada." }
```

## PATCH /api/conversas/{id}/titulo

Renomeia a conversa.

**Corpo**:

```json
{ "titulo": "Contratos de março (revisado)" }
```

**Resposta 200**:

```json
{ "id": "4f2c8a6e-...", "titulo": "Contratos de março (revisado)" }
```

**Resposta 400** — título vazio/inválido:

```json
{ "mensagem": "Informe um título entre 1 e 100 caracteres." }
```

**Resposta 404** — conversa inexistente.

## DELETE /api/conversas/{id}

Exclui a conversa permanentemente (SC-006).

**Resposta 204** (sem corpo).

**Resposta 404** — conversa inexistente.

## POST /api/assistente/consultas (ajuste aditivo)

Corpo de mesma forma da 002, agora com **`conversaId` opcional**:

```json
{
  "provedorDeIa": "openai",
  "chaveDeApi": "sk-proj-...",
  "conversaId": "4f2c8a6e-81d2-4b5a-9c3f-000000000001",
  "mensagem": "na consulta anterior, também retorne o nome do cliente",
  "contextoDeBanco": { "provedor": "mysql", "versao": "8.0.35", "nomeDoBanco": "erp", "tabelas": [], "relacionamentos": [] }
}
```

**Header adicionado a toda resposta 200**: `X-Contexto-Truncado: true|false`
(FR-011) — `true` quando a janela de contexto omitiu mensagens antigas; o corpo
do `200` permanece **exatamente o contrato estruturado** da 002 (inalterado).

**Novos códigos de erro**:

| Condicao                                         | Resultado                                    |
|--------------------------------------------------|----------------------------------------------|
| `conversaId` não encontrado                      | `404` `{ "mensagem": "Conversa não encontrada." }` |
| banco do pedido difere do registrado na conversa | `409` `{ "mensagem": "O banco conectado difere do registrado nesta conversa." }` |
| `conversaId` ausente                            | comportamento legado: troca única, sem persistência |

**Semântica da continuação**: com `conversaId`, o backend carrega a conversa,
adiciona a mensagem do usuário, envia ao provedor o schema de contexto +
**janela das mensagens mais recentes** como contexto (FR-002), valida a
resposta e, em sucesso, persiste o par usuário+assistente (D5). Em falha nada
é persistido — o usuário pode tentar novamente.

## Consideracoes de seguranca (constituição G3/G6)

- Nenhuma resposta contém `chaveDeApi`, senha, segredo ou conteúdo de outras
  conversas. Mensagens persistidas e contexto são sanitizados de padrões de
  segredo (D8).
- Logs registram somente o provedor utilizado e o id de conversa; nunca o corpo
  da mensagem, a chave, a senha ou o contexto.
- O SQL gerado permanece somente exibido/copiado — nunca executado.
- O diretório de conversas fica fora da pasta servida; escrita atômica
  (temp + rename) e formato versionado (FR-016).
- Rate limit `AssistentePorCliente` mantido no `POST /api/assistente/consultas`
  (a lista/CRUD de conversas é barata e não tem rate limit dedicado no MVP).