# Deploy e operação

Decisões nos ADRs 0007 (auth), 0008 (proteções) e 0009 (deploy).

## Endereços

| Endereço | O quê |
|---|---|
| `https://mcp.agateo.com.br/mcp` | servidor MCP |
| `https://mcp.agateo.com.br/health` | saúde do host (inclui o Postgres) |
| `https://mcp.agateo.com.br/.well-known/oauth-protected-resource/mcp` | metadados OAuth |
| `https://auth.agateo.com.br` | login, cadastro e consentimento |

DNS: registros A de `mcp` e `auth` em `agateo.com.br` para `145.223.92.37`.

## Esteira

Push na `main` → `.github/workflows/deploy.yml`:

1. build + testes
2. imagens `ghcr.io/web-j/mcp-lab-api` e `ghcr.io/web-j/mcp-lab-auth` com as tags `prod` e `<sha>`
3. SSH como `deploy`: copia `deploy/prod/` para `/opt/agateo/mcp-lab`, regrava o `.env` e roda `scripts/deploy.sh <sha>`
4. smoke test público (health, metadados, 401 sem token)

Environment `production` (só a `main`):

| Tipo | Nome | Uso |
|---|---|---|
| secret | `VPS_SSH_KEY` | chave privada exclusiva do deploy (comentário `mcp-lab-deploy` no `authorized_keys` do `deploy`) |
| secret | `VPS_KNOWN_HOSTS` | host keys da VPS |
| secret | `POSTGRES_PASSWORD` | senha do Postgres do lab |
| variable | `VPS_HOST`, `SUPABASE_URL`, `SUPABASE_PUBLISHABLE_KEY`, `LETSENCRYPT_EMAIL` | |

## Na VPS

```
/opt/agateo/mcp-lab/
  docker-compose.yml   mcp-lab-postgres, mcp-lab-api, mcp-lab-auth
  .env                 gerado pela esteira (modo 600)
  nginx/               vhosts copiados para /opt/agateo/waha/nginx/live por scripts/sync-edge.sh
  scripts/             deploy.sh, init-tls.sh, sync-edge.sh
```

```bash
ssh agateo-vps 'cd /opt/agateo/mcp-lab && docker compose ps'
ssh agateo-vps 'cd /opt/agateo/mcp-lab && docker compose logs -f api'

# voltar para uma versão anterior
ssh agateo-vps 'cd /opt/agateo/mcp-lab && sudo -u deploy sh scripts/deploy.sh <sha> contato@agateo.com.br'
```

O certificado `mcp-lab` (os dois nomes) é emitido pelo certbot do stack da borda na primeira execução do
`deploy.sh` e renovado junto com os outros.

## Supabase (projeto `mcp-lab`)

- OAuth 2.1 server e DCR ligados; Site URL `https://auth.agateo.com.br`, consentimento em `/oauth/consent`.
- Redirects permitidos: `https://auth.agateo.com.br/**` e `http://localhost:5190/**`.
- E-mail e senha com confirmação; SMTP do Resend (`no-reply@agateo.com.br`, chave `supabase-mcp-lab-smtp`, só envio).
- Para fechar o cadastro em caso de abuso: ligar `disable_signup` (Authentication → Sign In / Providers).
- Plano gratuito: o projeto pausa depois de ~7 dias sem uso; reativar pelo painel.
