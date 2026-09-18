# Modelo de Dados — Configuração e Seleção de Provedores de IA no Assistente

Área de domínio coberta: provedores de IA do assistente (configuração de
sessão, seleção por conversa) e a extensão do modelo de conversa da feature
003. O contrato estruturado da resposta do assistente (feature 002) permanece
intocado.

## Entidades

### Conversa (estendida — persistida)

Já persistida como arquivo `{id}.json` (feature 003). Ganha **um** campo novo.

| Campo                    | Tipo     | Mudança | Regra/validação                                       |
|--------------------------|----------|---------|-------------------------------------------------------|
| versao                   | int      | —       | Mantém `1`; campo novo é opcional (retrocompatível)   |
| id                       | string   | —       | Guid (nome do arquivo)                                |
| titulo                   | string   | —       | Renomeável; derivado da 1ª mensagem                   |
| tituloAutomatico         | bool     | —       | Herdado                                                |
| criadaEm / atualizadaEm  | string   | —       | ISO-8601 UTC                                          |
| contextoDeBanco          | objeto?  | —       | Identidade do banco (feature 003)                     |
| **provedorDeIaSelecionado** | string? | **NOVO** | Identificador do provedor de IA da conversa (`openai`, `google-ai-studio`, `claude`…). Setado na 1ª troca persistida que usar o provedor; nulo em conversas antigas → frontend usa o padrão da sessão. Deve corresponder a um provedor da fábrica (validado no `AssistenteControlador`). |
| mensagens                | array    | —       | Herdado                                                |

**Transições de estado**: o campo nasce nulo; é gravado/atualizado em cada
`PersistirTrocaAsync` com o `provedorDeIa` da requisição. Não há exclusão —
trocar de provedor simplesmente reescreve o valor (histórico intacto).

### ConfiguraçãoDeSessao (efêmera — frontend apenas, em memória)

Estado do **usuário/iinterface**, nunca persistido (constituição III).

| Campo            | Tipo                          | Regra                                                     |
|------------------|-------------------------------|-----------------------------------------------------------|
| chavesPorProvedor| `Record<provedor, token>`     | Token por provedor suportado pela fábrica; preenchido/salvo no modal; nunca exibido integralmente (mascarado/`configurado`) |
| provedorPadrao   | provedor                      | Inicia com o 1º provedor configurado; alterável no modal   |

**Regras**: o token não pode ser vazio; editar exige digitar o novo valor
(substitui); remover um provedor remove também seu token e o badge
correspondente; remover o padrão promove o 1º restante (ou volta a nenhum
padrão, orientando configuração).

### SelecaoDeProvedor (derivada — não persistida)

Provedor **ativo no seletor** durante uma conversa. Origem, em ordem de
prioridade: (1) `conversa.provedorDeIaSelecionado` ao abrir; (2) senão, o
`provedorPadrao` da sessão; (3) senão, nenhum (envio bloqueado + orientação ao
modal).

### BadgeDeProvedor (derivada — virtual)

Elemento visual `button` do `SeletorDeProvedor`, um por provedor com token
configurado na sessão. Estados: `ativo` (selecionado, `aria-pressed=true`) e
`inativo`. Relação: `SeletorDeProvedor` → 1..n `BadgeDeProvedor`.

## Relacionamentos

- `Conversa` 1 — 0..n `MensagemDaConversa` (herdado da 003).
- `Conversa` 1 — 1 `provedorDeIaSelecionado` (0..1 em conversas antigas).
- `ConfiguracaoDeSessao` 1 — n `chavesPorProvedor` (provedor → token).
- `SeletorDeProvedor` 1 — n `BadgeDeProvedor` (um por provedor configurado).
- `PaginaDoAssistente` orquestra `ConfiguracaoDeSessao`, `SeletorDeProvedor`,
  `ModalDeConfiguracaoDeProvedores` e `ListaDeConversas`.

## Regras de validação derivadas da spec

- **FR-012**: provedor por conversa → campo novo persistido/restaurado.
- **FR-013**: envio sem token do provedor selecionado → bloqueado com erro
  orientado (atalho para o modal).
- **FR-007/FR-014**: token nunca exibido por completo; segredo ausente de
  mensagens de erro/logs (mantém o sanitizador e o 502 genérico da 002/003).
- **FR-011**: layouts de badges escalam (flex-wrap/overflow) até 8 provedores.
- **FR-015**: tokens somente em memória da sessão (nenhuma entidade persistida
  os guarda); fechar a aplicação descarta o mapa.
- **FR-017**: remoção de provedor selecionado na conversa → fallback para o
  padrão ou aviso de configuração, sem travar o chat.