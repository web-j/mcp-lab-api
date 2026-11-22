# 0009. Deploy na VPS com Docker Compose

- Estado: aceito
- Data: 2026-11-22

## Contexto

O lab precisa estar público em `mcp.agateo.com.br`, separado do resto da VPS (ADS/Nuncie), e o repositório é público.

## Decisão

- Stack próprio em `/opt/agateo/mcp-lab` (compose `deploy/prod/docker-compose.yml`): `mcp-lab-postgres` (só na rede interna `mcplab`, volume próprio), `mcp-lab-api` e `mcp-lab-auth`. Nenhuma porta publicada no host.
- A borda é o nginx compartilhado que já atende a VPS (container `waha-nginx`, rede `waha`). O lab só acrescenta `mcp-lab.conf` e `mcp-lab.ssl.conf`, com um certificado `mcp-lab` para os dois nomes.
- A imagem do host é gerada pelo SDK (`dotnet publish -t:PublishContainer`, sem Dockerfile); a da página de login, por Dockerfile. As duas vão para o GHCR com as tags `prod` e o SHA do commit.
- Esteira `.github/workflows/deploy.yml`: push na `main` → testes → imagens em runner do GitHub → deploy por SSH com uma chave exclusiva do usuário `deploy`. Os secrets ficam no environment `production`, restrito à `main`.
- **Sem runner self-hosted:** num repositório público, um PR de fork poderia executar código na VPS.

## Consequências

- O deploy usa a tag do SHA, então dá para voltar uma versão rodando `scripts/deploy.sh <sha-anterior>` na VPS.
- O nginx de borda ainda mora em `/opt/agateo/waha`; quando ele for movido para um stack próprio, só mudam os caminhos no `sync-edge.sh` e no `init-tls.sh`.
- O Postgres não tem backup automático ainda.
