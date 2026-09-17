# Implementation Plan: Persistência do Histórico do Chat do Assistente

**Branch**: `003-chat-history-persistence` | **Date**: 2026-09-16 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/003-chat-history-persistence/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command; its definition describes the execution workflow.

## Summary

Evoluir o assistente de IA (feature 002, já implementada) para **persistir as
conversas** e **usar o histórico como contexto** em cada novo pedido, de modo
que o usuário continue uma conversa — inclusive modificando a consulta sugerida
na interação anterior — sem começar do zero. O backend passa a armazenar as
conversas em **arquivos JSON locais** (uma conversa por arquivo, diretório não
servido), a incluir uma **janela das mensagens mais recentes** como contexto no
pedido ao provedor e a expor CRUD de conversas (listar, criar, abrir, renomear,
excluir). O frontend ganha a lista/navegação de conversas persistidas e a troca
contínua entre elas. Detalhes técnicos em `research.md` (Fase 0) e contratos em
`contracts/api.md` (Fase 1).

## Technical Context

**Language/Version**: Backend C# / .NET 8 (ASP.NET Core Web API); frontend
TypeScript ~5.6 + React 18 + Vite 5. Sem novas linguagens nem runtimes.

**Primary Dependencies**:
- Backend: `System.Text.Json` (existente) para persistir/lê os arquivos de
  conversa e validar o contrato estruturado; `System.IO` para o diretório de
  conversas; `MySqlConnector` e HttpClient (existentes) inalterados. Nenhuma
  biblioteca nova.
- Frontend: React (existente); nenhuma biblioteca nova; cliente `fetch` já
  existente em `ApiAssistente.ts` ganha as funções de CRUD de conversas.
- Nova implementação de provedor? Não. Os provedores existentes (OpenAI,
  Google AI Studio) apenas passam a traduzir o histórico de conversa para o
  formato da própria API (particularidade isolada no provedor — G4).

**Storage**: Arquivos JSON locais no servidor: um arquivo por conversa, na
forma `{id}.json`, em diretório configurável (`Armazenamento:DiretorioDeConversas`,
padrão `<conteúdo>/dados/conversas/`) que **não** é servido pela aplicação.
Persistência de histórico, que não são credenciais; chave de API e senha
continuam efêmeras, somente em memória por requisição (constituição III).

**Testing**: Backend xUnit (persistência/CRUD do `ServicoDeConversas` em
diretório temporário injetado, janela de contexto e títulos, tradução do
histórico nos provedores, controlador de conversas, e `ServicoDoAssistente`
mantendo o contrato). Frontend Vitest + Testing Library (lista/navegação de
conversas, continuar conversa, criar/excluir/renomear, aviso de contexto
divergente e de histórico truncado). Provedores reais nunca chamados em testes.

**Target Platform**: Navegador web (mesmo target do MVP) + API ASP.NET Core
local.

**Project Type**: Aplicação web com frontend + backend (já existente na raiz:
`backend/` + `frontend/`).

**Performance Goals**: Resposta em até 60 s mesmo com contexto de histórico em
conversas de até 30 mensagens (SC-004); a janela de contexto evita crescimento
ilimitado da latência/custo (FR-010); leitura/escrita de conversas instantânea
para a escala de uso local (uma conversa inteira lida na abertura).

**Constraints**: Somente leitura — o SQL gerado nunca é executado (G3).
Nenhum segredo (chave de API, senha de banco) em histórico persistido ou
contexto (FR-012/SC-005). Mensagens de erro genéricas, sem conteúdo de
conversa; logs sem corpo de mensagens. Identificadores, campos, rotas e
mensagens em pt-BR (G2), com exceções técnicas de protocolo (headers HTTP,
roles de provedor). Provedores de IA isolados com contrato comum (G4).
Persistência sem dependência nova de banco (G1).

**Scale/Scope**: Um usuário por vez, sem autenticação (mesmo escopo do MVP).
Conversas ilimitadas em número; arquivo por conversa; janela de contexto padrão
de 20 mensagens (configurável); retenção indefinida até exclusão pelo usuário
(SC-006).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Gates extraídos da `constitution.md`:

