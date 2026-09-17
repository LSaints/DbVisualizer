# Guia de Validacao Rapida — Persistência do Histórico do Chat do Assistente

Guia de validação ponta a ponta da persistência de conversas (feature 003).
Referências de detalhe: [contratos](contracts/api.md), [modelo de dados](data-model.md),
[decisões](research.md) e [spec](spec.md). Este arquivo é um guia de
execução/validação, não contém implementação.

## Pre-requisitos

- Feature 002 funcional (backup do guia `specs/002-ai-sql-chat/quickstart.md`):
  backend .NET 8 + frontend React/Vite, schema conectado e pelo menos um
  provedor de IA com chave de teste.
- Diretório de conversas configurado (padrão: `dados/conversas` sob o conteúdo
  da API) criado no primeiro uso; não precisa existir previamente.

## Execucao

### Backend

```sh
cd backend
dotnet restore
dotnet run --project DatabaseDiagram.Api
```

API na porta de desenvolvimento (tipicamente `http://localhost:5000`).

### Frontend

```sh
cd frontend
npm install
npm run dev
```

Aplicação em `http://localhost:5173`.

## Cenarios de validacao

### 1. Criar uma conversa

```sh
curl -s -X POST http://localhost:5000/api/conversas \
  -H "Content-Type: application/json" -d '{}'
```

**Esperado**: `201` com `id` (Guid), `titulo` "Nova conversa" e timestamps.

### 2. Listar conversas

```sh
curl -s http://localhost:5000/api/conversas
```

**Esperado**: `200` com a lista (inicialmente vazia ou contendo a conversa do
passo 1), ordenada por `atualizadaEm` decrescente, itens com `id`, `titulo`,
`criadaEm`, `atualizadaEm` e `resumo`.

### 3. Continuar uma conversa gerando uma consulta com contexto

Com o schema (`contextoDeBanco`) e usando o `id` do passo 1 como `conversaId`:

```sh
curl -s -D - -X POST http://localhost:5000/api/assistente/consultas \
  -H "Content-Type: application/json" \
  -d '{
    "provedorDeIa":"openai",
    "chaveDeApi":"<SUA_CHAVE>",
    "conversaId":"<ID_DO_PASSO_1>",
    "mensagem":"quero consultar todos os contratos com cliente com status X",
    "contextoDeBanco":{ "provedor":"mysql","versao":"8.0.35","nomeDoBanco":"erp","tabelas":[...],"relacionamentos":[...] }
  }'
```

**Esperado**:
- `200` com o **contrato estruturado intacto** (como na 002);
- header `X-Contexto-Truncado: false` (conversa curta);
- a consulta reflete o schema real (ex.: `INNER JOIN` com `WHERE status='X'`).

### 4. Follow-up: modificar a consulta anterior

Envie outro pedido **sem repetir o contexto**, apenas referenciando a consulta:

```sh
curl -s -X POST http://localhost:5000/api/assistente/consultas \
  -H "Content-Type: application/json" \
  -d '{
    "provedorDeIa":"openai", "chaveDeApi":"<SUA_CHAVE>",
    "conversaId":"<ID_DO_PASSO_1>",
    "mensagem":"na consulta anterior, inclua também o nome do cliente no retorno",
    "contextoDeBanco":{ "provedor":"mysql","versao":"8.0.35","nomeDoBanco":"erp","tabelas":[...],"relacionamentos":[...] }
  }'
```

**Esperado**: `200` com SQL que mantém a base da consulta anterior e adiciona o
campo solicitado — **evidência de que o histórico foi enviado como contexto**
(FR-002/FR-003/SC-002).

### 5. Persistência entre sessões API

- Reinicie o backend (`Ctrl+C` + `dotnet run ...`).
- `GET /api/conversas` → a conversa **continua listada** (SC-001).
- `GET /api/conversas/{id}` → histórico completo restaurado, `conteudo` do
  assistente com o contrato estruturado integral.

### 6. Renomear e excluir

```sh
curl -s -X PATCH http://localhost:5000/api/conversas/<ID> \
  -H "Content-Type: application/json" -d '{"titulo":"Contratos de março"}'

curl -s -X DELETE http://localhost:5000/api/conversas/<ID> -o /dev/null -w "%{http_code}"
```

**Esperado**: `200` com o novo título (persistente após reinício) e `204` na
exclusão; `GET /api/conversas` não lista mais a conversa excluída e reiniciar
não a traz de volta (SC-006).

### 7. Fluxo completo no chat (frontend)

1. Conectar ao MySQL e carregar o diagrama (fluxo do MVP).
2. Abrir a aba **Assistente de IA** → a **lista de conversas** é exibida.
3. **Nova conversa** → conversa vazia criada; digitar o 1º pedido → resposta
   estruturada; a conversa ganha título derivado da pergunta.
4. **Nova conversa** novamente → outra conversa é criada; as duas aparecem na
   lista.
5. Voltar para a 1ª conversa → **histórico restaurado** (pedidos e cartões
   completos, SQL copiável).
6. Enviar um follow-up referenciando a consulta anterior → a resposta altera a
   consulta anterior sem repetir contexto (SC-002).
7. **Renomear** e **excluir** (com confirmação) funcionam; a excluída some da
   lista e do disco.
8. **Avisos**: com um banco diferente conectado, abrir a conversa mostra o
   aviso de contexto divergente (FR-013). Com histórico longo, a interface
   nota sutilmente quando mensagens antigas ficaram fora do contexto enviado
   (FR-011).

## Validacao automatizada

- Backend: `dotnet test` dentro de `backend/` — `ServicoDeConversas` (CRUD em
  diretório temporário, persistência entre instâncias, título automático,
  janela de contexto, sanitização, carga de formato inválido), tradução do
  histórico nos provedores (OpenAI e Gemini/merge de papéis),
  `ConversasControlador` e `AssistenteControlador` (404/409/header).
- Frontend: `npm test` dentro de `frontend/` — `ListaDeConversas` e
  `PaginaDoAssistente` (continuar conversa com contexto, criar/renomear/excluir
  e avisos). Provedores reais nunca chamados em testes (substitutos injetados).

## Criterios de sucesso desta feature

Referência completa em [spec.md](spec.md) (SC-001 a SC-008). Resumo
operacional:

- 100% das mensagens de uma conversa recuperadas após fechar/reabrir (SC-001).
- ≥90% dos follow-ups válidos aplicam a modificação sobre a consulta anterior
  sem repetir contexto (SC-002).
- Copiável com um clique em qualquer cartão restaurado (SC-003).
- Contexto de histórico (janela) não degrada significativamente o tempo de
  resposta em conversas de até 30 mensagens (SC-004).
- Nenhuma chave/senha em histórico persistido ou contexto (SC-005).
- Exclusão remove permanentemente as mensagens (SC-006).
- Qualquer conversa encontrada e aberta pela lista (SC-007).
- 100% dos pedidos em conversas com histórico usam histórico como contexto
  (SC-008).