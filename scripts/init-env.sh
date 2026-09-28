#!/usr/bin/env sh
# Creates .env with random secrets for docker compose. Never overwrites an existing .env.
set -eu
cd "$(dirname "$0")/.."

if [ -f .env ]; then
  echo ".env ya existe; no se modificó."
  exit 0
fi

# Hex keeps secrets safe to embed in connection strings without escaping.
secret() { openssl rand -hex 24; }

umask 077
cat > .env <<ENV
POSTGRES_PASSWORD=$(secret)
TOKA_OWNER_PASSWORD=$(secret)
TOKA_APP_PASSWORD=$(secret)
API_KEY=$(secret)
WEB_PORT=5173
API_PORT=8080
SWAGGER_ENABLED=true
ENV

echo ".env creado con secretos aleatorios (permisos 600)."
