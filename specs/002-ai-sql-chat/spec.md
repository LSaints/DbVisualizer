# Feature Specification: Assistente IA de Consultas SQL

**Feature Branch**: `002-ai-sql-chat`

**Created**: 2026-09-16

**Status**: Draft

**Input**: User description: "quero adicionar uma sessão onde o usuario terá um chat para conversar com uma LLM via api indepentende do provedor de IA (claude, openai, gemini e etc...), quero enviar o contexto das tabelas e do provedor de banco de dados junto com a versão para que ele entregue queries sql tendo o contexto completo do ambiente que ele se encontra... deve gerar uma resposta estruturada... deve ter uma interface intuitiva para que comporte todos esses dados na resposta do chat, mantendo um historico de consultas via chat"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Pedir uma consulta SQL em linguagem natural e receber resposta estruturada (Priority: P1)

Com uma conexão de banco ativa e o schema já carregado, o usuário abre a sessão
de chat de IA, digita um pedido em linguagem natural — por exemplo "quero
consultar todos os contratos com cliente com status X" — e a aplicação envia ao
provedor de IA o pedido acompanhado do contexto completo do ambiente (tabelas,
colunas, relacionamentos, provider do banco e versão). A resposta chega em um
formato estruturado e é apresentada no chat de forma legível: o SQL gerado em
destaque, além de explicação, objetivo, tabelas, relacionamentos, filtros,
campos de retorno, tipo de consulta, resultado esperado e parâmetros.

**Why this priority**: Sem esse fluxo não existe o produto. A resposta
estruturada com contexto completo é o núcleo do valor entregue: um SQL correto
para o banco real do usuário, na primeira resposta.

**Independent Test**: Pode ser testado ponta a ponta abrindo o chat sobre um
banco MySQL com schema carregado, enviando um pedido de consulta e verificando
que a resposta apresenta o SQL gerado e todos os metadados estruturados da
consulta.

**Acceptance Scenarios**:

1. **Given** um banco conectado com schema carregado e um provedor de IA
   configurado, **When** o usuário envia um pedido de consulta em linguagem
   natural, **Then** a resposta contém um SQL gerado e os metadados estruturados
   (explicação, objetivo, tabelas, relacionamentos, filtros, campos de retorno,
   tipo de consulta, resultado esperado e parâmetros).
2. **Given** um banco conectado, **When** o pedido é enviado, **Then** o pedido
   enviado ao provedor inclui o contexto do banco: tabelas, colunas,
   relacionamentos, provider e versão.
3. **Given** um schema com tabelas `contratos` e `clientes` com chave
   estrangeira `contratos.cliente_id → clientes.id`, **When** o usuário pede
   "todos os contratos com cliente com status X", **Then** o SQL gerado usa
   `INNER JOIN` entre as tabelas e filtra por `status = 'X'`.
4. **Given** um provedor de IA configurado, **When** a resposta chega como um
   JSON estruturado válido, **Then** todos os campos do contrato são exibidos de
   forma legível no chat, sem mostrar o JSON bruto.

---

### User Story 2 - Escolher o provedor de IA e configurar o acesso (Priority: P1)

O usuário escolhe com qual provedor de IA deseja conversar (Claude, OpenAI,
Gemini, etc.) e informa a chave de API correspondente. A chave é usada somente
durante a sessão atual: não é persistida em banco, nem em arquivos, nem em logs,
nem retornada pela aplicação. Um novo provedor pode ser adicionado sem alterar o
fluxo do chat.

**Why this priority**: A escolha do provedor é o pré-requisito para qualquer
conversa e define o isolamento por provedor que o projeto exige.

**Independent Test**: Pode ser testado abrindo a tela de configuração da
sessão de IA, selecionando um provedor disponível, informando a chave e
verificando que o chat passa a responder usando aquele provedor; um segundo
provedor pode ser selecionado e funciona sem alterar o chat.

**Acceptance Scenarios**:

1. **Given** ao menos um provedor de IA registrado na aplicação, **When** o
   usuário abre a sessão de chat, **Then** ele consegue selecionar um provedor
   e informar a chave de API.
2. **Given** uma chave de API informada, **When** o usuário usa o chat, **Then**
   a chave permanece apenas em memória e não aparece em nenhuma tela, log,
   arquivo de configuração ou resposta da aplicação após ser configurada.
3. **Given** um provedor de IA selecionado, **When** o usuário envia um pedido,
   **Then** a resposta é gerada pelo provedor selecionado.