- **G1 — Simplicidade Primeiro (I)**: superfície mínima. Novas peças:
  `ServicoDeConversas` (persistência em arquivo + CRUD + janela de contexto) e
  `ConversasControlador`; extensão de `PedidoDeResposta`/`ServicoDoAssistente`
  e tradução do histórico nos provedores existentes. Nada de repositório, CQRS,
  banco de aplicação, autenticação ou abstrações sem necessidade demonstrada.
- **G2 — Código em pt-BR (II)**: identificadores, campos JSON, endpoints
  (`api/conversas`), mensagens e testes em pt-BR. Exceções técnicas: papéis de
  provedor (`user`/`assistant`/`model`) e palavras reservadas/protocolo.
- **G3 — Somente Leitura (III)**: o SQL gerado continua apenas exibido/copiado.
  As credenciais (senha do banco e chave de API de IA) permanecem SOMENTE em
  memória: o **histórico persistido e o contexto** nunca contêm chave, senha ou
  segredo (FR-012/SC-005); a chave `chaveDeApi`/`senha` não faz parte de nenhuma
  mensagem persistida.
- **G4 — Provedores Isolados, Contrato Comum (IV)**: a tradução do histórico
  para o formato de cada API vive dentro de cada provider (particularidade
  isolada), mantendo o contrato comum `PedidoDeResposta` e o fluxo do chat
  inalterados ao adicionar um provedor.
- **G5 — Visual First (V)**: a lista/navegação de conversas complementa a
  exploração visual do diagrama, sem degradar carregamento nem navegação.
- **G6 — Segurança e Performance**: janela de contexto limita o custo/latência;
  rate limit existente mantido; escrita atômica do arquivo; erros genéricos sem
  expor conteúdo; diretório de conversas fora da pasta servida.

Resultado: **GATE PASSOU** (sem violações). Reavaliação pós-design no fim deste
plano e no `research.md`.

## Project Structure

### Documentation (this feature)

```text
specs/003-chat-history-persistence/
├── plan.md              # Este arquivo (/speckit.plan command output)
├── research.md          # Fase 0 (/speckit.plan command)
├── data-model.md        # Fase 1 (/speckit.plan command)
├── quickstart.md        # Fase 1 (/speckit.plan command)
├── contracts/           # Fase 1 (/speckit.plan command)
│   └── api.md
└── tasks.md             # Fase 2 (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
backend/
├── DatabaseDiagram.Api/
│   ├── Controladores/
│   │   ├── AssistenteControlador.cs          # Ajuste: aceita conversaId + header X-Contexto-Truncado
│   │   ├── ConversasControlador.cs           # NOVO: api/conversas (CRUD)
│   │   └── (demais controladores existentes)
│   ├── Dtos/
│   │   ├── RequisicaoDeConsultaDoAssistente.cs # Ajuste: + ConversaId (Guid?)
│   │   ├── ConversaResumo.cs                 # NOVO: item da lista
│   │   ├── ConversaDetalhada.cs              # NOVO: GET /api/conversas/{id}
│   │   ├── CriarConversa.cs                  # NOVO: corpo do POST /api/conversas
│   │   ├── RenomearConversa.cs               # NOVO: corpo do PATCH título
│   │   └── (demais DTOs existentes)
│   ├── Modelos/
│   │   ├── Conversa.cs                       # NOVO: modelo persistido (arquivo JSON)
│   │   ├── MensagemDaConversa.cs             # NOVO: troca usuário/assistente
│   │   └── (modelos existentes, incl. RespostaDeConsultaDoAssistente)
│   ├── Provedores/
│   │   ├── PedidoDeResposta.cs               # Ajuste: + MensagensDaConversa
│   │   ├── Ia/
│   │   │   ├── OpenAi/OpenAiProvedorDeIa.cs        # Ajuste: traduz histórico (roles user/assistant)
│   │   │   └── GoogleAiStudio/GoogleAiStudioProvedorDeIa.cs # Ajuste: traduz histórico (roles user/model, merge)
│   │   └── (demais provedores existentes)
│   ├── Servicos/
│   │   ├── ServicoDeConversas.cs             # NOVO: persistência/CRUD/janela/título/sanitização
│   │   ├── ServicoDoAssistente.cs            # Ajuste: recebe mensagens da janela de contexto
│   │   └── (demais serviços existentes)
│   ├── appsettings.json / appsettings.Development.json  # Ajuste: novas seções
│   └── Program.cs                            # Ajuste: DI do ServicoDeConversas + config
└── DatabaseDiagram.Testes/
    ├── Controladores/
    │   ├── ConversasControladorTestes.cs     # NOVO
    │   └── AssistenteControladorTestes.cs    # Ajuste: conversaId, 404, header truncado
    ├── Provedores/
    │   ├── OpenAiProvedorDeIaTestes.cs       # Ajuste: payload com histórico
    │   └── GoogleAiStudioProvedorDeIaTestes.cs # Ajuste: payload com histórico + merge
    └── Servicos/
        ├── ServicoDeConversasTestes.cs       # NOVO: persistir/listar/abrir/renomear/excluir/janela/título
        └── ServicoDoAssistenteTestes.cs      # Ajuste: histórico como contexto no pedido

frontend/
└── src/
    ├── componentes/
    │   ├── ListaDeConversas.tsx              # NOVO: navegação, criar/abrir/renomear/excluir
    │   └── (componentes existentes)
    ├── modelos/
    │   └── tiposDoAssistente.ts              # Ajuste: Conversa, Mensagem, + conversaId no request
    ├── paginas/
    │   └── PaginaDoAssistente.tsx            # Ajuste: conversa ativa, lista, continuar, avisos
    ├── servicos/
    │   └── ApiAssistente.ts                  # Ajuste: CRUD de conversas + header X-Contexto-Truncado
    └── testes/
        ├── ListaDeConversas.test.tsx         # NOVO
        └── PaginaDoAssistente.test.tsx       # Ajuste: continuar conversa com contexto
```

