# Feature Specification: Configuração e Seleção de Provedores de IA no Assistente

**Feature Branch**: `005-ai-provider-config`

**Created**: 2026-09-17

**Status**: Draft

**Input**: User description: "quero que na página do assistente tenha uma barra lateral onde possa ser consultadas as conversas anteriores e tenha uma area de configurações onde o usuario pode consultar configurar os tokens das apis e no chat apenas selecionar qual provedor ele deseja, nas configurações deve abrir um modal para que seja realizadas as configurações dos provedores de IA, quero que seja possivel selecionar o provider apartir de um componente criado especificamente para o text area, deve conter pequenos badges (botões) caso cresçar as opções de, mas que tenha a possibilidade de alterar o provider por alí mesmo, esse componente deve ficar na parte inferior do chat com o botão enviar. "

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Configurar os provedores de IA em um modal de configurações (Priority: P1)

Na página do assistente, o usuário acessa uma área de configurações que abre um
modal dedicado aos provedores de IA. Dentro do modal, ele consulta os provedores
já configurados, adiciona um novo provedor informando o provedor e o token da
API, edita o token de um provedor existente, remove um provedor e indica qual
provedor deve ser o padrão. Ao salvar, as configurações passam a valer para o
chat.

**Why this priority**: Sem uma forma de informar os tokens, nenhuma conversa pode
acontecer e o seletor do chat (US2) não tem o que mostrar. É o pré-requisito de
toda a feature.

**Independent Test**: Pode ser testado abrindo o modal de configurações na
página do assistente, adicionando um provedor com token, salvando e verificando
que o provedor passa a aparecer como opção no chat, sem que nenhuma outra
funcionalidade nova precise existir.

**Acceptance Scenarios**:

1. **Given** a página do assistente aberta, **When** o usuário aciona a área de
   configurações, **Then** um modal é aberto com a listagem dos provedores de IA
   configurados e as ações para gerenciá-los.
2. **Given** o modal de configurações aberto, **When** o usuário adiciona um
   provedor informando o provedor e o token, **Then** o provedor é salvo e passa
   a aparecer como opção no seletor do chat.
3. **Given** um provedor já configurado, **When** o usuário edita o token dele,
   **Then** o novo token passa a ser usado nas próximas chamadas desse provedor.
4. **Given** um provedor configurado, **When** o usuário o remove, **Then** ele
   deixa de aparecer no seletor do chat e não pode mais ser selecionado, sem
   afetar os demais provedores.
5. **Given** dois ou mais provedores configurados, **When** o usuário define um
   deles como padrão, **Then** o provedor padrão é adotado para conversas novas.

---

### User Story 2 - Selecionar e trocar o provedor diretamente no chat (Priority: P1)

Na barra inferior do chat, ao lado do campo de texto e do botão enviar, existe um
componente específico de seleção de provedor formado por pequenos badges
(botões), um para cada provedor disponível. O usuário troca de provedor com um
clique no badge desejado — sem abrir configurações — e vê claramente qual
provedor está selecionado. Quando o número de provedores cresce, o componente se
ajusta (ex.: rolagem horizontal ou quebra de linha) mantendo o acesso ao botão
enviar.

**Why this priority**: É o pedido central da feature — o provider deve ser
escolhido no local onde se escreve a mensagem, de forma rápida e sem ruído. É o
que permite ao usuário alternar provedores conforme a conveniência a cada
conversa.

**Independent Test**: Pode ser testado abrindo o chat com dois provedores
configurados, clicando nos badges para alternar entre eles e verificando que o
badge selecionado fica visualmente distinto e que uma mensagem enviada usa o
provedor selecionado no momento do envio.

**Acceptance Scenarios**:

1. **Given** ao menos um provedor configurado, **When** o chat é aberto, **Then**
   a barra inferior do chat exibe o componente com os badges dos provedores
   disponíveis junto ao campo de texto e ao botão enviar.
2. **Given** o componente de badges visível, **When** o usuário clica no badge de
   um provedor, **Then** o provedor é selecionado na hora, sem abrir nenhum modal,
   e o badge selecionado recebe estado visual distinto.
3. **Given** um provedor selecionado, **When** o usuário envia uma mensagem,
   **Then** a mensagem é enviada ao provedor selecionado no momento do envio.
4. **Given** um número de provedores configurados maior do que o espaço da barra
   suporta de uma vez, **When** o componente de badges é exibido, **Then** todos
   os provedores permanecem acessíveis (por rolagem horizontal ou quebra de
   linha) e o campo de texto e o botão enviar permanecem utilizáveis.

---

### User Story 3 - Consultar e abrir conversas anteriores na barra lateral (Priority: P1)

A página do assistente exibe uma barra lateral com as conversas anteriores
persistidas, identificadas por título, data/hora e prévia, ordenadas por
atualização. O usuário abre uma conversa pela barra lateral, revisita o
histórico e continua a conversa dali. Ao voltar para uma conversa anterior, o
provedor que estava selecionado nela é reaplicado.

