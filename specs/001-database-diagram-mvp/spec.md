# Feature Specification: Database Diagram MVP

**Feature Branch**: `001-database-diagram-mvp`

**Created**: 2026-09-15

**Status**: Draft

**Input**: User description: "com base @docs/PRD.md especifique o projeto"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Conectar e visualizar o banco (Priority: P1)

Um desenvolvedor precisa entender a estrutura de um banco MySQL existente. Ele
informa os dados de conexão — provider, host, porta, database, usuário e senha —
testa a conexão e, em caso de sucesso, a aplicação lê a estrutura do banco e
apresenta um diagrama interativo com todas as tabelas, suas colunas, chaves
primárias, chaves estrangeiras e relacionamentos.

**Why this priority**: Sem conexão e visualização do schema não existe produto.
É o fluxo central e o primeiro milestone definido no PRD.

**Independent Test**: Pode ser testado ponta a ponta conectando a um MySQL de
exemplo e verificando que o diagrama é renderizado com tabelas, colunas, PKs,
FKs e relacionamentos corretos.

**Acceptance Scenarios**:

1. **Given** credenciais válidas de somente leitura, **When** o usuário informa
   a conexão e solicita o teste, **Then** a aplicação confirma a conexão com
   sucesso.
2. **Given** uma conexão confirmada, **When** o usuário solicita o carregamento
   do schema, **Then** todas as tabelas do database são exibidas com suas
   colunas, e PKs e FKs são identificados.
3. **Given** um banco com relacionamentos por chave estrangeira, **When** o
   diagrama é carregado, **Then** os relacionamentos entre tabelas são
   representados visualmente.
4. **Given** credenciais inválidas ou banco indisponível, **When** o usuário
   testa a conexão, **Then** uma mensagem de erro clara e segura é exibida,
   sem expor a senha ou detalhes internos.

---

### User Story 2 - Navegar e pesquisar no diagrama (Priority: P2)

Com o diagrama carregado, o desenvolvedor explora o banco: aplica zoom, move o
diagrama, arrasta as tabelas, altera suas posições e pesquisa tabelas pelo
nome. Quando o banco tem muitas tabelas, ele organiza automaticamente o layout
para reduzir a sobreposição e continuar a exploração.

**Why this priority**: Navegação e pesquisa são essenciais para bancos grandes,
onde o mapa completo fica inviável de inspecionar manualmente.

**Independent Test**: Pode ser testado apenas navegando: zoom in/out, fit view,
pan, mover tabelas, pesquisar por nome e reorganizar automaticamente.

**Acceptance Scenarios**:

1. **Given** um diagrama com várias tabelas, **When** o usuário aplica zoom in,
   zoom out ou fit view, **Then** a visualização responde imediatamente.
2. **Given** um diagrama carregado, **When** o usuário digita o nome (parcial)
   de uma tabela na busca, **Then** as tabelas correspondentes são destacadas
   ou filtradas.
3. **Given** um diagrama desorganizado, **When** o usuário solicita
   reorganização automática, **Then** as tabelas são reposicionadas sem
   sobreposição excessiva, mantendo os relacionamentos visíveis.
4. **Given** tabelas ocultadas pelo usuário, **When** ele restaura as tabelas,
   **Then** elas retornam ao diagrama nas posições anteriores.

---

### User Story 3 - Focar nos relacionamentos de uma tabela (Priority: P2)

Em um banco grande, o usuário seleciona uma tabela e solicita a exploração de
relacionamentos. A aplicação mostra somente a tabela selecionada e as tabelas
diretamente relacionadas, permitindo responder rapidamente à pergunta "o que se
relaciona com esta tabela?".

**Why this priority**: É a principal métrica do produto — reduzir o tempo para
entender os relacionamentos de um banco existente.

**Independent Test**: Selecionando uma tabela com relacionamentos e verificando
que apenas ela e suas tabelas relacionadas permanecem visíveis no diagrama.

**Acceptance Scenarios**:

1. **Given** um diagrama com uma tabela selecionada, **When** o usuário solicita
   "mostrar relacionamentos", **Then** apenas a tabela e as tabelas diretamente
   relacionadas permanecem visíveis.
