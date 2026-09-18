# Pesquisa — Configuração e Seleção de Provedores de IA no Assistente

**Etapa**: Fase 0 do `/speckit.plan` (feature 005) — resolve os desconhecidos
da especificação antes do design.

## 1. Armazenamento dos tokens (FR-015 — marcador da especificação)

**Pergunta**: os tokens configurados no modal devem sobreviver entre execuções
da aplicação ou valem apenas na sessão atual?

**Decision**: Somente em memória durante a sessão. O frontend mantém o mapa
`provedor → token` em estado React; o token é enviado no corpo de cada
`POST /api/assistente/consultas` (fluxo atual) e descartado ao fechar a
aplicação. Sem persistência em banco, arquivos, `localStorage` ou logs e **sem
novos endpoints/armazenamento de segredos no backend**.

**Rationale**: a constituição do projeto (Princípio III — Somente Leitura) é
categórica: "Credenciais vivem SOMENTE em memória durante a sessão: NUNCA em
banco da aplicação, localStorage, logs, arquivos de configuração ou respostas
da API." O marcador da FR-015 foi aberto exatamente por essa tensão entre a
"área de configurações de tokens" pedida e a regra de credenciais efêmeras. A
constituição prevalece (Governança: prevalece sobre demais práticas e
convenções). A arquitetura atual já segue esse modelo — a chave só existia no
campo de senha do `ConfiguracaoDoAssistente` e viajava por requisição.

**Alternatives considered**:

- Persistir tokens criptografados localmente para reuso entre execuções —
  rejeitado: exige infraestrutura de criptografia, novo serviço e emenda da
  constituição (MAJOR). Custo alto sem benefício para usuário único local.
- Persistir apenas no contexto da conversa — rejeitado: viola o princípio III
  da mesma forma, com mais complexidade e nenhuma vantagem real de UX.
- Teste de conexão do token (assumption da spec) — **adiado** (opcional no
  MVP, GATE I): seria um endpoint novo de validação; a validação primária é a
  não-vacância e o erro de autenticação já tratado pelo fluxo do chat (502
  genérico orientado à ação). Mantido fora do escopo para simplificar.

## 2. Semântica da seleção de provedor no chat

**Pergunta**: a seleção é por mensagem, por sessão ou por conversa?

**Decision**: A seleção de provedor pertence à **conversa**. O campo
`provedorDeIaSelecionado` é gravado na conversa na primeira troca realizada com
aquele provedor e devolvido por `GET /api/conversas/{id}`; ao reabrir, o
frontend reaplica esse provedor no seletor. Trocar no meio da conversa atualiza
o campo para as próximas mensagens e preserva o histórico.

**Rationale**: alinha com a feature 003 (conversa como unidade persistida e
navegável) e com FR-012 da spec. Seleção por mensagem seria granular demais
para a UX de "ao abrir a conversa, ela continua como estava"; seleção global por
sessão impediria que conversas distintas usassem provedores distintos.

**Alternatives considered**: seleção global de sessão (rejeitada — perde
preferência por conversa); seleção por mensagem (descartada — complexa para o
badge e sem ganho percebido).

## 3. Provedor padrão

**Pergunta**: qual provedor usar ao abrir o chat sem conversa/entre conversas?

**Decision**: o primeiro provedor configurado na sessão assume o papel de
padrão; alterável a qualquer momento no modal. Conversas novas e conversas sem
provedor registrado usam o padrão. Modo sem provedor configurado: o enviar é
desabilitado e a interface orienta a configurar pelo modal.

**Rationale**: regra simples, previsível e sem estado extra de "default"
persistido (tudo na sessão, coerente com o GATE III).

## 4. Escalabilidade dos badges

**Pergunta**: como o componente se comporta quando o número de provedores cresce?

**Decision**: contêiner flexível com `flex-wrap`/`overflow-x` no rodapé do
chat; cada provedor é um badge (`<button>`) com `aria-pressed` para o
selecionado. Meta mensurável: 8 provedores configurados não escondem o
`textarea` nem o botão Enviar (SC-005).

**Rationale**: badges são o padrão pedido na spec ("pequenos badges (botões)"
com crescimento); flex-wrap mantém acessibilidade de teclado (botões) sem
depender de dropdown ou overflow escondido.

**Alternatives considered**: menu suspenso condensado (rejeitado — a spec pede
seleção visível e direta "por ali mesmo"); tooltip/overflow com "+N" (adiado —
complexidade desnecessária para o volume atual de provedores).

## 5. Dependências e integrações

- **D1 — Fábrica de provedores (`FabricaDeProvedoresDeIa`)**: fonte de verdade
  dos provedores suportados (`GET /api/assistente/provedores`). Novo provedor
  entra via fábrica + DI, sem alterar página/badges/modal (GATE IV).
- **D2 — Persistência de conversas (feature 003)**: `ServicoDeConversas` +
  `Conversa` + contratos. Estende com `provedorDeIaSelecionado`
  (backward-compatible, sem bump de formato obrigatório).
- **D3 — Contrato estruturado da resposta (feature 002)**: intocado; o chat e o
  `CartaoDeRespostaDeConsulta` não mudam.
- **D4 — Frontend atual**: `ConfiguracaoDoAssistente` (seletor `select` + campo
  de chave inline) é substituído pelo modal + badges; `PaginaDoAssistente`
  orquestra o novo estado de sessão; testes existentes são atualizados.

## Consolidado

| # | Unknown | Decision | Rationale resumido |
|---|---------|----------|--------------------|
| 1 | Token FR-015 | Somente memória na sessão | Constituição III (não-negociável) |
| 2 | Seleção de provedor | Por conversa (campo novo, restaurado) | Coerente com 003/FR-012 |
| 3 | Provedor padrão | 1º configurado; alterável no modal | Simples, previsível, em sessão |
| 4 | Badges | flex-wrap/overflow; até 8 medidos | Spec pede badges; SC-005 |
| 5 | Deps | Fábrica + 003 + 002 (intacto) | Sem camadas novas (GATE I) |