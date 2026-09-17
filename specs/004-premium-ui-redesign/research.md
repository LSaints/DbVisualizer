# Research: Premium Developer-Tool UI/UX Redesign

Nenhum item do Technical Context ficou marcado como `NEEDS CLARIFICATION` — todas as decisões abaixo são escolhas técnicas necessárias para orientar o design (Phase 1), documentadas no formato Decision/Rationale/Alternatives.

## 1. Mecanismo de tokens de tema

- **Decision**: Usar CSS Custom Properties (`:root { --cor-fundo: ...; }`) definidas em `frontend/src/estilos.css`, agrupadas por categoria (`--superficie-*`, `--texto-*`, `--borda-*`, `--accent-*`, `--espaco-*`, `--raio-*`, `--duracao-*`). Um atributo `data-tema="escuro"` no elemento raiz seleciona o conjunto de valores, preparando a troca futura para `data-tema="claro"`.
- **Rationale**: O projeto já usa CSS puro sem pré-processador nem CSS-in-JS (Princípio I — Simplicidade Primeiro). Custom properties são suportadas nativamente, não exigem build step adicional, permitem trocar tema em runtime via atributo no `<html>`/`<body>`, e mantêm o CSS existente como base evolutiva em vez de reescrita total.
- **Alternatives considered**:
  - **Tailwind CSS**: rejeitado — exigiria nova dependência de build e reescrita de toda a marcação existente; viola Simplicidade Primeiro sem necessidade demonstrada.
  - **CSS-in-JS (styled-components/emotion)**: rejeitado — adiciona dependência de runtime e complexidade de bundling não justificada pelo escopo (app pequena, CSS já centralizado em um arquivo).
  - **Sass/Less com variáveis**: rejeitado — variáveis Sass são resolvidas em build-time, dificultando troca de tema em runtime sem duplicar folhas de estilo; custom properties nativas resolvem isso sem dependência extra.

## 2. Biblioteca de ícones

- **Decision**: Não adicionar biblioteca de ícones nova. Reduzir o uso de ícones ao mínimo funcional, usando glifos SVG inline simples (criados sob demanda) apenas onde comunicam uma ação sem texto (ex.: recolher sidebar, fechar drawer).
- **Rationale**: Spec pede explicitamente para evitar "excesso de ícones" e "elementos decorativos sem função" (FR-019). Uma biblioteca de ícones completa (ex.: `lucide-react`, `react-icons`) adicionaria peso e uma nova dependência sem necessidade clara, contrariando Simplicidade Primeiro.
- **Alternatives considered**:
  - **`lucide-react`**: rejeitado por ora — poderia ser reconsiderado se o número de ícones necessários crescer muito, mas hoje o escopo (poucos ícones utilitários) não justifica a dependência.
  - **Emoji/texto como ícone**: rejeitado — não combina com a estética técnica/premium pedida.

## 3. Abordagem de animações/microinterações

- **Decision**: Usar transições CSS (`transition`, `@keyframes`) para todas as microinterações (sidebar, drawer, hover, seleção, destaque de relacionamento, sucesso/erro), com durações curtas padronizadas via tokens (`--duracao-rapida: 120ms`, `--duracao-media: 200ms`) e `prefers-reduced-motion` respeitado (desabilita/reduz animações não essenciais).
- **Rationale**: CSS transitions são nativas, performáticas (compositor da GPU para `transform`/`opacity`), não exigem biblioteca de animação, e já são suficientes para o padrão de microinterações descrito na spec (sem animações complexas orquestradas). Alinhado a Simplicidade Primeiro e a boas práticas de acessibilidade.
- **Alternatives considered**:
  - **Framer Motion**: rejeitado — biblioteca poderosa, mas desnecessária para transições simples de opacidade/transform/largura; adicionaria bundle e complexidade sem ganho proporcional.
  - **React Transition Group**: rejeitado pelo mesmo motivo — utilidade real só para casos de entrada/saída de listas complexas, que não é o caso predominante aqui (drawer/sidebar são casos simples resolvíveis com CSS + estado boolean).

## 4. Realce de tabela/relacionamento no diagrama (React Flow)

- **Decision**: Usar os mecanismos nativos do React Flow para estado de nó/edge selecionado (`selected`, `className` condicional) combinados com um estado derivado em React (`tabelaFocadaId`, `arestasDestacadasIds`) que aplica classes CSS (`.tabela--selecionada`, `.aresta--destacada`, `.aresta--atenuada`) já estilizadas pelos tokens de tema. O foco (zoom/pan) usa a API `fitView`/`setCenter` do React Flow.
- **Rationale**: React Flow já expõe hooks (`useReactFlow`, props `selected`) e é a biblioteca já adotada pelo projeto; reaproveitar essa API evita reinventar lógica de seleção/viewport e mantém uma única fonte de verdade para o estado do diagrama.
- **Alternatives considered**:
  - **Gerenciar seleção fora do React Flow (estado paralelo sem usar `selected`)**: rejeitado — duplicaria lógica que a biblioteca já oferece, aumentando risco de dessincronização visual.
  - **Biblioteca de diagrama alternativa**: fora de escopo — trocar a biblioteca de diagramação não foi solicitado e violaria Simplicidade Primeiro/valor sem necessidade demonstrada.

## 5. Layout responsivo (drawer vs. painel fixo, sidebar recolhível)

- **Decision**: Usar CSS media queries com breakpoints baseados em largura de container (`min-width: 1280px` para "desktop amplo" = painel de detalhes fixo; abaixo disso até o piso suportado = painel de detalhes como drawer sobreposto) combinadas com um estado React (`sidebarRecolhida: boolean`) persistido em memória (não requer persistência entre sessões, fora de escopo da spec). Toolbar usa `flex-wrap`/ocultação progressiva de labels (ícone + tooltip) abaixo de um breakpoint intermediário.
- **Rationale**: Media queries são a abordagem padrão e mais simples para responsividade em CSS puro, sem exigir biblioteca de detecção de viewport em JS. O estado de sidebar recolhida precisa ser controlado em JS porque afeta o layout de forma binária (não é só CSS responsivo, é uma preferência do usuário).
- **Alternatives considered**:
  - **Container queries (`@container`)**: consideradas, mas o suporte é mais recente e o ganho (adaptar por container em vez de viewport) não é necessário aqui, já que o layout responde à largura da janela como um todo; mantido como possível refinamento futuro, não bloqueia esta feature.
  - **Biblioteca de layout (ex.: react-resizable-panels)**: rejeitada por ora — a spec não pede painéis redimensionáveis pelo usuário, apenas recolhíveis/drawer; adicionar a dependência não é justificado.

## 6. Acessibilidade de contraste (WCAG AA)

- **Decision**: Validar manualmente as combinações de cor definidas nos tokens (texto primário/secundário/muted sobre background/surface/surface-elevated) com uma ferramenta de contraste (ex.: cálculo de razão de contraste) durante a definição da paleta em `data-model.md`, garantindo razão mínima 4.5:1 para texto normal e 3:1 para texto grande/elementos gráficos, conforme WCAG AA.
- **Rationale**: A spec exige explicitamente conformidade com WCAG AA (SC-005) e não há budget para uma ferramenta automatizada de auditoria nesta feature; validação manual da paleta (poucos tokens) é suficiente e simples.
- **Alternatives considered**:
  - **Ferramenta automatizada de CI (ex.: axe-core em testes)**: fora de escopo desta feature — poderia ser um follow-up, mas não é necessário para entregar a paleta em conformidade agora.
