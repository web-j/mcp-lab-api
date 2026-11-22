# mcp-platform Specification

## Purpose

Comportamento do servidor MCP visto por qualquer cliente (Claude, ChatGPT, Grok, Cursor...): transporte,
autorização, convenções das tools e as tools do próprio host. Decisões em `docs/adr/` (0002, 0003, 0005, 0007, 0008).

## Requirements

### Requirement: Endpoint MCP único

O servidor SHALL expor MCP somente por Streamable HTTP em `/mcp`, em modo sem sessão. Fora do MCP SHALL existir
apenas `/health`, `/alive` e os metadados OAuth; não SHALL existir endpoint REST de negócio.

#### Scenario: Cliente lista as tools

- **GIVEN** um cliente com token válido
- **WHEN** envia `tools/list` para `POST /mcp`
- **THEN** recebe as tools do host e de todos os módulos registrados

### Requirement: Autorização OAuth 2.1

O servidor SHALL agir como resource server do Supabase Auth do lab. Sem token SHALL responder 401 com
`WWW-Authenticate: Bearer resource_metadata=...`; os metadados SHALL apontar o authorization server e anunciar
os escopos `openid`, `email` e `offline_access`. Tokens SHALL ter emissor, assinatura (JWKS), `aud = authenticated`,
`role = authenticated` e `client_id` válidos.

#### Scenario: Primeira conexão sem token

- **WHEN** um cliente chama `POST /mcp` sem `Authorization`
- **THEN** recebe 401 com o endereço de `/.well-known/oauth-protected-resource/mcp`

#### Scenario: Sessão comum do Supabase sem client_id

- **GIVEN** um token do Supabase sem a claim `client_id`
- **WHEN** o cliente chama `POST /mcp`
- **THEN** recebe 403

### Requirement: Convenções das tools

Toda tool SHALL ter nome `modulo_verbo_objeto` com prefixo de módulo registrado (ou `system`), `Title`, descrição
com pelo menos 60 caracteres dizendo quando usar e quando não usar, as anotações `readOnlyHint`, `destructiveHint`,
`idempotentHint` e `openWorldHint` declaradas e `outputSchema`.

#### Scenario: Tool fora do padrão

- **GIVEN** uma tool sem anotação `destructiveHint`
- **WHEN** a suíte `McpLab.McpContractTests` roda
- **THEN** o teste falha citando a tool e a regra

### Requirement: Dados por usuário

Toda tool SHALL agir em nome do usuário do token (`sub`) e SHALL NOT receber o ID do usuário como parâmetro.

#### Scenario: Conta conectada

- **WHEN** o agente chama `system_get_current_user`
- **THEN** recebe o ID e o e-mail do dono do token e o `client_id` do app conectado

### Requirement: Status do servidor

`system_get_status` SHALL devolver nome, versão, ambiente, horário UTC e os módulos carregados (prefixo e descrição).

#### Scenario: Descobrir módulos

- **WHEN** o agente chama `system_get_status`
- **THEN** recebe a lista de módulos com prefixo e descrição

### Requirement: Limites de uso

O servidor SHALL limitar chamadas por usuário (120 por minuto) e, sem token, por IP (30 por minuto), respondendo 429
com `Retry-After`, e SHALL recusar corpo acima de 1 MB.

#### Scenario: Excesso de chamadas

- **GIVEN** um usuário que já fez 120 chamadas no último minuto
- **WHEN** faz mais uma
- **THEN** recebe 429 com `Retry-After`
