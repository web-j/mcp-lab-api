#!/bin/sh
# Roda na VPS, chamado pela esteira (ou à mão): baixa as imagens, sobe o stack, garante TLS e vhosts.
# Uso: ./scripts/deploy.sh <tag-da-imagem> <email-letsencrypt>
set -eu

TAG="${1:?tag da imagem}"
EMAIL="${2:?e-mail para o Let's Encrypt}"
ROOT="$(CDPATH= cd -- "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

sed -i "s/^IMAGE_TAG=.*/IMAGE_TAG=$TAG/" .env

docker compose pull api auth
docker compose up -d --remove-orphans

sh scripts/init-tls.sh "$EMAIL"
sh scripts/sync-edge.sh

# Espera o host responder pela rede interna antes de declarar sucesso.
for i in $(seq 1 30); do
  if docker run --rm --network waha curlimages/curl:8.11.1 -sf http://mcp-lab-api:8080/health >/dev/null 2>&1; then
    echo "mcp-lab-api saudável"
    exit 0
  fi
  sleep 2
done

echo "mcp-lab-api não ficou saudável" >&2
docker compose logs --tail 80 api >&2
exit 1
