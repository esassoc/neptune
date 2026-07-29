#!/usr/bin/env bash
# Downloads the latest BACPAC from Azure Blob Storage.
# Requires: az login (run `make auth` first)
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

DB_NAME="${DB_NAME:-NeptuneDB}"
CONTAINER_NAME="${AZ_CONTAINER_NAME:-prod-backup}"
STORAGE_ACCOUNT="${AZ_STORAGE_ACCOUNT:-}"

BLOB_NAME="${DB_NAME}.bacpac"
DEST_DIR="$SCRIPT_DIR/temp"
DEST_FILE="$DEST_DIR/$BLOB_NAME"

mkdir -p "$DEST_DIR"

if ! command -v az &>/dev/null; then
    echo "ERROR: Azure CLI (az) is not installed."
    exit 1
fi

echo "==> Checking Azure login..."
if ! az account show &>/dev/null; then
    echo "ERROR: Not logged in to Azure. Run 'make auth' first."
    exit 1
fi

echo "==> Downloading $BLOB_NAME from container '$CONTAINER_NAME'..."
AZ_ARGS=(
    storage blob download
    --container-name "$CONTAINER_NAME"
    --name "$BLOB_NAME"
    --file "$DEST_FILE"
    --auth-mode login
    --overwrite
)

if [ -n "$STORAGE_ACCOUNT" ]; then
    AZ_ARGS+=(--account-name "$STORAGE_ACCOUNT")
fi

az "${AZ_ARGS[@]}"

echo "==> Downloaded to $DEST_FILE"