**Why this priority**: É a porta de entrada para o histórico persistido (feature
003) dentro da página do assistente; sem ela o usuário não recupera conversas
de forma direta no contexto do chat.

**Independent Test**: Pode ser testado criando duas conversas com assuntos
distintos e provedores distintos, verificando que ambas aparecem na barra
lateral e que abrir cada uma restaura seu histórico e reaplica o provedor
selecionado naquela conversa.

**Acceptance Scenarios**:

1. **Given** ao menos uma conversa persistida, **When** a página do assistente é
   exibida, **Then** a barra lateral lista as conversas com título, data/hora e
   prévia, ordenadas pela mais recente.
2. **Given** uma conversa listada na barra lateral, **When** o usuário a abre,
   **Then** todo o histórico é restaurado e o provedor que estava selecionado
   naquela conversa é reaplicado no componente de badges.
3. **Given** uma conversa em andamento, **When** o usuário troca para outra
   conversa pela barra lateral, **Then** a conversa atual é salva e não perde
   nada da última troca de mensagens.

---

### User Story 4 - Gerenciar múltiplos provedores sem expor os tokens (Priority: P2)

O usuário gerencia vários provedores no modal de configurações (adicionar,
editar, remover, definir padrão), mas em nenhum momento o token é exibido por
completo: apenas o fato de estar configurado (e eventualmente um valor mascarado)
é apresentado. Mensagens de erro e estado nunca revelam a chave.

**Why this priority**: Segurança dos tokens é pré-requisito de confiança; a
gestão em si só faz sentido com vários provedores, o que depende das US1/US2
para existir.

**Independent Test**: Pode ser testado configurando um token, reabrindo o modal
e verificando que o token não aparece por completo em nenhuma tela ou mensagem,
e que editar exige digitá-lo novamente (ou confirmá-lo) para substituição.

**Acceptance Scenarios**:

1. **Given** um token configurado para um provedor, **When** o usuário reabre o
   modal de configurações, **Then** o token não é exibido por completo (valor
   mascarado ou apenas indicação de configurado).
2. **Given** uma mensagem de erro de autenticação (ex.: token inválido), **When**
   o erro é exibido, **Then** nenhuma parte do token aparece na mensagem.
3. **Given** um pedido para substituir o token, **When** o usuário salva um novo
   valor, **Then** o valor antigo deixa de ser usado em chamadas futuras.

---

### Edge Cases

- Nenhum provedor configurado: o chat orienta o usuário a configurar um provedor
  pelo modal; o componente de badges aparece vazio ou com estado de "configurar".
- Provedor selecionado sem token (apagado ou nunca informado): enviar mensagem
  exibe erro claro com atalho para o modal de configurações, sem expor a chave.
- Trocar de provedor no meio de uma conversa: a troca vale para as próximas
  mensagens; o histórico da conversa permanece intacto e o provedor da conversa
  é atualizado.
- Provedor removido que está selecionado na conversa atual: o chat cai para o
  provedor padrão (ou avisa que não há provedor) sem travar.
- Conversa anterior cujo provedor foi removido ou teve o token apagado: ao
  abrir, o chat avisa que o provedor não está mais disponível e orienta a trocar
  ou configurar.
- Número grande de provedores: os badges se ajustam (rolagem horizontal ou quebra
  de linha) sem esconder o campo de texto nem o botão enviar.
- Modal de configurações aberto durante o envio de mensagem: a troca de token ou
  provedor não interrompe as mensagens já em andamento; novas mensagens usam a
  configuração salva no momento do envio.
- Token duplicado ou provedor já configurado: a interface impede duplicar o mesmo
  provedor e orienta a editar o existente.
- Banco não conectado: as configurações de provedores funcionam normalmente; o
  fluxo depende apenas do assistente, não do banco.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: A página do assistente DEVE apresentar uma barra lateral com a
  lista de conversas anteriores persistidas (título, data/hora e prévia,
  ordenadas pela mais recente), permitindo abrir qualquer uma delas.
- **FR-002**: A página do assistente DEVE oferecer acesso a uma área de
  configurações que abre um modal dedicado aos provedores de IA.
- **FR-003**: O modal de configurações DEVE permitir adicionar um provedor
  informando o provedor e o token da API.
- **FR-004**: O modal de configurações DEVE permitir editar o token de um
  provedor já configurado.
- **FR-005**: O modal de configurações DEVE permitir remover um provedor
  configurado.
- **FR-006**: O modal de configurações DEVE permitir definir qual provedor é o
  padrão entre os configurados.
- **FR-007**: O modal de configurações NÃO DEVE exibir o token por completo em
  nenhum momento; valores configurados são apresentados mascarados ou apenas com
  indicação de "configurado".
- **FR-008**: O chat DEVE apresentar, na parte inferior da página (junto ao
  campo de texto e ao botão enviar), um componente específico de seleção de
  provedor composto por badges (pequenos botões), um para cada provedor
  disponível.
- **FR-009**: O usuário DEVE poder selecionar o provedor pelo badge
  correspondente, diretamente no chat, sem abrir o modal de configurações.
