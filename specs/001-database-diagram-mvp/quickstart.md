# Guia de Validacao Rapida — Database Diagram MVP

Guia de validação ponta a ponta do MVP. Referências de detalhe:
[contratos da API](contracts/api.md) e [modelo de dados](data-model.md).
Este arquivo é um guia de execução/validação, não contém implementação.

## Pre-requisitos

- SDK .NET 8.
- Node.js 18+ (com npm).
- Banco MySQL acessível com um usuário de somente leitura (ex.: `readonly`) com
  permissão de leitura de `information_schema` e do database alvo.
- Um database de exemplo com ao menos 2 tabelas e uma FK para validar
  relacionamentos (ex.: `clientes` e `pedidos`, com `pedidos.cliente_id → clientes.id`).

## Execucao

### Backend

```sh
cd backend
dotnet restore
dotnet run --project DatabaseDiagram.Api
```

API fica disponível na porta de desenvolvimento do ASP.NET Core (log no
terminal informa a URL, tipicamente `http://localhost:5000`).

### Frontend

```sh
cd frontend
npm install
npm run dev
```

Aplicação fica disponível na URL informada pelo Vite (tipicamente
`http://localhost:5173`).

## Cenarios de validacao

### 1. Testar conexao invalida

```sh
curl -s -X POST http://localhost:5000/api/conexoes/teste \
  -H "Content-Type: application/json" \
  -d '{"provedor":"mysql","host":"localhost","porta":3307,"bancoDeDados":"erp","usuario":"readonly","senha":"errada"}'
```

**Esperado**: `200` (ou `400` para validação) com `"sucesso": false` e
`mensagem` em pt-BR. A resposta **não** contém senha ou connection string.

### 2. Testar conexao valida

```sh
curl -s -X POST http://localhost:5000/api/conexoes/teste \
  -H "Content-Type: application/json" \
  -d '{"provedor":"mysql","host":"localhost","porta":3306,"bancoDeDados":"erp","usuario":"readonly","senha":"segredo"}'
```

**Esperado**: `"sucesso": true`.

### 3. Carregar o schema

```sh
curl -s -X POST http://localhost:5000/api/esquema \
  -H "Content-Type: application/json" \
  -d '{"provedor":"mysql","host":"localhost","porta":3306,"bancoDeDados":"erp","usuario":"readonly","senha":"segredo"}'
```

**Esperado**: JSON com `nomeDoBanco`, `tabelas` (com `colunas`,
`chavePrimaria`, `chaveEstrangeira`, `autoIncremento`) e `relacionamentos`
conforme [api.md](contracts/api.md).

### 4. Diagrama completo no frontend

1. Abrir a aplicação e informar a conexão (provider MySQL, host, porta,
   database, usuário, senha).
2. Clicar em **Testar conexão** → deve confirmar sucesso.
3. Carregar o schema → **esperado**: diagrama com as tabelas, colunas,
   indicadores de PK (`🔑`) e FK (`🔗`), e edges ligando as FKs.

### 5. Navegacao e pesquisa

- Aplicar zoom in/out e fit view → diagrama responde.
- Mover uma tabela → a posição é alterada; os relacionamentos continuam.
- Digitar parte do nome de uma tabela na busca → resultado filtrado/destacado
  em < 1s.

### 6. Reorganizacao automatica

Clicar em **Organizar automaticamente** → **esperado**: tabelas reposicionadas
sem sobreposição excessiva, relacionamentos visíveis.

### 7. Ocultar / restaurar tabela

Ocultar uma tabela com relacionamentos → ela e seus edges somem.
Restaurar → tabela volta com seus relacionamentos.

### 8. Exploracao de relacionamentos

Selecionar uma tabela e solicitar **Mostrar relacionamentos** → **esperado**:
apenas a tabela selecionada e as tabelas diretamente relacionadas permanecem;
retorno ao diagrama completo funciona.

### 9. Somente leitura

- Confirmar que não existem funcionalidades de escrita (INSERT/UPDATE/DELETE/
  comandos SQL) no produto.
- Confirmar (ex.: com `SHOW PROCESSLIST` ou log do banco) que as únicas
  operações executadas são consultas de leitura de metadados.

## Validacao automatizada

- Backend: `dotnet test` dentro de `backend/` (unitários do provider/serviços).
- Frontend: `npm test` dentro de `frontend/` (componentes/fluxos).
- Testes de integração do provider MySQL exigem um banco real
  (variável de ambiente documentada na implementação, ex.: `MYSQL_TEST_*`).

## Criterios de sucesso do MVP

Referência dos critérios mensuráveis em [spec.md](spec.md) (SC-001 a SC-006).
Resumo operacional:

- Conectar e ver o diagrama em até 3 minutos a partir da tela inicial.
- Descobrir relacionamentos de uma tabela em menos de 30 segundos.
- Banco com centenas de tabelas: carrega e permanece navegável.
- Busca por nome em menos de 1 segundo.
- Nenhuma operação de escrita executada em 100% das sessões.