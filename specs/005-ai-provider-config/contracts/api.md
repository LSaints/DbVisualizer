# Contratos da API — Configuração e Seleção de Provedores de IA no Assistente

Interface externa: API REST consumida pelo frontend (`ApiAssistente.ts`) e por
scripts/curl de validação local. Estende os contratos das features 002 e 003.

**Convenções (mantidas das 002/003)**:

- Erros de validação: `400` com `mensagem` pt-BR; inexistente: `404`; conflito
  de contexto: `409`; limite: `429`.
- Nenhuma resposta contém `chaveDeApi`, senha ou segredo; tokens **nunca**
  trafegam em resposta (apenas no **corpo do pedido** de `consultas`, como já
  ocorre — efêmero).
- Campos JSON camelCase.
- O contrato estruturado da resposta do assistente (002) **não muda**.

## Resumo de mudanças nesta feature

| Recurso | Mudança |
|---------|---------|
| `GET /api/assistente/provedores` | **Sem mudança** — a fábrica continua listando os provedores suportados; o seletor de badges e o modal consomem essa lista. |
| `POST /api/assistente/consultas` | **Sem mudança de contrato** — `provedorDeIa` + `chaveDeApi` já informados; o backend passa a **gravar** `provedorDeIa` na conversa (`persistirTroca`). |
| `POST /api/conversas` | **Sem mudança.** |
| `GET /api/conversas/{id}` | **+1 campo opcional** `provedorDeIa` (idem aplicável a uma nova conversa já com trocas). |
| `GET /api/conversas` | **Sem mudança** (o resumo da lista não expõe provedor — decisão de simplicidade). |
| `PATCH /api/conversas/{id}/titulo` / `DELETE /api/conversas/{id}` | **Sem mudança.** |

> Não há endpoint de gestão de tokens no backend: a configuração de tokens é
> estado de sessão no frontend (constituição III), e o token já viaja no corpo
> de cada consulta. Isso mantém o princípio de credenciais efêmeras.

## GET /api/conversas/{id}

Restaura a conversa completa, agora incluindo o provedor de IA selecionado.

**Resposta 200**:

```json
{
  "id": "4f2c8a6e-81d2-4b5a-9c3f-000000000001",
  "titulo": "Contratos com status X",
  "criadaEm": "2026-09-16T10:15:00Z",
  "atualizadaEm": "2026-09-16T10:17:00Z",
  "provedorDeIa": "openai",
  "contextoDeBanco": { "provedor": "mysql", "nomeDoBanco": "erp", "versao": "8.0.35" },
  "mensagens": [
    {
      "papel": "usuario",
      "conteudo": "quero consultar todos os contratos com cliente com status X",
      "criadaEm": "2026-09-16T10:15:00Z"
    }
  ]
}
```

| Campo        | Tipo     | Descricao                                                                 |
|--------------|----------|---------------------------------------------------------------------------|
| provedorDeIa | string?  | Identificador (`openai`/`google-ai-studio`/`claude`…) da última seleção da conversa; **ausente/nulo em conversas antigas** → frontend usa o padrão da sessão (FR-012). |

**Resposta 404**:

```json
{ "mensagem": "Conversa não encontrada." }
```

## POST /api/assistente/consultas

Contrato de **pedido** inalterado (corpo com `provedorDeIa`, `chaveDeApi`,
`mensagem`, `contextoDeBanco`, `conversaId?`). Mudança **de efeito colateral**:
quando `conversaId` é informado, o `provedorDeIa` válido é gravado na conversa
em `PersistirTrocaAsync` (sucesso apenas), de modo que um `GET /api/conversas/{id}`
posterior devolva o `provedorDeIa`.

**Resposta 200**: o contrato estruturado (002) — inalterado, inclusive o header
`X-Contexto-Truncado` (003).

**Erros (inalterados)**: `400` provedor não suportado / mensagem inválida /
contexto ausente; `404` conversa não encontrada; `409` banco divergente; `422`
resposta não interpretável ou com escrita; `429` limite; `502` falha do
provedor com mensagem genérica (nunca a chave).

## Endpoints consumidos na etapa de configuração (frente/sessão)

- `GET /api/assistente/provedores` → lista `{ "provedor", "rotulo" }` usada
  pelo **modal** (para cadastrar tokens) e pelo **seletor de badges** (para
  exibir os provedores já configurados na sessão).
- `POST /api/assistente/consultas` → recebe o token do provedor selecionado
  (`chaveDeApi`) conforme o mapa de sessão.