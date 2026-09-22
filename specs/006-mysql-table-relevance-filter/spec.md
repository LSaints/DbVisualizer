# Feature Specification: Filtro de Relevância de Tabelas na Introspecção MySQL

**Feature Branch**: `006-mysql-table-relevance-filter`

**Created**: 2026-09-22

**Status**: Draft

**Input**: User description: "Alterar a descoberta de tabelas do provedor MySQL para considerar como relevantes apenas as tabelas com quantidade estimada de registros (TABLE_ROWS) maior que 10, evitando poluir o diagrama com tabelas vazias ou praticamente irrelevantes, mantendo o limite de segurança de 500 tabelas, a ordenação por relevância (TABLE_ROWS DESC) e o fluxo atual de introspecção de colunas, chaves primárias e chaves estrangeiras."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Diagrama sem ruído de tabelas vazias (Priority: P1)

Como usuário que conecta um banco MySQL para gerar o diagrama, quero que o sistema mostre apenas tabelas que efetivamente possuem dados relevantes, para que o diagrama fique legível e focado nas entidades de negócio reais, em vez de poluído por tabelas de apoio, temporárias ou vazias.

**Why this priority**: É o núcleo do pedido de mudança — sem esse filtro, o restante da introspecção (colunas, PKs, FKs) continua funcionando normalmente, mas o problema de poluição visual não é resolvido.

**Independent Test**: Pode ser testado conectando a um banco com uma mistura de tabelas vazias/pequenas (≤10 registros estimados) e tabelas com dados (>10 registros estimados) e verificando que apenas as tabelas com dados aparecem no diagrama resultante.

**Acceptance Scenarios**:

1. **Given** um banco MySQL com tabelas cuja quantidade estimada de registros é maior que 10, **When** o usuário solicita a introspecção do schema, **Then** essas tabelas aparecem no diagrama com suas colunas normalmente.
2. **Given** um banco MySQL com tabelas cuja quantidade estimada de registros é 10 ou menos (incluindo tabelas vazias), **When** o usuário solicita a introspecção do schema, **Then** essas tabelas não aparecem no resultado do diagrama.

---

### User Story 2 - Priorização das tabelas mais relevantes quando há muitas tabelas com dados (Priority: P2)

Como usuário de um banco com um grande número de tabelas com dados (mais que o limite de segurança do sistema), quero que as tabelas com maior quantidade estimada de registros sejam priorizadas na introspecção, para que as entidades mais significativas do meu domínio apareçam no diagrama mesmo quando nem todas as tabelas cabem no limite.

**Why this priority**: Só passa a importar depois que o filtro de relevância (User Story 1) já reduziu o universo de tabelas candidatas; ainda assim é necessário para bancos muito grandes, prevenindo que tabelas pouco relevantes “ocupem vaga” de tabelas mais importantes por mero acaso de ordenação alfabética.

**Independent Test**: Pode ser testado com um banco que tenha mais tabelas com registros que o limite de segurança do sistema e verificando que as tabelas selecionadas são as de maior quantidade estimada de registros, e que um aviso de truncamento é exibido.

**Acceptance Scenarios**:

1. **Given** um banco com mais tabelas relevantes (>10 registros estimados) do que o limite de segurança permite introspeccionar, **When** o schema é obtido, **Then** o sistema seleciona as tabelas com maior quantidade estimada de registros até o limite, e informa ao usuário que houve truncamento.
2. **Given** um banco com uma quantidade de tabelas relevantes dentro do limite de segurança, **When** o schema é obtido, **Then** todas as tabelas relevantes são introspeccionadas e nenhum aviso de truncamento é exibido.

---

### User Story 3 - Relacionamentos continuam corretos após o filtro (Priority: P1)

Como usuário, quero que colunas, chaves primárias e chaves estrangeiras das tabelas relevantes continuem sendo detectadas corretamente após a aplicação do novo filtro, para que o diagrama continue mostrando os relacionamentos entre as entidades exibidas.

**Why this priority**: É uma condição de não-regressão essencial — o valor da introspecção depende de PKs/FKs corretas; sem isso a mudança quebraria uma funcionalidade central já existente.

**Independent Test**: Pode ser testado conectando a um banco com tabelas relevantes que possuem chave primária e relacionamentos por chave estrangeira entre si, e verificando que essas informações aparecem corretamente no schema resultante.

**Acceptance Scenarios**:

1. **Given** uma tabela relevante com chave primária definida, **When** o schema é obtido, **Then** a coluna correspondente é marcada como chave primária no resultado.
2. **Given** duas tabelas relevantes relacionadas por chave estrangeira, **When** o schema é obtido, **Then** o relacionamento entre elas aparece no resultado do schema.