4. **Given** um novo provedor de IA, **When** ele é adicionado à aplicação,
   **Then** o chat passa a oferecê-lo sem necessidade de alterar o fluxo
   principal de conversa.

---

### User Story 3 - Visualizar e copiar consultas geradas (Priority: P2)

O usuário recebe cada consulta no chat com a visualização organizada dos
metadados: o SQL em destaque com sintaxe destacável, explicação e objetivo em
texto corrido, e as seções de tabelas, relacionamentos, filtros, campos de
retorno, tipo de consulta, resultado esperado e parâmetros apresentadas de
forma tabular e legível. Ele consegue copiar o SQL completo com um clique.

**Why this priority**: A legibilidade da resposta e a cópia em um passo são o
que transforma a resposta da IA em um artefato utilizável pelo usuário.

**Independent Test**: Enviando um pedido, verificando que cada seção estruturada
aparece na interface de forma legível e que o botão de copiar coloca o SQL
completo na área de transferência.

**Acceptance Scenarios**:

1. **Given** uma resposta estruturada exibida no chat, **When** o usuário
   examina a resposta, **Then** o SQL aparece em destaque e cada metadado
   (explicação, objetivo, tabelas, relacionamentos, filtros, campos de retorno,
   tipo de consulta, resultado esperado, parâmetros) é apresentado em sua
   própria seção.
2. **Given** um SQL exibido no chat, **When** o usuário aciona copiar, **Then**
   o SQL completo é copiado para a área de transferência com um único clique.
3. **Given** uma resposta com múltiplas tabelas e relacionamentos, **When** a
   resposta é exibida, **Then** as tabelas listam nome, apelido e função, e os
   relacionamentos mostram origem, destino e tipo de junção.

---

### User Story 4 - Revisitar consultas anteriores pelo histórico do chat (Priority: P2)

O chat mantém o histórico de toda a conversa da sessão: os pedidos do usuário e
as respostas estruturadas do assistente. O usuário rola o histórico, relê um
pedido anterior, reexamina a consulta gerada e a copia novamente, sem precisar
refazer o pedido.

**Why this priority**: O histórico é o que permite reutilizar trabalho já feito
e comparar respostas recebidas ao longo da exploração.

**Independent Test**: Enviando vários pedidos na mesma sessão e verificando que
todas as trocas permanecem visíveis e que qualquer resposta antiga continua
exibida integralmente e copiável.

**Acceptance Scenarios**:

1. **Given** um chat com várias interações na sessão, **When** o usuário rola o
   histórico, **Then** todas as mensagens (pedidos e respostas) permanecem
   visíveis na ordem em que ocorreram.
2. **Given** uma resposta antiga no histórico, **When** o usuário reexamina essa
   resposta, **Then** ela continua completa, com SQL e metadados, e o SQL segue
   copiável.
3. **Given** um usuário que deseja começar de novo, **When** ele inicia uma nova
   conversa, **Then** o histórico da sessão anterior é descartado e o chat volta
   ao estado inicial.

---

### User Story 5 - Lidar com falhas do provedor e contexto indisponível (Priority: P3)

Quando algo falha — chave inválida, provedor indisponível, limite de uso
atingido, demora na resposta ou pedido sem contexto de banco — o usuário recebe
uma mensagem clara que aponta a ação possível, sem expor a chave ou detalhes
internos. Caso nenhum banco esteja conectado, o chat avisa que não há contexto
de schema para gerar a consulta.

**Why this priority**: Erros claros evitam bloqueios, mas dependem das demais
funcionalidades para existir; por isso é a prioridade mais baixa.

**Independent Test**: Enviando um pedido com uma chave inválida, verificando a
mensagem orientada à ação; enviando um pedido sem banco conectado, verificando
o aviso de contexto ausente.

**Acceptance Scenarios**:

1. **Given** uma chave de API inválida ou revogada, **When** o usuário envia um
   pedido, **Then** a aplicação exibe um erro claro orientando à verificação da
   chave, sem expor a chave completa em nenhuma mensagem.
2. **Given** um provedor indisponível ou com limite de uso atingido, **When** o
   usuário envia um pedido, **Then** a aplicação informa o problema e sugere
   aguardar ou trocar de provedor.
3. **Given** nenhum banco conectado ou schema não carregado, **When** o usuário
   envia um pedido de consulta, **Then** a aplicação avisa que não há contexto
   de banco para gerar a consulta.
