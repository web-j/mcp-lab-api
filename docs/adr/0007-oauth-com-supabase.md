# 0007. OAuth 2.1 com Supabase Auth

- Estado: aceito
- Data: 2026-11-22

## Contexto

O servidor é público e precisa funcionar em qualquer cliente MCP (Claude, ChatGPT, Cursor, Grok...). Claude.ai e ChatGPT só aceitam servidores remotos com OAuth e registro automático de cliente. Escrever um servidor OAuth próprio é arriscado num servidor público e foge do foco do ensaio.

## Decisão

- O **Supabase Auth** do projeto dedicado `mcp-lab` (`eowxffcgonmbbsohedox`, separado da Nuncie) é o authorization server, com o servidor OAuth 2.1 e o registro dinâmico de clientes (DCR) ligados.
- O host é só **resource server**: valida o JWT pelo JWKS do Supabase (ES256) e publica o Protected Resource Metadata (RFC 9728) em `/.well-known/oauth-protected-resource/mcp`. Requisição sem token recebe 401 com `WWW-Authenticate` apontando para esses metadados.
- A tela de login, cadastro e consentimento é nossa: `src/McpLab.Auth`, servida em `https://auth.agateo.com.br` (a Site URL do projeto Supabase), com o caminho `/oauth/consent`.
- Login por e-mail e senha, com confirmação de e-mail obrigatória e envio pelo Resend (`no-reply@agateo.com.br`). Google e GitHub ficam para depois.
- O host exige a claim `client_id`, que só tokens emitidos pelo servidor OAuth têm. Uma sessão comum da página de login não acessa o MCP.

## Consequências

- **Sem audience por recurso:** o Supabase ignora o parâmetro `resource` (RFC 8707) e emite todo token com `aud = authenticated`. Um token do projeto vale para qualquer recurso que confie nele. Isso é aceitável só porque o projeto Supabase é exclusivo do lab; ele não deve ser reaproveitado por outro sistema.
- **Sem escopos por módulo:** o Supabase só oferece `openid`, `email`, `profile` e `phone`. A permissão por módulo, quando existir, fica no nosso código.
- Cada módulo filtra os dados pelo `ICurrentUser.Id` (claim `sub`).
- Com o DCR aberto, qualquer app consegue se registrar. Por isso a tela de consentimento mostra o nome do app e para onde ele volta, e falta uma página de "apps conectados" para a pessoa revogar acessos.
- O plano gratuito do Supabase pausa o projeto depois de cerca de 7 dias sem atividade, e aí o login para de funcionar até alguém reativar.
