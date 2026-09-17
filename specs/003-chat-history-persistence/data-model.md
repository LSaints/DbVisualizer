# Modelo de Dados — Persistência do Histórico do Chat do Assistente

Modelos da persistência de conversas, em continuidade ao `data-model.md` da
feature 002. Nomes de identificadores em pt-BR (constituição II). O formato do
arquivo de conversa usa campos camelCase (fica fora do contrato da API, é
formato interno).

Regras transversais:

- **Credenciais nunca são persistidas**: `chaveDeApi` e `senha` não fazem parte
  do modelo de conversa (nem de mensagem nem de contexto); são campos
  transitórios do corpo da requisição (efêmeros, por memória). Padrões típicos
  de segredo colados no texto do chat são sanitizados na persistência e no
  contexto (D8 em `research.md`).
- O SQL gerado permanece dado de exibição: nunca executado (constituição III).
- O arquivo de conversa é armazenado em diretório **fora da pasta servida** e
  versionado (`versao`) para leitura segura de conversas de versões anteriores.
- O formato de armazenamento diverge do contrato da API: a API expõe DTOs
  (`ConversaResumo`, `ConversaDetalhada`); o arquivo guarda o modelo de
  domínio `Conversa`.

## Entidades

### Conversa (arquivo `{id}.json`)

Modelo persistido (uma conversa por arquivo). Nada nele é segredo.

| Campo              | Tipo                          | Validacao/Nota                                              |
|--------------------|-------------------------------|-------------------------------------------------------------|
| versao             | int                           | Versão do formato; hoje `1` (FR-016). Carga de versão não suportada → conversa parcial com aviso |
| id                 | string                        | `Guid` (file name). Obrigatório                              |
| titulo             | string                        | Título da conversa (listagem); padrão inicial "Nova conversa"; renomeável (US4) |
| tituloAutomatico   | bool                          | `true` até a primeira mensagem; título derivado da 1ª mensagem; PATCH renomear marca `false` (D6) |
| criadaEm           | string (ISO-8601)             | Data/hora de criação                                          |
| atualizadaEm       | string (ISO-8601)             | Data/hora da última troca persistida                          |
| contextoDeBanco    | `IdentidadeDeBanco`           | Identidade do banco usado na criação da conversa (FR-013, D7) |
| mensagens          | `List<MensagemDaConversa>`    | Ordem cronológica das trocas; vazio logo após a criação       |

### IdentidadeDeBanco

Identidade de contexto de banco registrada na conversa (não é o schema
completo; é o suficiente para comparar divergências — D7).

| Campo      | Tipo   | Nota                                           |
|------------|--------|------------------------------------------------|
| provedor   | string | Ex.: `mysql`                                    |
| nomeDoBanco| string | Ex.: `erp`                                      |
| versao     | string?| Ex.: `8.0.35` (opcional, como no `EsquemaDeBanco`) |

### MensagemDaConversa

Uma troca dentro da conversa (pedido ou resposta estruturada).

| Campo     | Tipo                         | Nota                                                  |
|-----------|------------------------------|-------------------------------------------------------|
| papel     | string (`"usuario"`/`"assistente"`) | Papel do remetente; traduzido por provedor (D3) |
| conteudo  | string `OU` `RespostaDeConsultaDoAssistente` | `usuario` → texto do pedido; `assistente` → resposta estruturada (contrato da 002) |
| criadaEm  | string (ISO-8601)            | Data/hora da troca                                     |

### DTOs de API (frontend/curl)

- **ConversaResumo** (item de `GET /api/conversas`): `id`, `titulo`, `criadaEm`,
  `atualizadaEm`, `resumo` (prévia curta da última troca — ou "Conversa vazia").
- **ConversaDetalhada** (resposta de `GET /api/conversas/{id}`): tudo do
  `ConversaResumo` + `contextoDeBanco` (`IdentidadeDeBanco`) + `mensagens`
  (`List<MensagemDaConversa>`).

## Relações

- `Conversa` 1 → 1 `IdentidadeDeBanco` (referência de contexto/banco).
- `Conversa` 1 → N `MensagemDaConversa` (lista ordenada).
- `MensagemDaConversa` de papel `"assistente"` referencia (embute) o contrato
  `RespostaDeConsultaDoAssistente` da feature 002 (SQL + metadados).
- O `contextoDeBanco` DTO de `POST /api/assistente/consultas` (a feature 002)
  fornece a `IdentidadeDeBanco` da conversa no momento da criação.

## Formato de persistência (exemplo)

```json
{
  "versao": 1,
  "id": "4f2c8a6e-...",
  "titulo": "Contratos com status X",
  "tituloAutomatico": false,
  "criadaEm": "2026-09-16T10:15:00Z",
  "atualizadaEm": "2026-09-16T10:17:00Z",
  "contextoDeBanco": { "provedor": "mysql", "nomeDoBanco": "erp", "versao": "8.0.35" },
  "mensagens": [
    { "papel": "usuario", "conteudo": "quero consultar todos os contratos com cliente com status X", "criadaEm": "2026-09-16T10:15:00Z" },
    { "papel": "assistente", "conteudo": { "consulta": "SELECT ...", "explicacao": "...", "objetivo": "...", "tabelas": [], "relacionamentos": [], "filtros": [], "campos_retorno": {}, "tipo_consulta": "SELECT", "resultado_esperado": "...", "parametros": [] }, "criadaEm": "2026-09-16T10:17:00Z" }
  ]
}
```

## Ciclos de estado

### Conversa

`criada (vazia)` → `ativa (≥1 troca persistida)` → `excluída`. Exclusão remove
o arquivo; as mensagens não são mais recuperáveis (SC-006). Conversas vazias
permanecem na lista até exclusão.

### Troca de mensagem (backend)

Pedido com `conversaId` → carrega conversa → valida identidade de banco (409) →
sanitiza mensagem → monta contexto (schema + janela de histórico) → chama
provedor → valida contrato → **em sucesso**: persiste o par
usuário+assistente e devolve `200` + `X-Contexto-Truncado`. Em falha (502/422):
nada é persistido (D5).

## Regras de validação

- `POST /api/conversas`: `titulo` opcional no corpo; se ausente, "Nova conversa".
  `GET /api/conversas`: ordenação por `atualizadaEm` decrescente.
- `PATCH /api/conversas/{id}/titulo`: `titulo` obrigatório, tamanho 1–100;
  vazio inválido.
- `POST /api/assistente/consultas` com `conversaId`:
  - `conversaId` inexistente → `404` "Conversa não encontrada." (pt-BR).
  - identidade de banco divergente → `409` com mensagem orientada (D7).
  - sem `conversaId` → comportamento legado de troca única (sem persistência).
- Carga de arquivo: `versao` não suportada → `GET` devolve a conversa com os
  campos que puderem ser lidos + aviso (FR-016); escrita sempre atômica
  (temp + rename, D9).