- **FR-010**: O badge do provedor atualmente selecionado DEVE apresentar estado
  visual distinto dos demais.
- **FR-011**: O componente de badges DEVE se adaptar quando o número de
  provedores crescer (ex.: rolagem horizontal ou quebra de linha), mantendo o
  acesso ao campo de texto e ao botão enviar.
- **FR-012**: O provedor selecionado DEVE ser associado à conversa atual e
  persistido/restaurado junto com ela (em consistência com a persistência de
  conversas existente), incluindo a reaplicação ao reabrir a conversa.
- **FR-013**: Ao enviar uma mensagem, o sistema DEVE usá-la com o provedor
  selecionado no momento do envio.
- **FR-014**: Ao enviar mensagem com um provedor sem token configurado ou com
  token inválido, o sistema DEVE exibir um erro orientado à ação com atalho para
  o modal de configurações, sem expor a chave.
- **FR-015**: Os tokens dos provedores DEVE ser mantido somente em memória durante a sessão (constituição III): a configuração feita no modal vale na sessão atual e é descartada ao fechar a aplicação, sem persistência em banco, arquivos, logs ou armazenamento local. (Decisão resolvida na pesquisa: constituição do projeto prevalece — credenciais efêmeras.)
- **FR-016**: A adição de um novo provedor NÃO DEVE exigir alterações na página
  do assistente nem no fluxo principal do chat (isolamento por provedor).
- **FR-017**: Ao remover um provedor que está selecionado na conversa atual ou em
  uma conversa reaberta, o sistema DEVE trocar para o provedor padrão ou orientar
  a configuração, sem travar o chat.
- **FR-018**: A página do assistente, o modal de configurações e todas as
  mensagens do sistema DEVE estar em português do Brasil.

### Key Entities *(include if feature involves data)*

- **Provedor de IA (configuração)**: provedor disponível no assistente com o
  token de acesso associado, indicador de provedor padrão e posição na lista de
  opções; o token é tratado conforme a decisão de armazenamento da FR-015.
- **Seleção de provedor da conversa**: provedor ativo em uma conversa específica,
  salvo e restaurado junto com a conversa persistida.
- **Badge de provedor**: elemento visual clicável do componente da barra inferior
  do chat, um por provedor disponível, com estado normal/selecionado.
- **Configuração do assistente**: conjunto de provedores configurados, gerenciado
  pelo modal de configurações da página do assistente.
- **Lista de conversas da barra lateral**: apresentação das conversas persistidas
  (mesma entidade da persistência de conversas existente) em barra lateral na
  página do assistente.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: O usuário consegue selecionar qualquer provedor disponível
  diretamente no chat com, no máximo, 1 clique no badge, sem abrir o modal de
  configurações, em 100% dos casos.
- **SC-002**: O usuário consegue adicionar e salvar um novo provedor pelo modal
  de configurações em menos de 2 minutos.
- **SC-003**: Em 100% dos casos, nenhum token é exibido por completo na
  interface nem aparece em mensagens de erro, logs ou respostas da aplicação.
- **SC-004**: Ao reabrir uma conversa persistida, o provedor que estava
  selecionado nela é reaplicado no seletor em 100% dos casos.
- **SC-005**: Com até 8 provedores configurados, o componente de badges mantém o
  layout da barra inferior utilizável, sem sobreposição e sem esconder o campo de
  texto ou o botão enviar.
- **SC-006**: 100% das conversas anteriores persistidas são acessíveis pela
  barra lateral a partir da página do assistente com um clique.
- **SC-007**: Enviar uma mensagem com provedor sem token gera aviso com ação
  corretiva e atalho para as configurações em 100% dos casos, sem expor a chave.

## Assumptions

- O MVP suporta os provedores Claude, OpenAI e Gemini como opções iniciais;
  demais provedores entram apenas via nova configuração isolada, sem alterar a
  página do assistente ou o fluxo do chat.
- A seleção de provedor pertence à conversa: fica salva com ela, é restaurada ao
  reabrir e a troca no meio da conversa vale a partir da próxima mensagem,
  preservando o histórico.
- O primeiro provedor configurado assume o papel de padrão; o padrão é alterável
  a qualquer momento pelo modal.
- A página do assistente é uma superfície única composta por: barra lateral de
  conversas, área do chat com o componente de badges na parte inferior, acesso ao
  modal de configurações. O modal independe de banco de dados conectado.
- O modal de configurações pode incluir uma validação opcional do token (ex.:
  teste de conexão) antes de salvar, sem exigir credenciais adicionais.
- Reutiliza integralmente os contratos e a persistência das features 002 (chat de
  IA com provedores isolados e resposta estruturada) e 003 (persistência do
  histórico de conversas e lista de conversas).
- A barra lateral usa a ordenação por última atualização já prevista na feature
  003; busca avançada e filtros ficam fora do escopo desta feature.
- Para o MVP não há necessidade de autenticação de usuários ou contas; o
  gerenciamento de provedores é local ao uso da aplicação.