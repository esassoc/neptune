# =============================================================================
# Neptune dev-environment infrastructure (Terraform)
# =============================================================================
# Stands up the DEV support resources that local devcontainers use — most
# importantly the dev Key Vault that the app reads at runtime when
# `KeyVaultName` is set (see .devcontainer/.env + Neptune.{API,ExternalAPI}
# Program.cs AddAzureKeyVault). This is a SEPARATE, lighter stack from the
# deployed qa/prod infra in the root neptune.tf. Mirrors wave-runup WAVE-30.
#
# Auth model: RBAC (enable_rbac_authorization). A developer's `az login`
# identity reads secrets via DefaultAzureCredential once their AAD group has the
# "Key Vault Secrets User" role (var.devReaderGroupObjectId below).
#
# Dev blob storage is OWNED here (neptuneappdev + its `files` container). The
# same NPT-1112 change removes it from the root neptune.tf
# (storageAccountDevApplicationName), so one account never sits under two
# Terraform states. This is a globally-unique-name move — run the root apply
# (destroys the old account) BEFORE this stack's apply (recreates it). See README.
# =============================================================================

variable "keyVaultName" {
  type        = string
  description = "Dev Key Vault name, e.g. neptune-keyvault-dev. Must be globally unique."
}

variable "resourceGroupName" {
  type        = string
  description = "Dev resource group for the vault + storage + identity, e.g. neptune-dev."
}

variable "storageAccountName" {
  type        = string
  description = "Dev blob storage account, e.g. neptuneappdev. 3-24 lowercase alphanumerics, globally unique."
}

variable "team" {
  type = string
}

variable "projectNumber" {
  type = string
}

# AAD security group whose members (the dev team) get read access to the dev
# vault via `az login` + DefaultAzureCredential. Neptune shares the h2o team, so
# this is typically the H2O QA group object id. Leave "" to skip the group grant
# (individual devs can be granted Key Vault Secrets User out of band).
variable "devReaderGroupObjectId" {
  type    = string
  default = ""
}

# --- Seeded secrets ----------------------------------------------------------
# Neptune app secrets pulled from the vault at runtime. Names map 1:1 onto
# Neptune's flat PascalCase config keys (see NeptuneKeyVaultSecretManager).
# AzureBlobStorageConnectionString is derived from the dev storage account
# created below, not passed in. The devcontainer DB is local, so
# DatabaseConnectionString is intentionally NOT seeded here.
variable "secretSendGridApiKey" {
  type      = string
  sensitive = true
  default   = ""
}

variable "secretAnthropicApiKey" {
  type      = string
  sensitive = true
  default   = ""
}

terraform {
  required_version = ">= 1.1"
  backend "azurerm" {
    container_name = "terraform"
    # DISTINCT key from the root neptune.tf state ("terraform.tfstate") so this
    # dev stack can never clobber the deployed-infra state even if they share a
    # storage account/container. Account/container in dev-terraform.yml.
    key = "dev-terraform.tfstate"
  }
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "=3.91.0"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.2.0"
    }
  }
}

provider "azurerm" {
  features {}
}

data "azurerm_client_config" "current" {}

locals {
  tags = {
    "managed"       = "terraformed"
    "environment"   = "dev"
    "team"          = var.team
    "projectNumber" = var.projectNumber
  }
}

resource "azurerm_resource_group" "dev" {
  name     = var.resourceGroupName
  location = "West US"
  tags     = local.tags
}

# --- Dev blob storage (owned by THIS state; throwaway dev data) --------------
resource "azurerm_storage_account" "dev" {
  name                     = var.storageAccountName
  resource_group_name      = azurerm_resource_group.dev.name
  location                 = azurerm_resource_group.dev.location
  account_replication_type = "LRS"
  account_tier             = "Standard"
  tags                     = local.tags
}

# The app expects this container to exist — AzureBlobStorageService uploads do
# not create it. Terraforming it removes a manual footgun.
resource "azurerm_storage_container" "files" {
  name                  = "files"
  storage_account_name  = azurerm_storage_account.dev.name
  container_access_type = "private"
}

# --- Dev Key Vault (RBAC) ----------------------------------------------------
resource "azurerm_key_vault" "dev" {
  name                       = var.keyVaultName
  location                   = azurerm_resource_group.dev.location
  resource_group_name        = azurerm_resource_group.dev.name
  tenant_id                  = data.azurerm_client_config.current.tenant_id
  sku_name                   = "standard"
  soft_delete_retention_days = 7
  purge_protection_enabled   = true
  enable_rbac_authorization  = true
  tags                       = local.tags
}

