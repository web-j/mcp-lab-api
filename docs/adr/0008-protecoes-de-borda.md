# 0008. Proteções de borda do servidor público

- Estado: aceito
- Data: 2026-11-22

## Contexto

Um endpoint público com cadastro aberto atrai abuso: força bruta, requisições gigantes, varredura e uso excessivo por um único usuário.

## Decisão

- **nginx de borda:** TLS (Let's Encrypt), HSTS, `client_max_body_size 1m` e `limit_req` por IP (10 req/s, rajada de 40).
- **Host:** limite de corpo de 1 MB no Kestrel; rate limit por usuário (`sub`, 120 chamadas/min) e, sem token, por IP (30/min); `ForwardedHeaders` confiando no nginx (o container só é alcançável pela rede Docker).
- **CORS liberado para qualquer origem, sem credenciais.** Clientes MCP de navegador (MCP Inspector, apps web) precisam disso. A autenticação é por bearer token, nunca por cookie, então isso não abre CSRF. A checagem de `Origin` da especificação existe contra DNS rebinding em servidores locais e não se aplica aqui.
- **Página de login:** CSP restrita (`script-src 'self'`, `connect-src` só para o Supabase), `frame-ancestors 'none'`, supabase-js servido pelo próprio container (sem CDN numa página de senha), `noindex`.

## Consequências

- Os limites são configuráveis na seção `Edge` do `appsettings.json`.
- Para fechar o cadastro em caso de abuso, basta ligar `disable_signup` no projeto Supabase.
- Ainda falta captcha no cadastro (Cloudflare Turnstile é suportado pelo Supabase).
