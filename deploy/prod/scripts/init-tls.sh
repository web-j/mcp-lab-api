#!/bin/sh
# Emite o certificado de mcp.agateo.com.br + auth.agateo.com.br pelo certbot do stack da borda, uma vez.
# O deploy roda este script quando o certificado não existe; depois é no-op.
# Os registros A precisam apontar para esta VPS.
# Uso: ./scripts/init-tls.sh voce@exemplo.com
set -eu

EMAIL="${1:?e-mail para o Let's Encrypt}"
ROOT="$(CDPATH= cd -- "$(dirname "$0")/.." && pwd)"

if docker exec waha-nginx test -f /etc/letsencrypt/live/mcp-lab/fullchain.pem; then
  echo "certificado mcp-lab já existe"
  exit 0
fi

sh "$ROOT/scripts/sync-edge.sh"

docker compose -f /opt/agateo/waha/docker-compose.yml --project-directory /opt/agateo/waha run --rm --entrypoint certbot certbot \
  certonly --webroot -w /var/www/certbot \
  -d mcp.agateo.com.br -d auth.agateo.com.br \
  --agree-tos --non-interactive -m "$EMAIL" \
  --cert-name mcp-lab

sh "$ROOT/scripts/sync-edge.sh"
