# Modelo de Dados — Assistente IA de Consultas SQL

Modelos do assistente de IA, em continuidade ao `data-model.md` do MVP 001.
Nomes de identificadores em pt-BR (constituição II). O contrato JSON da
resposta estruturada usa as chaves exatas do exemplo fornecido pelo usuário
(decisão D1 em `research.md`): chaves compostas em snake_case são mapeadas no
C# via `[JsonPropertyName]` (ex.: `CamposRetorno` → `campos_retorno`).

Regras transversais:

- **Nada é persistido.** Chave de API de IA, histórico do chat e contexto de
  banco vivem apenas em memória, durante a sessão/requisição.
- A chave de API e a senha de banco NUNCA aparecem em resposta de API, em logs
  ou em qualquer teste de saída.
- O SQL gerado pelo assistente é **dado de exibição**: nunca é executado,
  ajustado ou enviado de volta ao banco (constituição III).
- Os modelos reaproveitam o contrato comum `EsquemaDeBanco` existente, com um
  campo aditivo (`versao`), sem quebrar consumidores atuais.

## Entidades

### RequisicaoDeConsultaDoAssistente

Corpo de `POST /api/assistente/consultas` (DTO de entrada). Transitória: a
`chaveDeApi` é usada somente na requisição que a originou.

| Campo          | Tipo               | Validacao                                            |
|----------------|--------------------|------------------------------------------------------|
| provedorDeIa   | string             | Obrigatório; deve ser suportado pela fábrica         |
| chaveDeApi     | string             | Obrigatória; nunca retornada nem logada              |
| mensagem       | string             | Obrigatória; mínimo de caracteres para um pedido     |
| contextoDeBanco| `EsquemaDeBanco`   | Obrigatório; deve ter `tabelas` preenchidas          |

### EsquemaDeBanco (ajuste: campo `versao`)