### Edge Cases

- Tabela com quantidade estimada de registros nula/desconhecida no `information_schema` (ex.: engines que não mantêm essa estatística) é tratada como não relevante e não aparece no resultado.
- Todas as tabelas do banco têm 10 registros estimados ou menos: o resultado tem zero tabelas, sem erro — o sistema retorna um schema válido com lista de tabelas vazia.
- Uma tabela relevante referencia (via chave estrangeira) uma tabela que ficou de fora do filtro por ter poucos registros: o relacionamento com a tabela ausente não é exibido, mas isso não gera erro na introspecção.
- A estimativa de registros (`TABLE_ROWS`) pode estar desatualizada em relação à quantidade real de linhas, especialmente em tabelas InnoDB; o sistema não deve ser tratado como fornecendo uma contagem exata.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST considerar relevante, para fins de descoberta e introspecção, apenas as tabelas do tipo tabela-base cuja quantidade estimada de registros seja maior que 10.
- **FR-002**: O sistema MUST usar a estatística de quantidade de registros já disponível nos metadados do banco (sem executar uma contagem exata por tabela) para decidir a relevância de cada tabela.
- **FR-003**: O sistema MUST manter o limite de segurança já existente de tabelas introspeccionadas por requisição, aplicado sobre o conjunto de tabelas relevantes.
- **FR-004**: Quando o número de tabelas relevantes exceder o limite de segurança, o sistema MUST priorizar as tabelas com maior quantidade estimada de registros e MUST continuar emitindo o aviso de truncamento já existente ao usuário.
- **FR-005**: O sistema MUST continuar obtendo colunas, chave primária e chaves estrangeiras apenas para as tabelas selecionadas como relevantes, preservando o comportamento atual dessas etapas.
- **FR-006**: O sistema MUST continuar respeitando o cancelamento da operação e o tempo limite de cada consulta ao banco, exatamente como no comportamento atual.
- **FR-007**: O sistema MUST tratar a quantidade estimada de registros como uma estimativa (não uma contagem exata), sem introduzir consultas adicionais de contagem por tabela para validar esse valor.
- **FR-008**: O sistema MUST retornar um schema válido (sem erro) quando nenhuma tabela do banco atender ao critério de relevância.

### Key Entities

- **Tabela candidata**: representa uma tabela-base do banco de origem antes da seleção final; possui nome, tipo, motor de armazenamento, comentário e uma quantidade estimada de registros usada somente durante a etapa de seleção das tabelas relevantes.
- **Tabela introspeccionada**: subconjunto das tabelas candidatas que atendeu ao critério de relevância e ao limite de segurança; é a que segue para as etapas de descoberta de colunas, chave primária e chaves estrangeiras, e que compõe o schema final exibido ao usuário.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Ao introspeccionar um banco com tabelas vazias ou quase vazias (≤10 registros estimados) misturadas com tabelas com dados, 100% das tabelas com ≤10 registros estimados são excluídas do diagrama resultante.
- **SC-002**: Ao introspeccionar um banco cujo número de tabelas com dados excede o limite de segurança, as tabelas exibidas no diagrama são exatamente as de maior quantidade estimada de registros, até o limite.
- **SC-003**: Para tabelas relevantes exibidas no diagrama, 100% das chaves primárias e chaves estrangeiras previamente detectadas continuam sendo detectadas corretamente após a mudança.
- **SC-004**: O tempo de resposta da introspecção para um banco típico não piora em relação ao comportamento atual, já que nenhuma consulta adicional por tabela é introduzida.

## Assumptions

- A estatística de quantidade de registros já disponível nos metadados do banco (equivalente ao `TABLE_ROWS` do `information_schema.TABLES` no MySQL) é considerada uma estimativa aceitável para decidir relevância, mesmo sabendo que pode estar desatualizada para tabelas InnoDB.
- O limiar de "mais de 10 registros estimados" é o critério inicial de relevância solicitado; não há necessidade de torná-lo configurável nesta mudança.
- Nenhuma heurística adicional baseada em nome de tabela (ex.: excluir `logs`, `migrations`, `cache`) é aplicada nesta mudança — apenas o critério de quantidade estimada de registros.
- O limite de segurança de tabelas e o tempo limite de comando já existentes permanecem com os mesmos valores atuais; esta mudança não os altera.
- Dados de quantidade de registros usados na seleção não precisam ser persistidos ou expostos no schema final retornado ao usuário.