4. **Given** uma resposta fora do formato estruturado esperado, **When** a
   aplicação recebe a resposta, **Then** ela informa que a resposta não pôde ser
   interpretada e permite tentar novamente.

---

### Edge Cases

- Nenhum banco conectado: o chat avisa que não há contexto de schema e orienta a
  conectar e carregar o diagrama antes de consultar.
- Banco conectado sem nenhuma tabela: o contexto é enviado vazio e a resposta é
  gerada com o aviso de que não foram encontradas tabelas.
- Resposta do provedor fora do formato estruturado (JSON inválido ou campos
  ausentes): a aplicação não quebra o chat, informa que a resposta não pôde ser
  interpretada e permite nova tentativa.
- Demora na resposta do provedor: o chat permanece em estado "gerando resposta"
  e permite cancelar e tentar novamente; respostas que excedem um limite
  razoável de tempo são tratadas como falha com mensagem orientada à ação.
- Chave de API inválida ou revogada: mensagem de erro genérica orientando à ação,
  sem expor a chave.
- Provedor indisponível ou limite de uso atingido (rate limit): mensagem clara
  sugerindo aguardar ou trocar de provedor.
- Pedido vazio ou muito curto: o sistema orienta o usuário a descrever o que
  deseja consultar.
- Pedido que implique operação de escrita (INSERT/UPDATE/DELETE/DDL): a
  aplicação gera somente consultas de leitura e avisa quando o pedido não é
  compatível, sem executar nada.
- Histórico do chat: mantém-se somente durante a sessão; ao fechar ou iniciar uma
  nova conversa, o histórico é descartado.
- Banco com centenas de tabelas: o contexto enviado permanece enxuto o
  suficiente para uma resposta em tempo útil, sem truncar silenciosamente
  informações essenciais.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema DEVE oferecer uma sessão de chat de IA acessível a
  partir do fluxo existente da aplicação.
- **FR-002**: O usuário DEVE poder escolher o provedor de IA (Claude, OpenAI,
  Gemini, etc.) entre os provedores disponíveis na aplicação.
- **FR-003**: O usuário DEVE poder informar a chave de API do provedor
  selecionado; a chave DEVE permanecer somente em memória durante a sessão e
  NUNCA ser persistida, logada, exibida ou retornada pela aplicação.
- **FR-004**: O sistema DEVE suportar cada provedor de IA de forma isolada, de
  modo que a adição de um novo provedor não exija alterações no fluxo principal
  do chat.
- **FR-005**: O sistema DEVE enviar, junto a cada pedido, o contexto do
  ambiente: tabelas, colunas, relacionamentos, provider do banco e versão,
  quando houver um banco conectado e um schema carregado.
- **FR-006**: O sistema DEVE indicar ao usuário qual banco/schema está sendo
  usado como contexto da conversa; sem contexto, o sistema DEVE avisar que não
  há schema carregado.
- **FR-007**: O usuário DEVE poder digitar pedidos em linguagem natural e
  enviá-los ao provedor de IA.
- **FR-008**: O sistema DEVE receber a resposta do provedor em formato
  estruturado e validar a presença dos campos do contrato: consulta, explicação,
  objetivo, tabelas, relacionamentos, filtros, campos de retorno, tipo de
  consulta, resultado esperado e parâmetros.
- **FR-009**: O sistema DEVE exibir cada resposta de forma legível no chat, com
  o SQL gerado em destaque e cada metadado apresentado em uma seção própria
  (tabelas, relacionamentos, filtros, campos de retorno, tipo de consulta,
  resultado esperado e parâmetros).
- **FR-010**: O usuário DEVE poder copiar o SQL completo de uma resposta com um
  único clique.
- **FR-011**: O chat DEVE manter o histórico completo da conversa durante a
  sessão, permitindo revisitar, reler e copiar respostas anteriores.
- **FR-012**: O usuário DEVE poder iniciar uma nova conversa, descartando o
  histórico da sessão atual.
- **FR-013**: O sistema DEVE exibir estados claros de interface no chat:
  aguardando envio, gerando resposta, sucesso e erro, com possibilidade de
  cancelar enquanto a resposta está sendo gerada.
- **FR-014**: O sistema DEVE tratar falhas do provedor (chave inválida,
  indisponibilidade, limite de uso, tempo excedido) com mensagens orientadas à
  ação, sem expor a chave ou detalhes internos.
