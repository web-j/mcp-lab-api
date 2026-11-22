# Página de login, cadastro e consentimento OAuth do auth.agateo.com.br (ADR 0007).
# O supabase-js é servido pelo próprio container (sem CDN de terceiros numa página de senha).

FROM node:22-alpine AS vendor
ARG SUPABASE_JS_VERSION=2.119.0
WORKDIR /vendor
RUN npm pack @supabase/supabase-js@${SUPABASE_JS_VERSION} --silent \
 && tar xzf supabase-supabase-js-${SUPABASE_JS_VERSION}.tgz \
 && cp package/dist/umd/supabase.js /vendor/supabase.js

FROM nginx:1.29-alpine
COPY nginx/default.conf /etc/nginx/conf.d/default.conf
COPY nginx/40-write-config.sh /docker-entrypoint.d/40-write-config.sh
COPY public/ /usr/share/nginx/html/
COPY --from=vendor /vendor/supabase.js /usr/share/nginx/html/vendor/supabase.js
RUN chmod +x /docker-entrypoint.d/40-write-config.sh
EXPOSE 80
