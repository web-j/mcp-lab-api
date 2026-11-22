#!/bin/sh
# Gera o config.js a partir do ambiente do container. Só valores públicos: URL do projeto e chave publishable.
set -eu
: "${SUPABASE_URL:?defina SUPABASE_URL}"
: "${SUPABASE_PUBLISHABLE_KEY:?defina SUPABASE_PUBLISHABLE_KEY}"
MCP_URL="${MCP_URL:-https://mcp.agateo.com.br/mcp}"

cat > /usr/share/nginx/html/config.js <<JS
window.AGATEO_CONFIG = {
  supabaseUrl: "${SUPABASE_URL}",
  supabaseKey: "${SUPABASE_PUBLISHABLE_KEY}",
  mcpUrl: "${MCP_URL}"
};
JS

# connect-src da CSP precisa conhecer o projeto Supabase.
sed -i "s|__SUPABASE_URL__|${SUPABASE_URL}|g" /etc/nginx/conf.d/default.conf