- **FR-015**: O sistema DEVE tratar respostas fora do formato estruturado sem
  interromper o chat, informando o usuário e permitindo nova tentativa.
- **FR-016**: O sistema NUNCA DEVE executar as consultas geradas pelo
  assistente; o SQL é apenas exibido e copiado pelo usuário.
- **FR-017**: O sistema DEVE limitar a geração a consultas de leitura (SELECT)
  e DEVE avisar o usuário quando um pedido implicar operações de escrita.
- **FR-018**: Toda a interface e todas as mensagens do chat DEVE estar em
  português do Brasil.
- **FR-019**: O sistema DEVE tratar pedidos vazios ou insuficientes orientando o
  usuário a descrever o que deseja consultar.

### Key Entities *(include if feature involves data)*

- **Configuração do provedor de IA**: provedor escolhido e chave de API
  (somente memória), usados para autenticar as chamadas ao provedor durante a
  sessão.
- **Sessão de chat**: conversa corrente entre o usuário e o assistente, com o
  histórico de mensagens mantido apenas durante a sessão.
- **Mensagem**: pedido em linguagem natural do usuário ou resposta estruturada
  do assistente; compõe o histórico do chat.
- **Consulta gerada**: SQL resultante acompanhado dos metadados estruturados —
  explicação, objetivo, tabelas (nome, apelido, função), relacionamentos
  (origem, campo de origem, destino, campo de destino, tipo de junção,
  explicação), filtros (campo, operador, valor, explicação), campos de retorno,
  tipo de consulta, resultado esperado e parâmetros.
- **Contexto de banco**: snapshot do ambiente usado para montar o pedido ao
  provedor — tabelas, colunas, relacionamentos, provider do banco e versão.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Um usuário consegue, a partir de uma frase em linguagem natural,
  receber uma consulta SQL estruturada com contexto completo em até 60 segundos,
  em um banco de médio porte conectado.
- **SC-002**: 100% das chaves de API informadas permanecem somente em memória;
  nenhuma chave aparece em banco de dados, arquivos, logs ou respostas da
  aplicação.
- **SC-003**: Ao menos 90% das respostas bem-sucedidas do provedor são
  apresentadas no contrato estruturado completo (todos os campos do contrato
  presentes e legíveis), sem exigir retrabalho manual do usuário.
- **SC-004**: O usuário consegue copiar o SQL de qualquer resposta exibida no
  chat com um único clique, em 100% dos casos.
- **SC-005**: Ao menos 90% dos usuários conseguem revisitar uma consulta
  anterior pelo histórico do chat sem precisar refazer o pedido.
- **SC-006**: Em 100% dos pedidos enviados com um banco conectado e schema
  carregado, o contexto de ambiente (tabelas, colunas, relacionamentos,
  provider e versão) acompanha o pedido ao provedor.
- **SC-007**: 100% das consultas geradas são somente exibidas e copiadas; a
  aplicação nunca executa o SQL gerado.

## Assumptions

- O usuário fornece sua própria chave de API do provedor de IA escolhido; a
  aplicação não gerencia contas ou assinaturas.
- A chave de API e o histórico do chat vivem somente durante a sessão em memória;
  persistência entre execuções está fora do escopo do MVP, em linha com o
  princípio de credenciais efêmeras da constituição do projeto.
- O MVP usa o schema já carregado pelo fluxo de diagrama existente como contexto
  de banco, reutilizando o contrato comum de schema já estabelecido.
- O MVP suporta ao menos um provedor de IA funcional; demais provedores entram
  apenas via nova implementação isolada, sem alterar o fluxo do chat.
- O assistente gera somente consultas de leitura (SELECT); operações de escrita
  são avisadas ao usuário. As consultas geradas nunca são executadas pela
  aplicação (princípio somente leitura da constituição).
- O contrato da resposta estruturada segue o formato em pt-BR fornecido pelo
  usuário (consulta, explicacao, objetivo, tabelas, relacionamentos, filtros,
  campos_retorno, tipo_consulta, resultado_esperado, parametros).
- O funcionamento depende de acesso de rede aos provedores de IA escolhidos.
- O chat é utilizado por um único usuário por vez, sem autenticação nem
  gerenciamento de contas no MVP, conforme o escopo atual do projeto.
- Pedidos de contexto com bancos de centenas de tabelas são simplificados para
  manter a resposta em tempo útil sem perder as informações essenciais do
  relacionamento.