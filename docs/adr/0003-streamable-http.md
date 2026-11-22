# 0003. Transporte Streamable HTTP

- Estado: aceito
- Data: 2026-11-22

## Contexto

O servidor precisa ser acessado remotamente por clientes diferentes e, depois, com autenticação.

## Decisão

Streamable HTTP em `/mcp` com `ModelContextProtocol.AspNetCore`. Autenticação OAuth 2.1, com o servidor como resource server (detalhes no ADR 0007). Stdio não é suportado por enquanto.

## Consequências

- O SDK 2.4 usa modo stateless por padrão, o que facilita escalar horizontalmente. Elicitation e sampling exigem sessão; a fase 3 decide se o modo com sessão será ligado.
- Em desenvolvimento a autenticação também é obrigatória: os clientes fazem o mesmo fluxo OAuth contra o Supabase do lab.
