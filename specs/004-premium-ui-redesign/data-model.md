# Data Model: Premium Developer-Tool UI/UX Redesign

Esta feature é primariamente de apresentação (UI/UX). Não há entidades de domínio/persistência novas. O "modelo de dados" aqui descreve as estruturas de estado de UI e os tokens de design que os componentes React consomem.

## 1. Tokens de Tema (`TemaTokens`)

Conjunto de CSS Custom Properties definidas em `:root[data-tema="escuro"]` (e futuramente `:root[data-tema="claro"]`), consumidas por todos os componentes via `var(--token)`.

| Grupo | Token | Descrição | Regra de validação |
|---|---|---|---|
| Superfície | `--superficie-fundo` | Cor de fundo da aplicação (nunca preto absoluto `#000000`) | Luminância > 0, distinta de `--superficie-elevada` |
| Superfície | `--superficie-base` | Cor de painéis/áreas de conteúdo padrão | Contraste suficiente com `--texto-primario` (≥ 4.5:1) |
| Superfície | `--superficie-elevada` | Cor de elementos elevados (menus, drawer, popovers, tooltips) | Deve ser perceptivelmente mais clara que `--superficie-base` |
| Borda | `--borda-sutil` | Cor de borda padrão entre superfícies | Contraste baixo/sutil, apenas perceptível o suficiente para delimitar |
| Texto | `--texto-primario` | Texto de maior ênfase (títulos, nomes de tabela/coluna) | Contraste ≥ 4.5:1 sobre `--superficie-base` e `--superficie-fundo` |
| Texto | `--texto-secundario` | Texto de apoio (descrições, metadados) | Contraste ≥ 4.5:1 sobre as mesmas superfícies |
| Texto | `--texto-muted` | Texto de baixa ênfase (hints, timestamps) | Contraste ≥ 3:1 (uso apenas para texto não essencial) |
| Accent | `--accent` | Cor de destaque (ação primária, seleção, foco) | Uso restrito: no máx. 1 accent ativo por contexto de tela (FR-003) |
| Accent | `--accent-texto` | Cor de texto sobre superfícies com `--accent` como fundo | Contraste ≥ 4.5:1 sobre `--accent` |
| Estado | `--sucesso` | Cor de indicação de sucesso (conexão bem-sucedida) | Distinta de `--erro` e do `--accent` |
| Estado | `--erro` | Cor de indicação de erro | Perceptível sem ser agressiva/saturada em excesso |
| Espaçamento | `--espaco-1` … `--espaco-8` | Escala de espaçamento consistente (ex.: 4/8/12/16/24/32/48/64px) | Usado em todo padding/margin/gap |
| Tipografia | `--fonte-tamanho-*` (xs, sm, base, lg, xl, 2xl) | Escala tipográfica | Hierarquia clara entre níveis |
| Raio | `--raio-sm`, `--raio-md` | Raio de borda para bordas sutis (sem cantos exagerados) | Consistente entre componentes similares |
| Movimento | `--duracao-rapida`, `--duracao-media` | Duração de transições (ex.: 120ms, 200ms) | Usado por todas as microinterações (FR-016) |
| Movimento | `--easing-padrao` | Curva de easing padrão | Consistente entre transições |

**Relacionamentos**: `TemaTokens` é referenciado por todos os componentes visuais; não depende de nenhuma outra entidade.

**Extensibilidade futura (fora do escopo desta feature)**: um segundo bloco `:root[data-tema="claro"] { ... }` redefine os mesmos nomes de token com valores claros, sem exigir mudança nos componentes (FR-005).

## 2. Estado de Interação do Diagrama (`EstadoDoDiagrama`)

Estado em memória (React state/hooks) na página `PaginaDoDiagrama`, não persistido.

| Campo | Tipo | Descrição |
|---|---|---|
| `tabelaSelecionadaId` | `string \| null` | Id da tabela atualmente selecionada no diagrama |
| `tabelaFocadaId` | `string \| null` | Id da tabela que recebeu a ação explícita de foco (zoom/pan) |
| `idsDeArestasDestacadas` | `string[]` | Ids das arestas (relacionamentos) conectadas à tabela selecionada/focada, usadas para aplicar destaque |
| `idsDeArestasAtenuadas` | `string[]` | Ids das arestas não conectadas à seleção atual, usadas para aplicar atenuação visual |

**Regras/transições**:
- Selecionar uma tabela → define `tabelaSelecionadaId`, recalcula `idsDeArestasDestacadas`/`idsDeArestasAtenuadas` a partir do grafo de relacionamentos já carregado.
- Focar uma tabela → mantém `tabelaSelecionadaId` e adicionalmente aciona a transição de viewport (fitView/setCenter do React Flow) para `tabelaFocadaId`.
- Desselecionar (clique em área vazia) → limpa `tabelaSelecionadaId`, `tabelaFocadaId` e os arrays de destaque/atenuação.

## 3. Estado de Navegação/Layout (`EstadoDeLayout`)

Estado em memória (React state) compartilhado entre as páginas, controla a apresentação estrutural.

| Campo | Tipo | Descrição |
|---|---|---|
| `sidebarRecolhida` | `boolean` | Se a barra lateral está no modo recolhido (ícones apenas) ou expandido |
| `painelDeDetalhesAberto` | `boolean` | Se o painel de detalhes (fixo ou drawer, dependendo do breakpoint) está visível |
| `modoDePainelDeDetalhes` | `"fixo" \| "drawer"` | Derivado da largura da viewport (media query/`ResizeObserver`), não definido diretamente pelo usuário |

**Regras/transições**:
- Abaixo do breakpoint de desktop amplo (< 1280px), `modoDePainelDeDetalhes` torna-se `"drawer"` automaticamente; acima, `"fixo"`.
- Alternar `sidebarRecolhida` não fecha `painelDeDetalhesAberto` (independentes), mas o layout deve reajustar a área do diagrama sem sobreposição (Edge Case da spec).

## 4. Estado de Feedback de Ação (`EstadoDeFeedback`)

Representa o resultado visual (sucesso/erro) de uma ação assíncrona (ex.: `FormularioDeConexao`, resposta do assistente).

| Campo | Tipo | Descrição |
|---|---|---|
| `tipo` | `"sucesso" \| "erro" \| null` | Tipo de feedback ativo no momento |
| `mensagem` | `string \| null` | Mensagem curta associada (ex.: causa do erro) |
| `visivel` | `boolean` | Controla a montagem/transição de entrada-saída do indicador |

**Regras/transições**: ao concluir uma ação, `tipo`/`mensagem`/`visivel` são definidos; após um tempo (ex.: alguns segundos, para sucesso) ou interação do usuário (para erro persistente), `visivel` volta a `false` disparando a transição de saída antes da desmontagem.

## Observação sobre entidades de domínio existentes

`TabelaNoDiagrama`, `Conversa`, `MensagemDaConversa`, etc. (modelos já existentes em `frontend/src/modelos` e no backend) não são alterados por esta feature — apenas sua apresentação visual e os estados de UI acima descritos, que são camadas adicionais sobre os dados já existentes.
