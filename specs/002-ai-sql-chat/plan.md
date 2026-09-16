# Implementation Plan: Assistente IA de Consultas SQL

**Branch**: `002-ai-sql-chat` | **Date**: 2026-09-16 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/002-ai-sql-chat/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command; its definition describes the execution workflow.

## Summary

Adicionar uma sessão de chat onde o usuário conversa com uma LLM (provedor de
IA livre: Claude, OpenAI, Gemini etc.) para gerar consultas SQL a partir de
linguagem natural. O pedido ao provedor é acompanhado do contexto completo do
ambiente (tabelas, colunas, relacionamentos, provider e versão do banco) para
que as consultas nasçam corretas para o banco real. A resposta chega em um
contrato estruturado (consulta, explicação, objetivo, tabelas, relacionamentos,
filtros, campos de retorno, tipo de consulta, resultado esperado e parâmetros)
e é renderizada no chat de forma legível, com um único clique para copiar o SQL
e histórico completo da conversa durante a sessão. Detalhes técnicos em
`research.md` (Fase 0) e contratos em `contracts/api.md` (Fase 1).

## Technical Context

**Language/Version**: Backend C# / .NET 8 (ASP.NET Core Web API); frontend
TypeScript ~5.6 + React 18 + Vite 5. Sem novas linguagens nem runtimes.

**Primary Dependencies**:
- Backend: `MySqlConnector` (existente, introspecção do schema); `System.Net.Http`
  nativo para chamar as APIs dos provedores de IA; `System.Text.Json` (existente)
  para validar o contrato estruturado. Nenhum SDK proprietário de LLM no MVP.
- Frontend: React (existente); `navigator.clipboard` nativo para copiar o SQL;
  nenhuma biblioteca nova necessária.
- Sem persistência: nenhum provedor de banco novo além do MySQL existente.

**Storage**: N/A (constituição G3 — nada é persistido). Contexto de banco e
histórico do chat vivem em memória durante a sessão (frontend); a chave de API
de IA vive somente na memória, por requisição, no backend.

**Testing**: Backend xUnit (construtor de contexto, validador do contrato
estruturado, fábrica de provedores de IA, montagem de payload do provedor e
controlador). Frontend Vitest + Testing Library (renderização do cartão da
resposta, fluxo do chat, estados de sucesso/erro). O provedor real de IA é
substituído por um falso nos testes (sem chamar rede).

**Target Platform**: Navegador web (mesmo target do MVP atual) +
API ASP.NET Core local.

**Project Type**: Aplicação web com frontend + backend (já existente na raiz:
`backend/` + `frontend/`).

**Performance Goals**: Resposta do assistente apresentada em até 60 s para
banco de médio porte (SC-001). Interface do chat responsiva em todas as trocas
de estado; cancelamento da geração disponível enquanto aguarda.

**Constraints**: Somente leitura — o SQL gerado é exibido e copiado, NUNCA
executado (SC-007). Chave de API e credenciais de banco somente em memória,
nunca persistidas, logadas ou retornadas (SC-002). Mensagens de erro genéricas
sem expor segredos. Identificadores e mensagens em pt-BR. Provedores de IA
isolados com contrato comum (G4).

**Scale/Scope**: Um usuário por vez, sem autenticação (mesmo escopo do MVP).
Histórico do chat limitado a uma conversa em memória; contexto de banco para
bancos de médio porte (centenas de tabelas simplificadas conforme `EsquemaDeBanco.Aviso`).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Gates extraídos da `constitution.md`:

- **G1 — Simplicidade Primeiro (I)**: apenas as abstrações mínimas
  necessárias. Novas abstrações: `InterfaceProvedorDeIa` + `FabricaDeProvedoresDeIa`
  (exigência do G4, espelhando o padrão existente de schema). Nada de repositório,
  CQRS, autenticação, filas ou camadas sem necessidade demonstrada.
- **G2 — Código em pt-BR (II)**: todos os identificadores, endpoints, DTOs,
  pastas, testes e mensagens em pt-BR. Exceções técnicas autorizadas: nomes de
  providers/modelos de IA (`openai`, `claude`, `gemini`, `gpt-4o`), palavras
  reservadas da linguagem e valores técnicos de banco.
- **G3 — Somente Leitura (III)**: o assistente NUNCA executa o SQL gerado;
  as consultas são apenas exibidas e copiadas. As credenciais (senha do banco e
  chave de API de IA) permanecem SOMENTE em memória: nunca em banco da aplicação,
  localStorage, logs, configuração ou resposta de API.
- **G4 — Provedores Isolados, Contrato Comum (IV)**: camada de IA isolada por
  provedor via interface + fábrica; adicionar um provedor não altera controllers,
  serviços comuns ou frontend. MVP com ao menos um provedor funcional.
- **G5 — Visual First (V)**: a funcionalidade complementa a exploração visual;
  o chat é acionado a partir do estado conectado e não degrada o diagrama
  (nem carregamento, nem navegação).
- **G6 — Segurança e Performance**: erros de provedor/contexto genéricos e
  orientados à ação; timeout e limite de requisições por cliente; limites de
  tamanho do contexto enviado.