2. **Given** a exploração de relacionamentos ativa, **When** o usuário retorna
   ao diagrama completo, **Then** todas as tabelas voltam a ser exibidas.
3. **Given** uma tabela sem relacionamentos, **When** o usuário explora seus
   relacionamentos, **Then** apenas a própria tabela é exibida.

---

### User Story 4 - Controlar a visibilidade do diagrama (Priority: P3)

O usuário filtra a visualização conforme a necessidade: oculta tabelas
irrelevantes, restaura tabelas ocultas e, quando a conexão expõe mais de um
schema em um database, compara o filtro por schema ou database.

**Why this priority**: O controle de visibilidade ajuda na exploração de bancos
grandes, mas depende de o diagrama já estar funcional.

**Independent Test**: Ocultando uma tabela, verificando que ela desaparece, e
restaurando-a em seguida; filtrando por schema quando mais de um está
disponível.

**Acceptance Scenarios**:

1. **Given** uma tabela visível no diagrama, **When** o usuário a oculta,
   **Then** ela desaparece sem romper os relacionamentos exibidos.
2. **Given** uma tabela oculta, **When** o usuário a restaura, **Then** ela
   reaparece com seus relacionamentos.
3. **Given** uma conexão com mais de um schema acessível, **When** o usuário
   aplica um filtro por schema, **Then** apenas as tabelas daquele schema são
   exibidas.

---

### Edge Cases

- Banco sem nenhuma tabela: o diagrama é exibido vazio com uma mensagem
  informativa.
- Tabela sem chave primária: a tabela é renderizada normalmente, sem indicador
  de PK.
- Tabela sem relacionamentos: a tabela aparece isolada no diagrama.
- Duas tabelas com o mesmo nome em schemas diferentes: ambas são distinguíveis
  no diagrama sem conflitos.
- Banco com centenas de tabelas: o schema carrega e o diagrama permanece
  navegável (não pode ficar inutilizável).
- Falha de conexão por host/porta indisponíveis, database inexistente ou
  credenciais inválidas: erro claro orientando à ação, sem expor senha,
  connection string ou detalhes internos.
- Demora anormal na leitura do schema: o usuário vê um estado de carregamento e
  pode cancelar ou tentar novamente.
- Ocultação de uma tabela que participa de relacionamentos: os relacionamentos
  da tabela são ocultados junto, sem deixar edges órfãos.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema DEVE permitir selecionar o provider do banco de dados
  na tela de conexão, estando apenas MySQL disponível no MVP.
- **FR-002**: O sistema DEVE permitir informar uma conexão com host, porta,
  database, usuário e senha.
- **FR-003**: O sistema DEVE usar a porta padrão `3306` para MySQL quando
  nenhuma porta for informada.
- **FR-004**: O sistema DEVE testar a conexão antes de carregar o schema.
- **FR-005**: O sistema DEVE identificar os schemas ou databases acessíveis
  conforme o provider utilizado.
- **FR-006**: O sistema DEVE identificar todas as tabelas acessíveis no
  database selecionado, priorizando tabelas e não views.
- **FR-007**: O sistema DEVE identificar as colunas de cada tabela, incluindo
  nome, tipo, tipo completo, nullable, posição, default, comentário quando
  disponível.
- **FR-008**: O sistema DEVE identificar chaves primárias.
- **FR-009**: O sistema DEVE identificar chaves estrangeiras.
- **FR-010**: O sistema DEVE identificar relacionamentos entre tabelas,
  incluindo tabela/coluna origem e destino, nome da constraint e regras de
  atualização/exclusão quando disponíveis.
- **FR-011**: O sistema DEVE identificar colunas com auto incremento quando a
  informação estiver disponível.
- **FR-012**: O sistema DEVE apresentar as tabelas visualmente com nome,
  schema, lista de colunas e indicadores de PK, FK e auto incremento.
- **FR-013**: O sistema DEVE apresentar os relacionamentos visualmente, de modo
  que a conexão origem→destino seja evidente.
