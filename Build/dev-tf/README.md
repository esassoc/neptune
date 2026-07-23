# Build/dev-tf — Neptune dev-environment infrastructure

Terraform + Azure DevOps pipeline that provisions the **dev** support resources
local devcontainers rely on. Separate, lighter stack from the deployed qa/prod
infra in the root `neptune.tf`. Mirrors wave-runup's WAVE-30 dev stack.

## What it creates (`Main.tf`)

- Resource group (`neptune-dev`) and an **RBAC** Key Vault
  (`neptune-keyvault-dev`) with soft-delete + purge protection.
- The dev blob storage account (`neptuneappdev`) **and its `files` container** —
  the app expects `files` to exist (uploads don't create containers).
- A user-assigned managed identity (`neptune-dev-identity`).
- Role assignments: pipeline SP → *Key Vault Secrets Officer*; dev identity →
  *Secrets User* + *Storage Blob Data Contributor*; dev AAD group →
  *Secrets User*.
- Seeded secrets: `AzureBlobStorageConnectionString` (always — derived from the
  storage account, so a re-apply heals key rotation) and, only when a value is
  supplied, `SendGridApiKey` / `AnthropicApiKey`.

## How it connects to the app

Set `KeyVaultName=neptune-keyvault-dev` in `.devcontainer/.developer.env` and
run `make auth` (`az login`). `Neptune.{API,ExternalAPI}` then pull these
secrets at runtime via `DefaultAzureCredential` — your `az login` identity reads
them because your AAD group has *Key Vault Secrets User*
(`devReaderGroupObjectId`).

Key Vault secret names map 1:1 to Neptune's flat PascalCase config keys. The
devcontainer DB is local, so `DatabaseConnectionString` is **not** seeded — the
devcontainer uses the local SQL Server via `appsecrets.json`.

## Before the first run

Supply these via an Azure DevOps variable group / library (never committed):
`azureSubscription`, `devReaderGroupObjectId`, and the optional `secret*`
values. **Verify** the tfstate target (account / container / resource group in
`dev-terraform.yml`) against Neptune's tfstate convention, and that the pipeline
SP holds **Key Vault Data Access Administrator** on the dev vault's resource
group (same manual RBAC prereq as the root stack). Then run the
`dev-terraform.yml` pipeline: **plan → approval → apply**.

## Cutover from the root stack (one-time, ordered)

`neptuneappdev` was originally created by the root `neptune.tf`
(`storageAccountDevApplicationName`) under the **root** tfstate. This commit
removes that resource + variable from `neptune.tf` and the `-var` from
`Build/azure-pipelines.yml`, so `neptuneappdev` moves under **this** dev stack.
Storage-account names are globally unique, so ordering matters and the old
account's blobs are treated as **throwaway dev data**:

1. **Run the main QA pipeline** (with this branch merged) — its Terraform apply
   **destroys** the old `neptuneappdev` in `neptune-qa` (the plan will show the
   destroy; that's expected).
2. **Run this `dev-terraform.yml` pipeline** — it recreates `neptuneappdev` in
   `neptune-dev` with the `files` container and seeds the vault secrets.
3. **Refresh the `storageAccountDevAccountKey`** Azure DevOps variable to the
   new account's key so `Build/restore-dev-blob.yml` (which restores prod blobs
   into `neptuneappdev` by name) keeps working. The account **name** is
   unchanged, so no YAML edit is needed there.
