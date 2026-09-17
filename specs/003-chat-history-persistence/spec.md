# Feature Specification: Persistência do Histórico do Chat do Assistente

**Feature Branch**: `003-chat-history-persistence`

**Created**: 2026-09-16

**Status**: Draft

**Input**: User description: "Como usuario quero que ao conversar com chat do assistente, quero preservar o historico e mandar como contexto de volta para o ele, para que continue a conversa sem que o mesmo comece tudo do zero após cada interação, quero que ele seja capaz de continuar entendendo a query que ele sugeriu e faça modificações que o usuario solicitar na próxima interação, deve se persistir o historico da conversa, para que o usuario possa consultar mais tarde se nescessario"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Continuar a conversa com o contexto das interações anteriores (Priority: P1)

O usuário abre o chat do assistente e faz um pedido de consulta em linguagem
natural — por exemplo, "quero listar os contratos com cliente com status X" —
e recebe a resposta estruturada com o SQL. Na próxima interação, sem repetir
nada do contexto, ele escreve "agora exclua a coluna de contratos e inclua o
nome do cliente no retorno". O assistente continua entendendo a consulta que
havia sugerido e devolve uma nova versão do SQL com as modificações
solicitadas, junto dos metadados estruturados atualizados. Para isso, o
histórico da conversa (pedidos anteriores e respostas estruturadas) é enviado
como contexto junto com cada novo pedido.

**Why this priority**: É exatamente o valor descrito pelo usuário: o
assistente não recomeça do zero a cada interação, mas evolui a consulta
anterior a partir de pequenos pedidos de ajuste. Sem isso o chat continuaria
trocando a consulta inteira do zero, exigindo retrabalho.

**Independent Test**: Pode ser testado abrindo o chat, enviando um primeiro
pedido de consulta, e em seguida um pedido de modificação referenciando a
consulta anterior, verificando que a segunda resposta altera exatamente o
trecho solicitado da primeira consulta sem pedir que o usuário reforneça o
contexto completo.

**Acceptance Scenarios**:

1. **Given** uma conversa já com pedido e resposta exibidos, **When** o usuário
   envia um segundo pedido que referencia a consulta anterior ("na consulta
   anterior..."), **Then** o pedido enviado ao provedor inclui as mensagens
   anteriores como contexto e a resposta respeita a consulta anterior,
   modificando o trecho solicitado.
2. **Given** um follow-up que solicita mudanças de campos, filtros ou junções,
   **When** a resposta chega, **Then** o SQL exibido contém as modificações
   solicitadas e os metadados (tabelas, filtros, campos de retorno etc.) são
   atualizados conforme a nova consulta.
3. **Given** um follow-up sem referência suficiente a alguma consulta anterior
   exibida, **When** o assistente não consegue identificar o que modificar,
   **Then** o chat pede esclarecimento de forma educada, sem descartar a
   conversa.
4. **Given** uma conversa em andamento, **When** um novo pedido é enviado,
   **Then** a nova resposta é adicionada ao histórico e o restante da conversa
   permanece intacto abaixo dela.

---

### User Story 2 - Persistir o histórico entre sessões e continuar depois (Priority: P1)

O usuário conversa com o assistente, fecha a aplicação e volta mais tarde. Ele
reencontra a mesma conversa, com todas as mensagens anteriores (pedidos e
respostas estruturadas completas), e pode continuar dali: escrever novos
pedidos que o assistente entende como continuação daquele histórico, em vez de
começar tudo do zero.

**Why this priority**: A persistência é a base da continuidade no tempo. Sem
ela o valor da feature 002 (histórico dentro da sessão) existe apenas em
memória e a cada nova abertura da aplicação o usuário perde o trabalho e o
contexto acumulados.

**Independent Test**: Pode ser testado abrindo o chat, realizando algumas trocas
de mensagens, fechando a aplicação, reabrindo-a e verificando que a conversa
aparece com todo o histórico e que um novo pedido é respondido como continuação
da conversa anterior.

**Acceptance Scenarios**:

1. **Given** uma conversa com pedidos e respostas anteriores, **When** a
   aplicação é fechada e reaberta, **Then** a conversa continua acessível com
   todas as mensagens anteriores completas, com SQL exibido e copiável.
2. **Given** uma conversa reaberta após a aplicação ser reiniciada, **When** o
   usuário envia um novo pedido de continuação, **Then** o pedido ao provedor
   inclui o histórico persistido como contexto e a resposta é adicionada ao
   histórico.
3. **Given** uma resposta estruturada persistida, **When** o usuário reexamina
   essa resposta após reabrir, **Then** todos os metadados (explicação,
   objetivo, tabelas, relacionamentos, filtros, campos de retorno, tipo de
   consulta, resultado esperado e parâmetros) são apresentados integralmente.
4. **Given** uma conversa persistida associada a um banco/schema, **When** o
   usuário reabre a conversa, **Then** o sistema indica o contexto de banco da
   conversa e avisa se o banco atualmente conectado for diferente do banco
   registrado na conversa.

---

### User Story 3 - Listar, revisitar e copiar conversas anteriores (Priority: P2)

O usuário tem acesso a uma lista das conversas persistidas — identificadas por
um título claro — e pode abrir qualquer uma delas para reler pedidos, reexaminar
consultas geradas e copiar o SQL novamente, sem precisar refazer nada. Ele
também pode iniciar uma conversa nova vazia sempre que quiser, sem que isso
apague as anteriores.

**Why this priority**: É o "consultar mais tarde se necessário" do pedido: o
histórico precisa ser encontrável e navegável, e não apenas gravado em algum
lugar.

**Independent Test**: Pode ser testado criando duas conversas com assuntos
distintos, verificando que ambas aparecem na lista com títulos identificáveis,
que abrir qualquer uma restaura o histórico e que o SQL de mensagens antigas
segue copiável com um clique.

**Acceptance Scenarios**:

1. **Given** ao menos uma conversa persistida, **When** o usuário abre a lista
   de conversas, **Then** cada conversa aparece com um título, data/hora e uma
   prévia curta que permitem identificá-la.
2. **Given** uma conversa escolhida na lista, **When** o usuário a abre, **Then**
   o histórico completo é restaurado e o SQL de qualquer resposta segue copiável
   com um único clique.
3. **Given** o usuário na lista de conversas, **When** ele inicia uma nova
   conversa, **Then** uma conversa vazia é criada e as conversas anteriores
   permanecem na lista.
4. **Given** uma conversa em andamento, **When** o usuário troca para outra
   conversa da lista, **Then** a conversa atual é salva (incluindo a última
   mensagem recebida) e a troca não perde nada.

---

### User Story 4 - Renomear e excluir conversas (Priority: P2)

O usuário consegue renomear uma conversa para um título que ele reconheça e
consegue excluir uma conversa permanentemente. Ao excluir, todas as mensagens
daquela conversa deixam de existir e não podem mais ser recuperadas.

**Why this priority**: Trata-se de histórico que pode conter consultas sobre
dados de negócio; o controle sobre o que permanece guardado e como é
identificado é parte da confiança na persistência. A exclusão é o mecanismo de
"apagar se necessário" mencionado na necessidade.

**Independent Test**: Pode ser testado renomeando uma conversa e conferindo o
novo título na lista, e excluindo uma conversa e verificando que ela some da
lista e que não é reexibida depois de reiniciar a aplicação.

**Acceptance Scenarios**:

1. **Given** uma conversa na lista, **When** o usuário a renomeia, **Then** o
   novo título é usado na lista e persiste após reabrir a aplicação.
2. **Given** uma conversa selecionada, **When** o usuário confirma a exclusão,
   **Then** a conversa some da lista e suas mensagens não são mais recuperáveis
   pela aplicação, mesmo após reiniciar.
3. **Given** uma exclusão de conversa em andamento, **When** a confirmação é
   concluída, **Then** o chat volta ao estado inicial (lista de conversas ou
   nova conversa) sem mensagens remanescentes da excluída.

---

### User Story 5 - Garantir privacidade e manter respostas em tempo útil com históricos longos (Priority: P3)

O histórico persistido e o contexto enviado ao provedor nunca contêm segredos
(chaves de API, senhas). Quando uma conversa fica muito longa, a aplicação
limita o contexto enviado ao provedor às mensagens mais recentes, mantendo
latência e custo sob controle, e informa o usuário que trechos antigos não
estão sendo reenviados como contexto (embora continuem persistidos e visíveis).

**Why this priority**: A privacidade das credenciais e a viabilidade de respostas
com conversas longas são pré-requisitos de confiança e usabilidade, mas dependem
das funcionalidades anteriores para existir.

**Independent Test**: Pode ser testado abrangendo uma conversa longa (muitas
trocas de mensagens) e verificando que respostas recentes continuam a chegar em
tempo útil, que o histórico antigo continua visível no chat e que nenhum segredo
persiste.

**Acceptance Scenarios**:

1. **Given** uma conversa com histórico longo, **When** o usuário envia um novo
   pedido, **Then** a resposta chega em tempo útil (a aplicação não inclui
   mensagens além da janela configurada como contexto ao provedor).
2. **Given** uma conversa com histórico longo, **When** o usuário examina a
   conversa, **Then** todas as mensagens anteriores permanecem visíveis,
   mesmo as fora da janela de contexto enviada ao provedor.
3. **Given** qualquer mensagens persistidas ou enviadas como contexto, **When**
   o histórico é persistido ou enviado ao provedor, **Then** nenhuma chave de
   API ou senha de banco é incluída.
4. **Given** uma janela de contexto ativa (mensagens fora da janela não enviadas
   como contexto), **When** o usuário envia um pedido, **Then** a interface
   informa de forma sutil que somente as mensagens mais recentes foram usadas
   como contexto.

---

### Edge Cases

- Conversa muito longa: o contexto enviado ao provedor é limitado à janela das
  mensagens mais recentes; o restante permanece completo no histórico visível.
- Primeira interação de uma conversa persistida reaberta: funciona como o fluxo
  normal da feature 002, sem diferença para o usuário.
- Follow-up vago sem referência clara a nenhum pedido anterior: o assistente
  pede esclarecimento e não quebra o chat.
- Reabrir conversa com banco/schema diferente do registrado na conversa: o
  sistema exibe aviso e, opcionalmente, impede novas consultas sem confirmação
  explícita do usuário.
- Exclusão de conversa em andamento: a conversa é removida da lista e o chat
  volta à lista de conversas ou a uma nova conversa.
- Falha ao persistir o histórico (ex.: falta de espaço em disco): o sistema
  informa que o histórico não pôde ser salvo, preserva o chat em memória e
  orienta o usuário a resolver o problema.
- Formato de armazenamento legado: uma conversa salva em versão anterior da
  aplicação pode não ser lida; o sistema exibe a conversa incompleta com aviso
  de que parte do histórico não pôde ser restaurada.
- Duas conversas com o primeiro pedido idêntico: o título é diferenciado (ex.:
  por data ou sufixo numérico) para que a lista não tenha duplicatas
  indistinguíveis.
- Usuário fecha a aplicação durante a geração de resposta: a resposta é perdida
  e o histórico persiste até a última mensagem salva; o usuário pode tentar
  novamente ao reabrir.
- Conversa com mensagens do assistente em formato diferente da versão atual: o
  histórico é exibido como estiver (parcialmente se necessário) com aviso.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema DEVE persistir duravelmente todas as mensagens de cada
  conversa (pedidos do usuário e respostas estruturadas do assistente) para que
  sobrevivam à finalização/reinício da aplicação.
- **FR-002**: O sistema DEVE enviar o histórico relevante da conversa como
  contexto junto com cada novo pedido ao provedor de IA, para que o assistente
  continue a conversa a partir das interações anteriores sem que o usuário
  precise repetir o contexto.
- **FR-003**: O assistente DEVE ser capaz de referenciar e modificar a consulta
  sugerida em respostas anteriores a partir de um pedido de ajuste do usuário,
  sem exigir que o usuário reforneça o contexto da consulta anterior.
- **FR-004**: O sistema DEVE manter uma lista de conversas persistidas acessível
  ao usuário, onde cada conversa aparece identificada por título, data/hora e
  prévia do conteúdo.
- **FR-005**: O sistema DEVE restaurar integralmente o histórico de uma conversa
  ao ser aberta, com todos os metadados (SQL, explicação, objetivo, tabelas,
  relacionamentos, filtros, campos de retorno, tipo de consulta, resultado
  esperado e parâmetros) presentes, legíveis e copiáveis.
- **FR-006**: O sistema DEVE permitir ao usuário continuar uma conversa
  restaurada (enviar novos pedidos) com o histórico persistido sendo usado
  como contexto.
- **FR-007**: O sistema DEVE permitir ao usuário excluir uma conversa, removendo
  permanentemente todas as suas mensagens, que não devem mais ser recuperáveis
  pela aplicação.
- **FR-008**: O sistema DEVE permitir ao usuário renomear o título de uma
  conversa persistida.
- **FR-009**: O sistema DEVE permitir ao usuário iniciar uma nova conversa (vazia)
  sem descartar conversas anteriores.
- **FR-010**: O sistema DEVE limitar o contexto enviado ao provedor de IA às
  mensagens mais recentes da conversa (janela configurável) quando o histórico
  ultrapassar o limite, mantendo latência e custo em níveis aceitáveis.
- **FR-011**: O sistema DEVE informar o usuário, quando aplicável, que o contexto
  enviado ao provedor contém somente as mensagens mais recentes (janela), sem
  que isso impeça a visualização do histórico completo.
- **FR-012**: Nenhuma chave de API ou senha de banco DEVE ser persistida ou
  incluída no contexto enviado ao provedor.
- **FR-013**: O sistema DEVE associar cada conversa à identidade do banco/schema
  usado na sua criação e exibir um aviso quando o banco conectado atualmente
  for diferente do registrado.
- **FR-014**: O sistema DEVE tratar falhas de persistência (ex.: falta de
  espaço) informando o usuário, preservando o chat em memória e orientando a
  resolução.
- **FR-015**: Toda a interface e todas as mensagens do sistema DEVE estar em
  português do Brasil.
- **FR-016**: O formato de persistência DEVE incluir uma versão que permita ao
  sistema detectar conversas de versões anteriores e exibir o que for possível
  com aviso claro.
- **FR-017**: O sistema DEVE permitir trocar entre conversas distintas sem
  perder o estado da conversa atual (salvando-a automaticamente).

### Key Entities

- **Conversa**: unidade de interação contínua entre o usuário e o assistente,
  identificada por um título (renomeável), data/hora de criação e atualização,
  referência ao banco/schema usado e lista ordenada de mensagens.
- **Mensagem**: cada interação dentro de uma conversa — pode ser um pedido em
  linguagem natural do usuário ou uma resposta estruturada do assistente com SQL
  e metadados; inclui data/hora.
- **Consulta gerada**: parte da resposta estruturada contendo o SQL gerado e seus
  metadados explicativos (explicação, objetivo, tabelas, relacionamentos,
  filtros, campos de retorno, tipo de consulta, resultado esperado e
  parâmetros).
- **Janela de contexto**: recorte das mensagens mais recentes de uma conversa
  usado como contexto ao enviar um pedido ao provedor, com tamanho máximo
  configurável.
- **Lista de conversas**: visão agregada de todas as conversas persistidas,
  ordenadas por data da última atualização, usada como navegação entre
  conversas.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% das mensagens de uma conversa são recuperadas integralmente
  em sua estrutura (texto e metadados) após fechar e reabrir a aplicação.
- **SC-002**: Em um pedido de follow-up sem repetir o contexto da consulta
  anterior, o assistente aplica corretamente a modificação solicitada sobre a
  consulta anterior em ao menos 90% dos casos válidos.
- **SC-003**: Ao reabrir uma conversa persistida, o usuário consegue copiar o
  SQL de qualquer resposta antiga com um único clique em 100% dos casos.
- **SC-004**: O tempo de resposta do provedor com contexto de histórico
  (janela) não é percebido como significativamente pior pelo usuário em
  conversas de até 30 mensagens, mantendo tempos de resposta comparáveis aos
  do fluxo sem contexto (observação qualitativa; objeção objetiva: a janela
  evita crescimento ilimitado de latência).
- **SC-005**: 100% das conversas persistidas não contêm chaves de API ou senhas
  em nenhuma mensagem persistida ou contexto enviado.
- **SC-006**: Ao excluir uma conversa, 100% das mensagens dela deixam de ser
  recuperáveis pela aplicação, mesmo após reiniciar a aplicação.
- **SC-007**: O usuário consegue encontrar e abrir qualquer conversa persistida
  pela lista, em 100% dos casos, sem necessidade de busca em outros locais.
- **SC-008**: 100% dos pedidos enviados em conversas com histórico utilizam o
  histórico como contexto (janela) no pedido ao provedor, em vez de enviar
  apenas o pedido atual.

## Assumptions

- Persistência local no ambiente da aplicação (sem contas, sem sync em nuvem),
  em linha com a condição de projeto de usuário único sem autenticação.
- O histórico completo é sempre persistido; somente a janela das mensagens mais
  recentes (tamanho configurável) é enviada como contexto ao provedor, para
  manter custo e latência em níveis aceitáveis.
- O título da conversa é gerado automaticamente a partir do primeiro pedido
  do usuário; o usuário pode renomear livremente.
- Reutiliza o contrato comum de esquema de banco (incluindo versão do banco)
  e o contrato estruturado da resposta do assistente já estabelecidos na
  feature 002.
- Evolução explícita da decisao da feature 002 de descartar o histórico no
  fechamento da sessão: esta feature substitui essa decisão e passa a persistir
  o histórico.
- Nenhuma chave de API, senha ou segredo semelhante participa do histórico
  persistido ou do contexto enviado; um segredo acidentalmente incluso em um
  pedido do usuário é ignorado na persistência.
- Conversa pertence à identidade do banco/schema usado na sua criação; a
  alteração do banco conectado gera aviso ao reabrir, sem bloquear a
  visualização do histórico.
- O formato de persistência é versionado para suportar a leitura de conversas
  salvas por versões anteriores da aplicação.
- Retenção das conversas: indefinida até exclusão explícita pelo usuário;
  não há política de expiração automática no MVP.
- A aplicação continua sendo single-thread para o chat: a persistência e a
  chamada ao provedor são operações síncronas na sequência do fluxo.
- O backend continua sem estado entre requisições; a persistência é feita em
  camada própria (arquivo local ou banco de aplicação, não o banco de dados
  conectado pelo usuário).
