#!/usr/bin/env bash
# Dev convenience only — not a migration framework. Applies every numbered
# script under migration/database/schema/, in order, against the given SQL
# Server instance. Each schema file is self-idempotent (DROP TABLE IF EXISTS
# then CREATE), so running this repeatedly is safe.
#
# Usage:
#   ONE77_SQL_HOST=localhost,1433 ONE77_SQL_USER=sa ONE77_SQL_PASSWORD=... \
#     ./apply_schema.sh <database-name>
#
# Or, when SQL Server is running in the "one77-sqlserver" Docker container
# used throughout this migration's development/testing (the default below),
# no environment variables are required beyond the password.

set -euo pipefail

DB_NAME="${1:?Usage: apply_schema.sh <database-name>}"
SQL_HOST="${ONE77_SQL_HOST:-localhost}"
SQL_USER="${ONE77_SQL_USER:-sa}"
SQL_PASSWORD="${ONE77_SQL_PASSWORD:?Set ONE77_SQL_PASSWORD before running this script}"
CONTAINER_NAME="${ONE77_SQL_CONTAINER:-one77-sqlserver}"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SCHEMA_DIR="$SCRIPT_DIR/../schema"

for f in "$SCHEMA_DIR"/[0-9][0-9][0-9]_*.sql; do
    echo "== Applying $(basename "$f") to database [$DB_NAME] =="
    docker cp "$f" "$CONTAINER_NAME:/tmp/$(basename "$f")"
    docker exec "$CONTAINER_NAME" /opt/mssql-tools18/bin/sqlcmd \
        -S "$SQL_HOST" -U "$SQL_USER" -P "$SQL_PASSWORD" -C \
        -d "$DB_NAME" -i "/tmp/$(basename "$f")"
done

echo "== Schema applied successfully =="
