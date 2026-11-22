#!/bin/sh
# Copia os vhosts do mcp-lab para o nginx de borda compartilhado e recarrega.
# O vhost HTTPS só entra depois que o certificado existe.
set -eu

EDGE_LIVE=/opt/agateo/waha/nginx/live
SRC="$(CDPATH= cd -- "$(dirname "$0")/.." && pwd)/nginx"

cp "$SRC/mcp-lab.conf" "$EDGE_LIVE/mcp-lab.conf"
if docker exec waha-nginx test -f /etc/letsencrypt/live/mcp-lab/fullchain.pem; then
  cp "$SRC/mcp-lab.ssl.conf" "$EDGE_LIVE/mcp-lab.ssl.conf"
else
  rm -f "$EDGE_LIVE/mcp-lab.ssl.conf"
fi

docker exec waha-nginx nginx -t
docker exec waha-nginx nginx -s reload