Resultado: **GATE PASSOU** (sem violações). Reavaliação pós-design no fim deste
plano e no `research.md`.

## Project Structure

### Documentation (this feature)

```text
specs/002-ai-sql-chat/
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
│   │   ├── AssistenteControlador.cs        # NOVO: api/assistente
│   ├── Dtos/
│   │   ├── RequisicaoDeConsultaDoAssistente.cs   # NOVO: pedido do chat
│   │   ├── RespostaDeConsultaDoAssistente.cs     # NOVO: wrapper do 200
│   │   └── ProvedorDeIaDisponivel.cs              # NOVO: item de GET provedores
│   ├── Infraestrutura/
│   │   ├── ConfiguradorDeConexao.cs        # existente
│   │   └── TipoDeProvedor.cs               # existente
│   ├── Modelos/
│   │   ├── EsquemaDeBanco.cs               # Ajuste: + campo Versao
│   │   ├── RespostaDeConsultaDoAssistente.cs # NOVO: contrato estruturado + aninhados
│   │   └── ... (demais modelos existentes)
│   ├── Provedores/
│   │   ├── InterfaceProvedorDeIa.cs        # NOVO: contrato mínimo do provedor de IA
│   │   ├── FabricaDeProvedoresDeIa.cs      # NOVO: resolve pelo Tipo
│   │   ├── ProvedorDeIaNaoSuportadoException.cs # NOVO
│   │   ├── Ia/
│   │   │   └── OpenAi/                     # NOVO: 1º provedor funcional (padrão p/ demais)
│   │   │       └── OpenAiProvedorDeIa.cs
│   │   └── (schema providers existentes: InterfaceProvedorDeSchema, Fabrica..., MySql/)
│   ├── Servicos/
│   │   ├── ServicoDoAssistente.cs          # NOVO: orquestra prompt → provedor → validação
│   │   ├── ConstrutorDeContextoDeBanco.cs  # NOVO: monta o prompt/system com o schema
│   │   └── (serviços existentes)
│   └── Program.cs                          # Ajuste: DI + rate limiter do assistente
└── DatabaseDiagram.Testes/
    ├── Servicos/ConstrutorDeContextoDeBancoTestes.cs  # NOVO
    ├── Servicos/ServicoDoAssistenteTestes.cs          # NOVO
    ├── Provedores/FabricaDeProvedoresDeIaTestes.cs    # NOVO
    ├── Provedores/OpenAiProvedorDeIaTestes.cs         # NOVO
    └── (testes existentes)

frontend/
└── src/
    ├── App.tsx                              # Ajuste: acesso ao assistente no estado conectado
    ├── componentes/
    │   ├── CartaoDeRespostaDeConsulta.tsx   # NOVO: renderiza o contrato estruturado
    │   ├── ConfiguracaoDoAssistente.tsx     # NOVO: provedor + chave de API (em memória)
    │   └── (componentes existentes)
    ├── modelos/
    │   ├── tipos.ts                         # Ajuste: + versao em EsquemaDeBanco
    │   └── tiposDoAssistente.ts             # NOVO: contrato estruturado + request/response
    ├── paginas/
    │   ├── PaginaDoAssistente.tsx           # NOVO: chat (histórico, entrada, estados)
    │   └── (páginas existentes)
    ├── servicos/
    │   ├── ApiAssistente.ts                 # NOVO: cliente dos endpoints do assistente
    │   └── (serviços existentes)
    └── testes/                              # test files co-localizados/Vitest
        ├── CartaoDeRespostaDeConsultaTestes.tsx   # NOVO
        └── PaginaDoAssistenteTestes.tsx           # NOVO
```

**Structure Decision**: reaproveitar a estrutura existente do MVP (backend
`Controladores/Dtos/Modelos/Servicos/Provedores/Infraestrutura` e frontend
`componentes/paginas/servicos/modelos`), adicionando as peças do assistente de
IA nos mesmo diretórios. Provedores de IA ficam em `Provedores/Ia/<provedor>/`
para separar claramente da introspecção de schema (`Provedores/MySql/`). Nenhum
novo projeto/camada é criado.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

Sem violações — tabela não preenchida.

## Reavaliação pós-design

Após a Fase 0 (`research.md`) e a Fase 1 (data-model, contratos, quickstart),
o Constitution Check foi reavaliado e **permanece aprovado, sem violações**
(registro completo em `research.md` → Registro de conformidade):

- **G1**: superfície mínima (`InterfaceProvedorDeIa` + fábrica +
  `ServicoDoAssistente` + `ConstrutorDeContextoDeBanco`).
- **G2**: pt-BR em código, endpoints, DTOs, prompt e mensagens; única exceção
  do contrato é a fidelidade às chaves do exemplo do usuário (D1).
- **G3**: `versao` é metadado de leitura; SQL gerado nunca é executado; chave de
  API e senha somente em memória.
- **G4**: provedores de IA isolados; provedores de schema inalterados.
- **G5**: chat complementa o diagrama, sem degradar a navegação visual.
- **G6**: rate limit, limite de corpo, timeout 90 s, erros genéricos.