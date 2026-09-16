# Guia de Validacao Rapida — Assistente IA de Consultas SQL

Guia de validação ponta a ponta do assistente de IA. Referências de detalhe:
[contratos](contracts/api.md), [modelo de dados](data-model.md), [decisões](research.md)
e [spec](spec.md). Este arquivo é um guia de execução/validação, não contém
implementação.

## Pre-requisitos

- SDK .NET 8 e Node.js 18+ (como no MVP 001).
- Banco MySQL acessível com usuário de somente leitura e um database com ao
  menos 2 tabelas relacionadas (ex.: `clientes` e `contratos`, com
  `contratos.cliente_id → clientes.id`).
- Chave de API de um provedor de IA configurado no MVP (ex.: OpenAI). Use uma
  chave de teste; ela trafega somente para a API local e nunca é armazenada.

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

### 1. Listar provedores de IA

```sh
curl -s http://localhost:5000/api/assistente/provedores
```

**Esperado**: `200` com a lista dos provedores implementados, ex.:

```json
[ { "provedor": "openai", "rotulo": "OpenAI" } ]
```

> A lista contém somente provedores implementados; adicionar um novo provedor
> deve fazê-lo aparecer sem alterar o frontend.

### 2. Carregar o schema com a versão do banco

```sh
curl -s -X POST http://localhost:5000/api/esquema \
  -H "Content-Type: application/json" \
  -d '{"provedor":"mysql","host":"localhost","porta":3306,"bancoDeDados":"erp","usuario":"readonly","senha":"segredo"}'
```

**Esperado**: `200` com o schema incluindo o novo campo `versao` (ex.:
`"versao":"8.0.35"`) e as tabelas/relacionamentos de costume (ver
`contracts/api.md` do MVP 001).

### 3. Gerar uma consulta pela API

Com o schema do passo 2 no corpo (`contextoDeBanco`), envie um pedido:

```sh
curl -s -X POST http://localhost:5000/api/assistente/consultas \
  -H "Content-Type: application/json" \
  -d '{
    "provedorDeIa":"openai",
    "chaveDeApi":"<SUA_CHAVE>",
    "mensagem":"quero consultar todos os contratos com cliente com status X",
    "contextoDeBanco":{ "provedor":"mysql","versao":"8.0.35","nomeDoBanco":"erp","tabelas":[...],"relacionamentos":[...] }
  }'
```

**Esperado**: `200` com o contrato estruturado — `consulta` (SQL SELECT), além
de `explicacao`, `objetivo`, `tabelas`, `relacionamentos`, `filtros`,
`campos_retorno`, `tipo_consulta`, `resultado_esperado` e `parametros`,
conforme [api.md](contracts/api.md). A `consulta` deve refletir o schema real:
no exemplo, `INNER JOIN` entre `contratos` e `clientes` com `WHERE status = 'X'`.

### 4. Fluxo completo no chat (frontend)

1. Conectar ao MySQL e carregar o diagrama (fluxo do MVP 001).
2. Acessar a aba **Assistente de IA**.
3. Selecionar o provedor e informar a chave de API (campo de senha).
4. Digitar: `quero consultar todos os contratos com cliente com status X`.
5. **Esperado**:
   - o cabeçalho do chat indica o contexto ativo
     (ex.: `erp · mysql · 8.0.35`);
   - o estado muda para "gerando resposta" e volta ao "sucesso";
   - o cartão da resposta mostra o SQL em destaque, a explicação/objetivo, e as
     tabelas/relacionamentos/filtros/parametros em seções legíveis;
   - clicar em **copiar** cola o SQL completo na área de transferência.

### 5. Histórico e nova conversa

- Enviar 2–3 pedidos → todas as trocas permanecem visíveis na ordem.
- Reabrir uma resposta antiga → continua completa e copiável.
- **Nova conversa** → histórico é descartado e o chat volta ao estado inicial.

### 6. Tratamento de erros

- **Chave inválida/revogada**: `502` com mensagem genérica; o chat exibe aviso
  orientado à ação; a chave nunca aparece na mensagem.
- **Sem banco conectado**: o chat bloqueia o envio com aviso de contexto ausente
  (o provedor não é chamado).
- **Resposta não interpretável**: `422`; o chat permite tentar novamente sem
  quebrar a conversa.
- **Limite de requisições**: após exceder o limite por minuto, respostas `429`.
- **Cancelamento**: enquanto "gerando resposta", cancelar interrompe a chamada e
  o chat volta ao estado pronto.

### 7. Somente leitura (constituição G3)

- Confirmar que a aplicação **não executa** o SQL gerado: o produto só exibe e
  copia. Verificar com `SHOW PROCESSLIST`/log do banco que não há execução de
  queries geradas pelo assistente.
- Confirmar que a chave de API não aparece em nenhuma resposta, tela, arquivo
  ou log do backend/frontend após o uso.

## Validacao automatizada

- Backend: `dotnet test` dentro de `backend/` (ConstrutorDeContextoDeBanco,
  ServicoDoAssistente com provedor falso, fábrica, payload do OpenAi e
  controlador).
- Frontend: `npm test` dentro de `frontend/` (cartão da resposta e fluxo do
  chat). O provedor real nunca é chamado em testes (substituto injetado).

## Criterios de sucesso desta feature

Referência completa em [spec.md](spec.md) (SC-001 a SC-007). Resumo
operacional:

- Pedido em linguagem natural → consulta estruturada com contexto completo em
  até 60 s (banco de médio porte).
- 100% das chaves ficam somente em memória (nunca persistidas, logadas ou
  retornadas).
- ≥90% das respostas bem-sucedidas chegam no contrato estruturado completo.
- Copiar o SQL com um clique funciona em 100% dos cartões.
- ≥90% dos usuários revisita consulta anterior pelo histórico sem refazer o
  pedido.
- 100% dos pedidos com banco conectado enviam tabelas, colunas, relacionamentos,
  provider e versão.
- 100% das consultas geradas são somente exibidas/copiadas (nunca executadas).