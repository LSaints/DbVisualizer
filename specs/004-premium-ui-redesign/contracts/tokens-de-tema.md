# Contrato: Tokens de Tema (CSS Custom Properties)

Esta feature não expõe nem consome nenhuma API HTTP nova — o backend permanece inalterado. O "contrato" relevante aqui é a interface visual entre o sistema de tokens de tema e os componentes React que o consomem, e o contrato de comportamento das microinterações. Documentado aqui para que qualquer componente novo ou existente possa ser implementado/validado de forma consistente.

## Contrato de nomenclatura de tokens

Todo token de tema DEVE:
- Ser definido em `frontend/src/estilos.css`, dentro de um seletor `:root[data-tema="escuro"]` (e futuramente `:root[data-tema="claro"]`).
- Seguir o nome em pt-BR conforme a tabela de `data-model.md` (`--superficie-fundo`, `--texto-primario`, `--accent`, etc.).
- Ser consumido exclusivamente via `var(--token-nome)` nos seletores CSS dos componentes — nenhum componente deve hardcodar valores de cor, espaçamento ou duração fora dos tokens.

## Contrato de atributo de tema

```html
<html data-tema="escuro">
  ...
</html>
```

- O atributo `data-tema` no elemento raiz (`<html>` ou `<body>`) seleciona o conjunto de tokens ativo.
- Nesta feature, apenas `"escuro"` é totalmente implementado; a estrutura DEVE permitir adicionar `"claro"` sem alterar nenhum componente (apenas um novo bloco de tokens no CSS).

## Contrato de classes de estado (diagrama)

Aplicadas pelos componentes do diagrama (`TabelaNoDiagrama` e as arestas do React Flow) com base no `EstadoDoDiagrama` (ver `data-model.md`):

| Classe CSS | Quando aplicada | Efeito visual esperado |
|---|---|---|
| `.tabela--selecionada` | `tabelaSelecionadaId === id` | Borda com `--accent`, transição `--duracao-rapida` |
| `.tabela--hover` (ou `:hover`) | Cursor sobre a tabela | Leve elevação/mudança de borda, transição `--duracao-rapida` |
| `.aresta--destacada` | Id está em `idsDeArestasDestacadas` | Cor/espessura reforçada, transição `--duracao-media` |
| `.aresta--atenuada` | Id está em `idsDeArestasAtenuadas` | Opacidade reduzida, transição `--duracao-media` |

## Contrato de classes de estado (layout)

| Classe/atributo | Quando aplicado | Efeito visual esperado |
|---|---|---|
| `.sidebar--recolhida` | `sidebarRecolhida === true` | Largura reduzida (ícones apenas), transição `--duracao-media` |
| `.painel-detalhes--drawer` | `modoDePainelDeDetalhes === "drawer"` | Painel sobreposto (position fixed/absolute) com transição de entrada/saída (`transform`/`opacity`) |
| `.painel-detalhes--fixo` | `modoDePainelDeDetalhes === "fixo"` | Painel ocupa espaço fixo no layout, sem sobreposição |
| `.toolbar--compacta` | Largura da viewport abaixo do breakpoint intermediário | Ícones sem label textual, com `title`/`aria-label` para acessibilidade |

## Contrato de feedback de ação

| Classe/atributo | Quando aplicado | Efeito visual esperado |
|---|---|---|
| `.feedback--sucesso` | `EstadoDeFeedback.tipo === "sucesso"` | Indicador com `--sucesso`, aparece/desaparece com transição curta, sem bloquear interação |
| `.feedback--erro` | `EstadoDeFeedback.tipo === "erro"` | Indicador com `--erro`, permanece visível até ação do usuário ou nova tentativa |

## Contrato de acessibilidade

- Todo elemento com classe de estado (`--selecionada`, `--destacada`, etc.) DEVE manter um indicador acessível equivalente que não dependa apenas de cor (ex.: `aria-selected`, espessura de borda, ícone), para usuários com daltonismo.
- Todo elemento interativo DEVE ter um estado de `:focus-visible` distinto usando `--accent` ou `--borda-sutil` reforçada, para navegação via teclado (FR-011).
- Transições/animações DEVEM respeitar `@media (prefers-reduced-motion: reduce)`, reduzindo ou removendo animações não essenciais.
