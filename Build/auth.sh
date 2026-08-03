#!/usr/bin/env bash
# Container auth bootstrap. Run once per host to sign in to:
#   - Azure CLI  (az) — tenant-scoped; used by `make download` and, when
#                       KeyVaultName is set, by the app's Key Vault config.
#   - GitHub CLI (gh, plus the git credential helper via `gh auth setup-git`)
#
# Auth state for claude / az / gh lives in SHARED named volumes
# (neptune-claude-config / neptune-azure-config / neptune-gh-config, see
# .devcontainer/docker-compose.yml), so ONE run seeds every current and future
# devcontainer (main + all worktrees) on this host and survives rebuilds.
# To force a full sign-out:
#   docker volume rm neptune-claude-config neptune-azure-config neptune-gh-config
#
# Claude Code login is intentionally NOT here — `claude` has no non-interactive
# login subcommand. Run `claude` once and complete the browser flow separately.
set -euo pipefail

echo "==> One-time auth bootstrap (az + gh). Two browser flows."
echo "    VS Code forwards the container's OAuth callback ports to your host,"
echo "    so this works from a VS Code devcontainer terminal."
echo ""

# Pass --tenant when AZURE_TENANT_ID is set (in .developer.env / .env). Without
# it, `az login` enumerates every tenant the user can access, which trips
# Conditional-Access policies on unrelated tenants and often hides the intended
# subscription.
az_tenant_args=()
if [ -n "${AZURE_TENANT_ID:-}" ]; then
    az_tenant_args+=(--tenant "$AZURE_TENANT_ID")
fi

echo "==> 1/2 Azure CLI..."
az login "${az_tenant_args[@]}"

if [ -n "${AZURE_SUBSCRIPTION_ID:-}" ]; then
    echo ""
    echo "==> Pinning default subscription to \$AZURE_SUBSCRIPTION_ID..."
    az account set --subscription "$AZURE_SUBSCRIPTION_ID"
fi

echo ""
echo "==> 2/2 GitHub CLI..."
gh auth login --hostname github.com --web --git-protocol https

echo ""
echo "==> Configuring git credential helper (so 'git push' to GitHub works)..."
gh auth setup-git

echo ""
echo "================================================"
echo "  Auth complete. Sanity:"
echo "================================================"
echo ""
az account show --query "{user:user.name, subscription:name, tenant:tenantId}" -o table 2>/dev/null \
    || echo "  az: not logged in"
echo ""
gh auth status 2>&1 | grep -E "(Logged in|Active account|Token scopes)" \
    || echo "  gh: not logged in"
echo ""
echo "  Next: run 'claude' once and complete the browser auth, then /quit."
echo "        (Shared volume persists it across worktrees + rebuilds.)"
echo "================================================"
