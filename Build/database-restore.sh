#!/usr/bin/env bash
# Restores a database from a BACPAC file into the devcontainer SQL Server.
# Usage: database-restore.sh [path-to-bacpac]
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

DB_SERVER="${DB_SERVER:-neptune.db}"
DB_NAME="${DB_NAME:-NeptuneDB}"
DB_PASSWORD="${DB_PASSWORD:-DevPassword#1}"

BACPAC="${1:-$SCRIPT_DIR/temp/${DB_NAME}.bacpac}"

if [ ! -f "$BACPAC" ]; then
    echo "ERROR: BACPAC not found at $BACPAC (run 'make download' first)"
    exit 1
fi

echo "==> Dropping database $DB_NAME (if exists)..."
sqlcmd -S "$DB_SERVER" -U sa -P "$DB_PASSWORD" -C -Q "
IF EXISTS (SELECT * FROM sys.databases WHERE name = '$DB_NAME')
BEGIN
    ALTER DATABASE [$DB_NAME] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [$DB_NAME];
END
"

echo "==> Importing BACPAC from $BACPAC..."
CONNECTION_STRING="Server=$DB_SERVER;Database=$DB_NAME;User Id=sa;Password=$DB_PASSWORD;TrustServerCertificate=True;"
sqlpackage \
    /Action:Import \
    /SourceFile:"$BACPAC" \
    /TargetConnectionString:"$CONNECTION_STRING"

echo "==> Database restore complete."