**Structure Decision**: reaproveitar a estrutura existente do MVP e do
assistente (backend `Controladores/Dtos/Modelos/Servicos/Provedores` e frontend
`componentes/paginas/servicos/modelos`), adicionando as peças nos mesmos
diretórios. A persistência fica em um único `ServicoDeConversas` (sem camada de
repositório — file IO feito diretamente na classe, com diretório injetado para
testes). O diretório de arquivos de conversa fica fora de `wwwroot`/pasta
servida. Nenhum novo projeto/camada é criado.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

Sem violações — tabela não preenchida.

## Reavaliação pós-design

Após a Fase 0 (`research.md`) e a Fase 1 (data-model, contratos, quickstart),
o Constitution Check foi reavaliado e **permanece aprovado, sem violações**
(registro completo em `research.md` → Registro de conformidade):

- **G1**: superfície mínima — `ServicoDeConversas` + `ConversasControlador` +
  extensões em `PedidoDeResposta`/`ServicoDoAssistente` e tradução do histórico
  nos dois provedores existentes. Sem repositório, CQRS, banco, fila ou
  autenticação.
- **G2**: pt-BR em códigos, campos JSON (`conversaId`, `titulo`, `criadaEm`,
  `atualizadaEm`, `resumo`, `mensagens`, `papel`, `conteudo`), rotas e
  mensagens; exceções técnicas de protocolo (headers HTTP, roles de provedor).
- **G3**: SQL nunca executado; histórico persistido/contexto nunca contêm
  chave, senha ou segredo (sanitização D8); credenciais continuam efêmeras.
- **G4**: tradução do histórico isolada em cada provider; contrato comum
  `PedidoDeResposta` estendido sem quebrar o fluxo; provedores novos seguem sem
  tocar controllers/frontend.
- **G5**: lista/navegação de conversas complementa o diagrama, sem degradar a
  exploração visual.
- **G6**: janela de contexto limitada; rate limit mantido; escrita atômica;
  diretório de conversas fora da pasta servida; erros genéricos; logs sem
  conteúdo de conversa.