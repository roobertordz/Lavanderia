#!/usr/bin/env bash
#
# LaundryPOS - Script de actualización segura
#
# Qué hace:
#   1. Respalda la base de datos SQL Server (BACKUP DATABASE).
#   2. Descarga la última versión etiquetada (tag) desde el remoto de git.
#   3. Reconstruye y reinicia los contenedores api/web con la nueva versión.
#   4. Verifica que la API responda saludablemente (health check).
#   5. Si algo falla, revierte automáticamente al tag anterior y reconstruye.
#
# Uso:
#   ./scripts/update.sh              # actualiza al último tag disponible
#   ./scripts/update.sh v1.2.0       # actualiza a un tag específico
#
# Requisitos: git, docker, docker compose. Debe ejecutarse desde la raíz del
# repo (o se puede llamar desde cualquier lado, se autolocaliza).

set -euo pipefail

REPO_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_DIR"

# Load secrets from .env (gitignored) instead of hardcoding them here.
if [ -f "$REPO_DIR/.env" ]; then
    set -a
    # shellcheck disable=SC1091
    source "$REPO_DIR/.env"
    set +a
fi

DB_CONTAINER="laundrypos-db"
DB_USER="sa"
DB_PASSWORD="${SA_PASSWORD:?SA_PASSWORD no está definida (revisa el archivo .env)}"
DB_NAME="LaundryPOS"
BACKUP_DIR_IN_CONTAINER="/var/opt/mssql/backup"
# Por defecto asume que corre directamente en el host (localhost:5002, el
# puerto publicado). Cuando corre dentro del contenedor hermano lanzado por
# SystemController.ApplyUpdate, este viene sobreescrito vía "-e HEALTH_URL=..."
# con el nombre DNS interno del servicio (ej. http://laundrypos-api/...),
# porque desde ese contenedor "localhost" es él mismo, no el host.
HEALTH_URL="${HEALTH_URL:-http://localhost:5002/api/system/version}"
HEALTH_TIMEOUT_SECONDS=60

TARGET_TAG="${1:-}"

log() { echo "[update] $(date '+%Y-%m-%d %H:%M:%S') - $*"; }

fail() {
    log "ERROR: $*"
    exit 1
}

# ─── 0. Verificaciones previas ───────────────────────────────────────────
command -v git >/dev/null 2>&1 || fail "git no está instalado en este equipo."
command -v docker >/dev/null 2>&1 || fail "docker no está instalado en este equipo."

if ! git -C "$REPO_DIR" rev-parse --is-inside-work-tree >/dev/null 2>&1; then
    fail "Este directorio no es un repositorio git ($REPO_DIR)."
fi

CURRENT_TAG="$(git -C "$REPO_DIR" describe --tags --abbrev=0 2>/dev/null || echo 'sin-tag')"
log "Versión actual: $CURRENT_TAG"

# ─── 1. Respaldo de base de datos ────────────────────────────────────────
BACKUP_FILE="LaundryPOS_$(date '+%Y%m%d_%H%M%S').bak"
log "Respaldando base de datos en $BACKUP_FILE ..."
docker exec "$DB_CONTAINER" mkdir -p "$BACKUP_DIR_IN_CONTAINER" || true
docker exec "$DB_CONTAINER" /opt/mssql-tools18/bin/sqlcmd \
    -S localhost -U "$DB_USER" -P "$DB_PASSWORD" -C \
    -Q "BACKUP DATABASE [$DB_NAME] TO DISK = N'$BACKUP_DIR_IN_CONTAINER/$BACKUP_FILE' WITH INIT" \
    || fail "El respaldo de la base de datos falló. Se aborta la actualización sin tocar los contenedores."
log "Respaldo completado."

# ─── 2. Descargar la nueva versión ───────────────────────────────────────
log "Buscando actualizaciones (git fetch --tags) ..."
git -C "$REPO_DIR" fetch --tags --quiet || fail "No se pudo contactar al repositorio remoto."

if [ -z "$TARGET_TAG" ]; then
    TARGET_TAG="$(git -C "$REPO_DIR" tag --sort=-v:refname | head -n1)"
fi

if [ -z "$TARGET_TAG" ]; then
    fail "No hay ningún tag de versión disponible para actualizar."
fi

if [ "$TARGET_TAG" == "$CURRENT_TAG" ]; then
    log "Ya estás en la última versión ($CURRENT_TAG). Nada que hacer."
    exit 0
fi

log "Actualizando de $CURRENT_TAG a $TARGET_TAG ..."
git -C "$REPO_DIR" checkout --quiet "$TARGET_TAG" || fail "No se pudo cambiar al tag $TARGET_TAG."

# ─── 3. Reconstruir y reiniciar contenedores ─────────────────────────────
log "Reconstruyendo contenedores (api, web) ..."
if ! docker compose up -d --build api web; then
    log "El build/deploy falló. Revirtiendo a $CURRENT_TAG ..."
    git -C "$REPO_DIR" checkout --quiet "$CURRENT_TAG"
    docker compose up -d --build api web || fail "Falló también la reversión. Revisar manualmente."
    fail "Actualización abortada y revertida a $CURRENT_TAG."
fi

# ─── 4. Health check ─────────────────────────────────────────────────────
log "Verificando salud de la API en $HEALTH_URL ..."
elapsed=0
until curl -sf -o /dev/null "$HEALTH_URL"; do
    sleep 2
    elapsed=$((elapsed + 2))
    if [ "$elapsed" -ge "$HEALTH_TIMEOUT_SECONDS" ]; then
        log "La API no respondió saludable tras ${HEALTH_TIMEOUT_SECONDS}s. Revirtiendo a $CURRENT_TAG ..."
        git -C "$REPO_DIR" checkout --quiet "$CURRENT_TAG"
        docker compose up -d --build api web || fail "Falló también la reversión. Revisar manualmente."
        fail "Actualización abortada y revertida a $CURRENT_TAG (health check falló)."
    fi
done

log "✅ Actualización completada con éxito: $CURRENT_TAG → $TARGET_TAG"
log "Respaldo de base de datos disponible en el contenedor $DB_CONTAINER:$BACKUP_DIR_IN_CONTAINER/$BACKUP_FILE"
