<!--
  SYNC IMPACT REPORT (rascunho de revisão — remover antes de commitar)
  --------------------------------------------------------------------
  Mudança de versão: (scaffold sem versão) → 1.0.0
  Tipo: adoção inicial — primeiro preenchimento real do scaffold.
  Princípios alterados: N/A (nenhum pré-existia)
  Seções adicionadas:
    - Princípios Centrais (I a V)
    - Restrições de Segurança e Performance
    - Workflow de Desenvolvimento
    - Governança
  Seções removidas: N/A
  TODOs adiados:
    - TODO(NOME_DOS_MODELOS): o PRD define modelos e chaves JSON em inglês
      (ex.: DatabaseSchema, databaseName, tableType). A convenção pt-BR deve
      ser conciliada na fase de especificação, definindo o mapeamento entre
      nomes internos (pt-BR) e o contrato JSON da API.
-->

# Constituição do Database Diagram

## Princípios Centrais

### I. Simplicidade Primeiro (NON-NEGOTIABLE)

NENHUMA abstração, interface, provider, projeto ou dependência pode ser criada
sem necessidade demonstrada no MVP ou em item explícito do roadmap. MANTER
apenas os contratos mínimos para suportar novos bancos: `IDatabaseSchemaProvider`
e o modelo comum de schema. NÃO criar camada de repositório, CQRS, mensageria,
autenticação ou qualquer indireção que o problema atual não exija.

Razão: desejamos um projeto simples e fácil de manter. Toda nova camada ou
abstração DEVE ser justificada por escrito antes de entrar no código.

### II. Código em Português (pt-BR)

TODOS os identificadores escritos em pt-BR: variáveis, funções, classes,
propriedades, métodos, parâmetros, DTOs, modelos, pastas, arquivos e endpoints.
Mensagens de erro e de log, comentários, mensagens de commit e documentação em
pt-BR. Exceções: palavras reservadas e convenções impostas pela linguagem ou por
bibliotecas, além de valores técnicos de banco que DEVEM conservar o nome
original.

Razão: facilitar a manutenção do projeto pelo time brasileiro.

### III. Somente Leitura (Read-only)

A aplicação NUNCA executa INSERT, UPDATE, DELETE, CREATE, ALTER, DROP, TRUNCATE
ou SQL arbitrário. Cada provider executa SOMENTE consultas de leitura
previamente definidas para obter metadados. Credenciais vivem SOMENTE em memória
durante a sessão: NUNCA em banco da aplicação, localStorage, logs, arquivos de
configuração ou respostas da API. Mensagens de erro NUNCA expõem senha,
connection string completa ou detalhes internos.

Razão: a ferramenta é de exploração, não de administração, e as credenciais são
informações sensíveis.

### IV. Provedores Isolados, Contrato Comum

TODA particularidade de um banco (consultas, regras, tipos) vive DENTRO do seu
provider. Adicionar um novo banco NÃO pode exigir alterações em controllers,
serviços comuns, modelo comum ou frontend. O MVP suporta SOMENTE MySQL; outros
bancos entram apenas via nova implementação de `IDatabaseSchemaProvider` e
registro na factory.

Razão: extensibilidade sem acoplar a lógica principal a um banco específico.

### V. Visual First

O diagrama é o produto principal; QUALQUER funcionalidade DEVE melhorar a
exploração visual do schema. Um banco grande (centenas de tabelas) NÃO pode
tornar o diagrama inutilizável: carregamento e navegação DEVEM permanecer
viáveis. O foco está em responder rápido à pergunta "o que se relaciona com
esta tabela?".

Razão: a métrica principal do produto é o tempo para entender os
relacionamentos de um banco existente.

## Restrições de Segurança e Performance

O MVP DEVE restringir o acesso ao banco à leitura de metadados
(`information_schema` e metadados do database selecionado). Mensagens de erro
apresentadas ao usuário DEVEM ser genéricas e orientadas à ação, sem expor
segredos. Funcionalidades não previstas no MVP — autenticação, persistência de
conexões, execução de SQL arbitrário — permanecem fora do escopo.

A aplicação DEVE carregar schemas com centenas de tabelas e o diagrama DEVE
continuar navegável nessa condição.

## Workflow de Desenvolvimento

Seguir o primeiro milestone: conectar a um MySQL e renderizar automaticamente um
diagrama com tabelas, colunas, PKs e FKs. Funcionalidades adicionais entram
apenas depois que esse fluxo funcionar de ponta a ponta.

Cada novo banco DEVE chegar acompanhado de testes específicos de introspecção e
dos ajustes da tela de conexão. A estrutura de pastas do backend DEVE permanecer
a do PRD: `Controllers`, `Models`, `DTOs`, `Services`, `Providers` e
`Infrastructure`.

Mensagens de commit e de interface em pt-BR. Revisões DEVEM verificar a
conformidade com esta constituição (simplicidade, pt-BR e somente leitura).

## Governança

Esta constituição prevalece sobre demais práticas e convenções do projeto.
Emendas exigem atualização deste documento, justificativa e incremento de
versão conforme as regras abaixo:

- MAJOR: remoção ou redefinição de princípio.
- MINOR: novo princípio ou seção adicionada.
- PATCH: esclarecimentos e correções de redação.

Revisões de conformidade DEVEM acontecer nas revisões de pull request e nas
atividades de especificação e planejamento. Complexidade DEVE ser desafiada em
qualquer revisão.

**Versão**: 1.0.0 | **Ratificado**: 2026-09-15 | **Última emenda**: 2026-09-15