# --- Managed identity for an optional deployed dev API consumer --------------
resource "azurerm_user_assigned_identity" "dev" {
  name                = "neptune-dev-identity"
  location            = azurerm_resource_group.dev.location
  resource_group_name = azurerm_resource_group.dev.name
}

# --- Role assignments (RBAC) -------------------------------------------------
# The pipeline SP writes/seeds secrets.
resource "azurerm_role_assignment" "pipeline_secrets_officer" {
  scope                = azurerm_key_vault.dev.id
  role_definition_name = "Key Vault Secrets Officer"
  principal_id         = data.azurerm_client_config.current.object_id
}

# The dev identity reads secrets + accesses dev blob.
resource "azurerm_role_assignment" "identity_secrets_user" {
  scope                = azurerm_key_vault.dev.id
  role_definition_name = "Key Vault Secrets User"
  principal_id         = azurerm_user_assigned_identity.dev.principal_id
}

resource "azurerm_role_assignment" "identity_blob_contributor" {
  scope                = azurerm_storage_account.dev.id
  role_definition_name = "Storage Blob Data Contributor"
  principal_id         = azurerm_user_assigned_identity.dev.principal_id
}

# The restore-dev-blob pipeline azcopies prod blobs into the dev account with Entra auth (no keys), so
# the restore SP needs blob data access on the dev account (the copy destination). CRITICAL: that pipeline
# runs under the PRODUCTION service connection, but THIS stack is applied by the Dev/Test connection - two
# different service principals - so data.azurerm_client_config.current (the applying SP) is the WRONG
# identity. Grant the restore SP explicitly by object id. Shared esassoc prod devops SP; object id is an
# identifier, not a secret (same as the h2o groups) and must be the Enterprise-App/SP object id (not the
# appId). Prod-source read is granted in the root neptune.tf.
variable "restorePipelineSpObjectId" {
  type    = string
  default = "6428d718-576e-44ad-a703-673723d2a88d"
}

resource "azurerm_role_assignment" "pipeline_blob_contributor" {
  count                = var.restorePipelineSpObjectId != "" ? 1 : 0
  scope                = azurerm_storage_account.dev.id
  role_definition_name = "Storage Blob Data Contributor"
  principal_id         = var.restorePipelineSpObjectId
}

# The dev team (AAD group) reads secrets via `az login` DefaultAzureCredential.
resource "azurerm_role_assignment" "dev_group_secrets_user" {
  count                = var.devReaderGroupObjectId != "" ? 1 : 0
  scope                = azurerm_key_vault.dev.id
  role_definition_name = "Key Vault Secrets User"
  principal_id         = var.devReaderGroupObjectId
}

# --- Seeded secrets ----------------------------------------------------------
# The blob connection string comes straight from the dev storage account, so it
# is always seeded and stays correct if keys rotate (re-apply).
resource "azurerm_key_vault_secret" "azureBlobStorageConnectionString" {
  name         = "AzureBlobStorageConnectionString"
  value        = azurerm_storage_account.dev.primary_connection_string
  key_vault_id = azurerm_key_vault.dev.id
  tags         = local.tags
  depends_on   = [azurerm_role_assignment.pipeline_secrets_officer]
}

# Only seed the rest when a value was supplied (avoids writing empty secrets).
resource "azurerm_key_vault_secret" "sendGridApiKey" {
  count        = var.secretSendGridApiKey != "" ? 1 : 0
  name         = "SendGridApiKey"
  value        = var.secretSendGridApiKey
  key_vault_id = azurerm_key_vault.dev.id
  tags         = local.tags
  depends_on   = [azurerm_role_assignment.pipeline_secrets_officer]
}

resource "azurerm_key_vault_secret" "anthropicApiKey" {
  count        = var.secretAnthropicApiKey != "" ? 1 : 0
  name         = "AnthropicApiKey"
  value        = var.secretAnthropicApiKey
  key_vault_id = azurerm_key_vault.dev.id
  tags         = local.tags
  depends_on   = [azurerm_role_assignment.pipeline_secrets_officer]
}

output "key_vault_name" {
  value = azurerm_key_vault.dev.name
}

output "dev_identity_client_id" {
  value = azurerm_user_assigned_identity.dev.client_id
}
