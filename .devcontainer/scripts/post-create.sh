#!/usr/bin/env bash
set -euo pipefail

WORKSPACE="/workspace"

echo "==> Restoring .NET packages..."
cd "$WORKSPACE"
dotnet restore Neptune.sln

echo "==> Installing .NET tools..."
dotnet tool install --global microsoft.sqlpackage 2>/dev/null || dotnet tool update --global microsoft.sqlpackage
dotnet tool install --global dotnet-ef 2>/dev/null || dotnet tool update --global dotnet-ef

echo "==> Installing frontend dependencies..."
cd "$WORKSPACE/Neptune.Web"
npm ci

echo "==> Disabling Angular CLI analytics/autocomplete prompts..."
npx --yes ng analytics disable --global 2>/dev/null || true
npx --yes ng config --global cli.completion.prompted true 2>/dev/null || true

echo "==> Generating appsecrets.json for devcontainer..."
# Local-only values. Real SendGrid/Anthropic/Blob come from the dev Key Vault
# (KeyVaultName) or a .developer.env override; the API reads the local SQL
# Server directly (no passwordless AAD in the devcontainer).
cat > "$WORKSPACE/Neptune.API/appsecrets.json" <<SECRETS
{
  "DatabaseConnectionString": "Data Source=${DB_SERVER};Initial Catalog=${DB_NAME};Persist Security Info=True;User ID=sa;Password=${DB_PASSWORD};Encrypt=False;TrustServerCertificate=True;",
  "HangfireUserName": "HangfireAdmin",
  "HangfirePassword": "password#1",
  "SendGridApiKey": "${SENDGRID_API_KEY:-not-a-real-key}",
  "AzureBlobStorageConnectionString": "${AZURE_BLOB_STORAGE_CONNECTION_STRING:-}",
  "AnthropicApiKey": "${ANTHROPIC_API_KEY:-not-a-real-key}",
  "ClaudeModelId": "claude-sonnet-4-6"
}
SECRETS
echo "    Generated appsecrets.json with devcontainer defaults."

echo "==> Installing language servers for Claude Code LSP..."
npm install -g typescript-language-server typescript

echo ""
echo "================================================"
echo "  Neptune devcontainer ready!"
echo "================================================"
echo ""
echo "  FIRST RUN: the database is empty until you deploy the schema —"
echo "    make build scaffold                     (schema + EF entities)"
echo "    make download restore build scaffold    (with a QA data restore; needs az login)"
echo ""
echo "  Run 'make help' to see available commands."
echo "  Run 'make start' to run the API and web dev server."
echo ""
echo "  Run 'make auth' once to sign in to Azure (az) + GitHub (gh)."
echo "    Auth persists in shared volumes, so this is one-time per host."
echo "    Needed for: 'make download' and Key Vault (KeyVaultName)."
echo ""
echo "  Run 'claude' to start Claude Code (first run opens browser auth)."
echo ""