Modelo comum já existente no MVP 001. **Alteração aditiva**: novo campo
opcional `versao`, preenchido pelo provider (MySQL: `SELECT VERSION()`; outros
bancos na sua própria consulta). Corresponde ao requisito FR-005 ("provider do
banco junto com a versão").

| Campo (novo) | Tipo    | Descricao                                      |
|--------------|---------|------------------------------------------------|
| versao       | string? | Versão do banco reportada pelo provider, ex.: `8.0.35` |

### ProvedorDeIaDisponivel

Item de `GET /api/assistente/provedores`.

| Campo     | Tipo   | Descricao                                             |
|-----------|--------|-------------------------------------------------------|
| provedor  | string | Identificador do provider, ex.: `openai`, `claude`, `gemini` |
| rotulo    | string | Rótulo exibido ao usuário em pt-BR, ex.: `OpenAI`     |

### RespostaDeConsultaDoAssistente

Contrato estruturado da resposta (JSON com as chaves do exemplo do usuário).
Representa a **consulta gerada** com seus metadados explicativos.

| Campo               | Tipo deChave JSON | Tipo C#            | Descricao                                    |
|---------------------|-------------------|--------------------|----------------------------------------------|
| consulta            | `consulta`        | string             | SQL gerado (somente leitura/SELECT)          |
| explicacao          | `explicacao`      | string             | Explicação da query em pt-BR                 |
| objetivo            | `objetivo`        | string             | Objetivo da consulta em pt-BR                |
| tabelas             | `tabelas`         | `List<TabelaDaResposta>` | Tabelas usadas na consulta             |
| relacionamentos     | `relacionamentos` | `List<RelacionamentoDaResposta>` | Junções entre tabelas  |
| filtros             | `filtros`         | `List<FiltroDaResposta>` | Condições aplicadas                 |
| camposRetorno       | `campos_retorno`  | `CamposDeRetorno`  | Campos retornados por tabela + explicação    |
| tipoConsulta        | `tipo_consulta`   | string             | Tipo de consulta, ex.: `SELECT`              |
| resultadoEsperado   | `resultado_esperado` | string         | Descrição do resultado esperado              |
| parametros          | `parametros`      | `List<ParametroDaResposta>` | Parâmetros usados na consulta    |

**Validação mínima no backend (D6)**: JSON válido; `consulta` (string não
vazia) e `explicacao` obrigatórios; demais campos presentes com o tipo correto
(listas podem ser vazias; `objetivo`/`tipo_consulta`/`resultado_esperado`
strings, mesmo vazias; `campos_retorno` objeto).

#### TabelaDaResposta

| Campo    | JSON   | Tipo   | Descricao                         |
|----------|--------|--------|-----------------------------------|
| nome     | `nome` | string | Nome da tabela                    |
| apelido  | `apelido` | string | Apelido/alias usado na query  |
| funcao   | `funcao` | string | Papel da tabela na consulta    |

#### RelacionamentoDaResposta

Chaves compostas em snake_case (decisão D1); mapeadas via `[JsonPropertyName]`.

| Campo         | JSON            | Tipo   | Descricao                                   |
|---------------|-----------------|--------|---------------------------------------------|
| tabelaOrigem  | `tabela_origem` | string | Tabela de origem da junção                  |
| campoOrigem   | `campo_origem`  | string | Coluna da origem                            |
| tabelaDestino | `tabela_destino`| string | Tabela de destino                           |
| campoDestino  | `campo_destino` | string | Coluna do destino                           |
| tipo          | `tipo`          | string | Tipo de junção, ex.: `INNER JOIN`           |
| explicacao    | `explicacao`    | string | Explicação da relação em pt-BR              |

#### FiltroDaResposta

| Campo      | JSON         | Tipo   | Descricao                              |
|------------|--------------|--------|----------------------------------------|
| campo      | `campo`      | string | Campo filtrado (tabela.coluna)         |
| operador   | `operador`   | string | Operador, ex.: `=`, `LIKE`, `>`         |
| valor      | `valor`      | string | Valor do filtro                        |
| explicacao | `explicacao` | string | Explicação do filtro em pt-BR          |

#### CamposDeRetorno

Mapa dinâmico `tabela → campos` (ex.: `"contratos": "*"`, `"clientes": "*"`)
com uma chave reservada `explicacao`. Representado em C# como
`Dictionary<string, string>` (a própria propriedade mapeada para
`campos_retorno` preserva as chaves como vieram, garantindo fidelidade).

| Chave reservada | Descricao                          |
|-----------------|------------------------------------|
| `explicacao`    | Explicação dos campos em pt-BR     |

#### ParametroDaResposta

| Campo     | JSON         | Tipo   | Descricao                              |
|-----------|--------------|--------|----------------------------------------|
| nome      | `nome`       | string | Nome do parâmetro                      |
| valor     | `valor`      | string | Valor usado, ex.: `X`                  |
| descricao | `descricao`  | string | O que o parâmetro representa           |

## Relações

- `RequisicaoDeConsultaDoAssistente` 1 → 1 `EsquemaDeBanco` (via
  `contextoDeBanco`): o contexto reutiliza o modelo comum do MVP 001, sem
  duplicação.
- `RespostaDeConsultaDoAssistente` 1 → N `TabelaDaResposta` (via `tabelas`),
  1 → N `RelacionamentoDaResposta`, 1 → N `FiltroDaResposta`,
  1 → N `ParametroDaResposta`, e 1 → 1 `CamposDeRetorno` (mapa).
- `CamposDeRetorno` chaveia por nome de tabela (dinâmico), mais a chave
  reservada `explicacao`.

## Ciclos de estado

### Chat (frontend, sessão)

`configurando` → `pronto` → `gerando` → `sucesso` (cartão renderizado)
ou `erro` (mensagem orientada) — e de volta a `pronto`. **Nova conversa**
descarta o histórico e retorna a `pronto`. Sem contexto de banco, o envio fica
bloqueado com aviso.

### Requisição do assistente (backend, sem estado)

`recebida` → `validada` → `chamada ao provedor` → `resposta validada` →
`200 com contrato`; ou falha → `400` (validação de entrada), `422` (resposta não
interpretável), `429` (limite), `502` (erro do provedor).

## Regras de validacao do contrato de entrada

- `RequisicaoDeConsultaDoAssistente`: `provedorDeIa` suportado; `chaveDeApi`
  não vazia (nunca validada em estrutura — o provedor valida); `mensagem` com
  tamanho mínimo configurável (ex.: 3 caracteres) e máximo; `contextoDeBanco`
  com `tabelas` não vazias e tamanho máximo (ex.: 200 tabelas e 1 MB de corpo),
  protegendo o provedor contra abuso (G6). Falhas retornam mensagem em pt-BR.
- Nenhuma validação adicional no modelo de saída além do contrato estruturado
  (D6).