- **FR-014**: O usuário DEVE conseguir mover as tabelas livremente no diagrama.
- **FR-015**: O usuário DEVE conseguir aplicar zoom in, zoom out e fit view.
- **FR-016**: O usuário DEVE conseguir pesquisar tabelas pelo nome.
- **FR-017**: O usuário DEVE conseguir ocultar tabelas do diagrama.
- **FR-018**: O usuário DEVE conseguir restaurar tabelas ocultas.
- **FR-019**: O usuário DEVE conseguir reorganizar automaticamente o diagrama
  para reduzir sobreposições.
- **FR-020**: O usuário DEVE conseguir selecionar uma tabela e visualizar
  somente os seus relacionamentos diretos, com a opção de retornar ao diagrama
  completo.
- **FR-021**: O usuário DEVE conseguir filtrar a visualização por schema ou
  database quando a conexão disponibilizar mais de um.
- **FR-022**: O sistema DEVE impedir qualquer operação de escrita no banco
  conectado (sem INSERT, UPDATE, DELETE ou comandos arbitrários).
- **FR-023**: O sistema DEVE tratar um provider isolado por tipo de banco, de
  modo que a adição de um novo banco não exija alterações na lógica principal
  de renderização do diagrama.
- **FR-024**: O sistema DEVE exibir estados claros de interface: desconectado,
  conectando, carregando schema, sucesso e erro.
- **FR-025**: O sistema DEVE apresentar toda a interface e todas as mensagens
  em português do Brasil, incluindo as mensagens geradas dinamicamente
  conforme o provider.

### Key Entities *(include if feature involves data)*

- **Conexão**: dados da conexão (provider, host, porta, database, usuário e
  senha), usados somente durante a sessão e nunca persistidos.
- **Esquema (Schema)**: banco/database descoberto, com nome, charset e
  collation quando disponíveis; agrupa as tabelas e os relacionamentos.
- **Tabela**: entidade do banco com nome, schema, tipo, engine, comentário e
  lista de colunas.
- **Coluna**: atributo de uma tabela com nome, tipo, tipo completo, nullable,
  posição, default, comentário e flags de PK, FK e auto incremento.
- **Relacionamento**: vínculo por chave estrangeira entre uma tabela/coluna
  origem e uma tabela/coluna destino, com nome da constraint e regras de
  atualização/exclusão quando disponíveis.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Um desenvolvedor consegue conectar a um banco MySQL e ver o
  diagrama completo em até 3 minutos a partir da tela inicial.
- **SC-002**: O tempo para descobrir "o que se relaciona com esta tabela?" por
  meio da exploração de relacionamentos é inferior a 30 segundos em banco de
  médio porte.
- **SC-003**: Um banco com centenas de tabelas é carregado e o diagrama
  permanece navegável, sem travar ou degradar o uso normal.
- **SC-004**: A busca por uma tabela pelo nome retorna o resultado esperado em
  menos de 1 segundo após digitação.
- **SC-005**: 100% das sessões de uso realizam somente operações de leitura no
  banco conectado; nenhuma escrita é executada pela aplicação.
- **SC-006**: Ao menos 90% dos casos de teste de conexão com dados corretos
  indicam sucesso à primeira tentativa e exibem o diagrama sem intervenção
  manual.

## Assumptions

- O MVP suporta somente MySQL; outros bancos entram por meio de novos providers
  em versões futuras.
- O usuário é um desenvolvedor com credenciais de somente leitura e permissão
  de leitura de metadados do banco.
- As conexões são efêmeras: as credenciais vivem apenas durante a sessão e não
  são persistidas, nem retornadas pela aplicação, nem registradas em logs.
- A interface e todas as mensagens são em português do Brasil, conforme os
  princípios do projeto.
- Views, procedures, triggers, índices detalhados, múltiplas conexões
  simultâneas e suporte a múltiplos providers no mesmo diagrama estão fora do
  escopo do MVP.
- O sistema é executado localmente por um único usuário por vez; não há
  autenticação nem gerenciamento de contas no MVP.
- O diagrama deve carregar corretamente mesmo em bancos sem relacionamentos ou
  sem chaves primárias.
- O funcionamento depende de um banco MySQL acessível no ambiente do usuário.