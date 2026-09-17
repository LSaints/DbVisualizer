# Quickstart: Validação da Premium UI/UX Redesign

## Pré-requisitos

- Node.js/npm instalados (mesma versão usada pelo restante do projeto `frontend/`)
- Backend rodando (para ter um schema real conectado) OU dados mockados existentes usados pelos testes
- Repositório na branch `004-premium-ui-redesign`

## Setup

```bash
cd frontend
npm install
npm run dev
```

Abra `http://localhost:5173` (ou a porta indicada pelo Vite) em uma janela de navegador com pelo menos 1280px de largura.

## Cenários de validação (mapeados às User Stories da spec)

### US1 — Hierarquia visual e superfícies (P1)

1. Conecte a um banco de dados (ou use um schema já carregado).
2. Observe a tela do diagrama: fundo, cartões de tabela e elementos elevados (menus/painéis) devem ser visualmente distintos entre si (ver tokens `--superficie-fundo`, `--superficie-base`, `--superficie-elevada` em `data-model.md`).
3. **Esperado**: nenhuma cor de destaque (`--accent`) aparece em mais de um elemento não relacionado a ação/seleção ao mesmo tempo.

### US2 — Seleção, foco e destaque de relacionamentos (P1)

1. No diagrama, clique em uma tabela.
2. **Esperado**: a tabela recebe borda de destaque (`--accent`) com transição curta (~120–200ms); os relacionamentos conectados a ela ficam destacados e os demais atenuados.
3. Acione a ação de "focar tabela" (ex.: botão/atalho dedicado).
4. **Esperado**: a viewport do diagrama anima até centralizar a tabela (zoom/pan suave).
5. Passe o mouse sobre uma tabela/coluna/relacionamento sem clicar.
6. **Esperado**: estado de hover visível e discreto antes de qualquer clique.

### US3 — Navegação responsiva (P2)

1. Com a janela em largura ampla (>1280px), acione o controle de recolher a sidebar.
2. **Esperado**: sidebar anima para o modo recolhido (ícones) e a área do diagrama se expande.
3. Redimensione a janela para uma largura de notebook menor (ex.: ~1024–1279px) e abra o painel de detalhes de uma tabela.
4. **Esperado**: painel de detalhes aparece como drawer sobreposto, com animação de entrada, e não desloca permanentemente o diagrama.
5. Continue reduzindo a largura até o piso suportado (1280px é o mínimo oficial — testar também abaixo dele para verificar degradação graciosa).
6. **Esperado**: toolbar se compacta (ícones sem label) mantendo as ações essenciais clicáveis; nenhuma sobreposição quebrada de layout.

### US4 — Feedback de sucesso/erro (P3)

1. No formulário de conexão, informe credenciais válidas e conecte.
2. **Esperado**: indicador de sucesso breve e discreto aparece e some sozinho, sem exigir ação do usuário.
3. Informe credenciais inválidas e tente conectar.
4. **Esperado**: indicador de erro claro (mas não agressivo) aparece e permanece visível até nova tentativa ou dispensa pelo usuário; a interface não fica bloqueada.

## Verificações automatizadas

```bash
cd frontend
npm run lint
npm run test
```

- A suíte Vitest/Testing Library existente (`frontend/testes/`) deve continuar passando; novos testes de interação (seleção, drawer, sidebar) devem ser adicionados durante a implementação (fase `/speckit-tasks` + `/speckit-implement`).

## Verificação de contraste (SC-005)

- Para cada par `--texto-*` sobre `--superficie-*` definido nos tokens (ver `data-model.md`), calcular a razão de contraste (ex.: via DevTools do navegador ou ferramenta de contraste) e confirmar ≥ 4.5:1 para texto normal / ≥ 3:1 para texto grande ou elementos não textuais essenciais.

## Critério de pronto (liga com Success Criteria da spec)

- [ ] SC-001: tabela selecionada identificável em <1s em diagrama com 20+ tabelas
- [ ] SC-003: todas as transições listadas em FR-016 percebidas como rápidas e não bloqueantes
- [ ] SC-004: layout utilizável sem rolagem horizontal em larguras de desktop/notebook suportadas
- [ ] SC-005: contraste WCAG AA confirmado para os tokens de texto/superfície
- [ ] SC-006: inspeção visual confirma redução de ícones/cards decorativos sem função
