# Quickstart — Configuração e Seleção de Provedores de IA no Assistente

Guia de validação ponta a ponta da feature 005. Assume API e frontend em
execução conforme o README (backend `localhost:5000`, frontend `localhost:5173`).

## Pré-requisitos

- API rodando: `cd backend && dotnet run --project DatabaseDiagram.Api`
- Frontend rodando: `cd frontend && npm run dev`
- Banco MySQL com schema carregado (para validar o envio com contexto)
- Rede com acesso aos provedores de IA (openai / google-ai-studio / claude) e
  respectivos tokens válidos (opcional para as etapas 1–3)

## Testes automatizados

```sh
# Backend (xUnit)
cd backend && dotnet test

# Frontend (Vitest)
cd frontend && npm test

# Frontend (lint + build)
cd frontend && npm run lint && npm run build
```

Cenários cobertos por testes: persistência/restauração do `provedorDeIa` da
conversa (backward-compatible), seletor de badges (seleção, estado, escala),
modal (adicionar/editar/remover token, mascaramento, padrão) e fluxo do chat.

## Cenários manuais de validação

### C1. Configurar um provedor pelo modal (FR-002/003/007)

1. Abra o Assistente e clique no botão **Configurações** da barra superior.
2. No modal, adicione o provedor `openai`, informe um token e salve.
3. Confirme que: o modal mostra o token como **configurado** (mascarado), nunca
   por completo; o badge **OpenAI** aparece no rodapé do chat; e o provedor
   passa a ser o padrão (primeiro configurado).

**Esperado**: provedor configurado sem recarregar a página.

### C2. Trocar de provedor no rodapé do chat (FR-008/009/010/013)

1. Com 2+ provedores configurados na sessão, clique no badge de outro provedor,
   sem abrir configurações.
2. Confirme que o badge selecionado ganha destaque visual e o `aria-pressed`.
3. Envie uma mensagem: `POST /api/assistente/consultas` deve ter sido chamado
   com `provedorDeIa` igual ao badge selecionado.

**Esperado**: troca instantânea por clique; envio usa o provedor selecionado.

### C3. Escala dos badges (FR-011/SC-005)

1. Configure até 8 provedores na sessão.
2. Confirme que todos permanecem clicáveis e que o `textarea` e o botão **Enviar**
   continuam visíveis e utilizáveis (wrapping/overflow, sem sobreposição).

**Esperado**: rodapé íntegro com 8 provedores.

### C4. Provedor por conversa, restaurado ao abrir (FR-012/SC-004)

1. Na conversa A, selecione `openai` no badge e envie uma mensagem.
2. Inicie a conversa B, selecione `claude` e envie outra mensagem.
3. Volte para a conversa A pela barra lateral: o badge ativo deve ser `openai`.
4. Abra a conversa B: o badge ativo deve ser `claude`.

**Esperado**: provedor restaurado por conversa em 100% dos casos.

### C5. Novo provedor sem token e erro orientado (FR-014/SC-007)

1. Adicione no modal um provedor **sem** digitar token (ou remova o token de um
   existente) e tente enviar com ele selecionado.
2. Confirme que o envio é bloqueado e a interface orienta a abrir Configurações.

**Esperado**: bloqueio + orientação, sem quebrar o chat.

### C6. Privacidade do token (FR-007/SC-003)

1. Envie uma mensagem com um token inválido.
2. Confirme que a mensagem de erro é genérica (`502`/`Não foi possível gerar...`),
   sem trecho do token.
3. Inspecione `dados/conversas/*.json`: nenhum token ou segredo em claro;
   somente o `provedorDeIaSelecionado`.

**Esperado**: token nunca exposto em tela, erro ou arquivo persistido.

### C7. Conversa antiga sem provedor (backward-compatible)

1. Com uma conversa persistida de versão anterior (sem `provedorDeIa`), abra-a
   pela barra lateral.
2. Confirme que o badge ativo usa o **provedor padrão** da sessão e o histórico
   carrega normalmente.

**Esperado**: nenhuma quebra ao ler conversas legadas.

## Referências

- Contratos: `contracts/api.md` (mudança em `GET /api/conversas/{id}`).
- Modelo: `data-model.md` (campo `provedorDeIaSelecionado`, `ConfiguracaoDeSessao`).
- Spec: `spec.md` (FR-001 a FR-018, SC-001 a SC-007).