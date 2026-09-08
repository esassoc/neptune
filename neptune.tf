
variable "keyVaultName" {
  type = string
}

variable "storageAccountName" {
  type = string
}

variable "resourceGroupName" {
  type = string
}

variable "sqlUsername" {
  type = string
}

variable "sqlPassword" {
  type = string
}

variable "databaseName" {
  type = string
}

variable "dbServerName" {
  type = string
}

variable "databaseEdition" {
  type = string
}

variable "databaseTier" {
  type = string
}

variable "environment" {
  type = string
}

variable "azureClusterResourceGroup" {
  type = string
}

variable "databaseResourceGroup" {
  type = string
}

variable "sqlApiUsername" {
  type = string
}

variable "sqlGeoserverUsername" {
  type = string
}

variable "datadogApiKey" {
  type = string
  sensitive = true
}

variable "datadogAppKey" {
  type = string
  sensitive = true
}

variable "domainApi" {
  type = string
}

variable "domainExternalApi" {
  type = string
}

variable "domainWeb" {
  type = string
}

variable "domainGeoserver" {
  type = string
}

variable "projectNumber" {
  type = string
}

variable "team" {
  type = string
}

variable "elasticPoolName" {
  type = string
}

// this variable is used for the keepers for the random resources https://registry.terraform.io/providers/hashicorp/random/latest/docs
variable "amd_id" {
  type = string
  sensitive = false
  default = "1"
}

# --- NPT-1112: workload identity + Key Vault runtime config ------------------

# AKS cluster OIDC issuer URL (spoke KV secret kv-clusterOidcIssuerUrl) — the
# issuer for the federated workload-identity credentials. Neptune shares the
# cluster with wave-runup, so this is the same issuer.
variable "clusterOidcIssuerUrl" {
  type = string
}

# K8s namespace the Neptune ServiceAccounts live in (helm deploys to $(team)).
variable "aksNamespace" {
  type    = string
  default = "h2o"
}

# Runtime app secrets seeded into the vault (only when non-empty).
variable "sendGridApiKey" {
  type      = string
  sensitive = true
  default   = ""
}

variable "anthropicApiKey" {
  type      = string
  sensitive = true
  default   = ""
}

# H2O Entra group object IDs (identifiers, not secrets) for Key Vault read
# access via `az login` + DefaultAzureCredential. Neptune shares the h2o team
# with wave-runup, so these are the same groups the pipeline's db-aad-user
# grants reference by display name. Empty string skips the grant.
variable "h2oQaGroupObjectId" {
  type    = string
  default = "c17266ef-57de-4cb9-b505-80a1eeccec60"
}

variable "h2oProdGroupObjectId" {
  type    = string
  default = "63de4f43-d4c8-4ba6-8718-a8a20a06f7cd"
}

variable "h2oReadersGroupObjectId" {
  type    = string
  default = "5136cec4-2c3d-41c5-b938-1a8053938118"
}

# Object id of the Dev/Test devops SP (esadatatechnology-Azure-Devops) that runs the QA blob restore
# under the Dev/Test service connection. That SP can't reach the prod subscription, so it gets read-only
# access to the prod SOURCE account here (azurerm_role_assignment.qa_restore_source_reader, prod env only).
# Object id = identifier, not a secret (same as the h2o groups); shared esassoc org-wide.
variable "qaRestoreSpObjectId" {
  type    = string
  default = "c85db245-efe7-45f4-a0b8-a6bcc397d307"
}

terraform {
	required_version   = ">= 0.11"
	backend "azurerm" {
		container_name          = "terraform"
		key                     = "terraform.tfstate"
	} 
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "=3.91.0"
    }
    random = {
      source = "hashicorp/random"
      version = "~> 3.2.0"
    }
    time = {
      source  = "hashicorp/time"
      version = "~> 0.9"
    }
      datadog = {
      source = "DataDog/datadog"
    }
  }
}

# Configure the Azure Provider
provider "azurerm" {
  features {}
}

# Configure the Datadog provider
provider "datadog" {
  api_key = var.datadogApiKey
  app_key = var.datadogAppKey
}

data "azurerm_client_config" "current" {}

locals {
  tags = {
    "managed"     = "terraformed"
    "environment" = var.environment
    "team" = var.team
    "projectNumber" = var.projectNumber
  }
}


resource "azurerm_resource_group" "web" {
	name                         = var.resourceGroupName
  location                     = "West US"
  tags                         = local.tags
}

#blob storage
resource "azurerm_storage_account" "web" {
	name                         = var.storageAccountName
	resource_group_name          = azurerm_resource_group.web.name
	location                     = azurerm_resource_group.web.location
  account_replication_type	 	 = "GRS"
	account_tier								 = "Standard"
	tags                         = local.tags
}

# outputs like this will be set as pipeline variables
# in this case the pipeline will have access to "$(TF_OUT_APPLICATiON_STORAGE_ACCOUNT_KEY)"
# to make this happen, you can do this with your pipeline:
# - task: TerraformCLI@0
#   displayName: 'terraform output'
#   inputs:
#     command: output
output "application_storage_account_key" {
  sensitive = true
  value = azurerm_storage_account.web.primary_access_key
}

# the SAS token which is needed for the geoserver file transfer
data "azurerm_storage_account_sas" "web" {
  connection_string = azurerm_storage_account.web.primary_connection_string
  https_only        = true

  resource_types {
    service   = true
    container = true
    object    = true
  }

  services {
    blob  = true
    queue = false
    table = false
    file  = true
  }

  start  = timestamp()
  expiry = timeadd(timestamp(), "24h")

  permissions {
    read    = true
    write   = true
    delete  = true
    list    = true
    add     = true
    create  = true
    update  = true
    process = true
    tag     = false
    filter  = false
  }
}

# can be used in pipeline like $(TF_OUT_STORAGE_ACCOUNT_SAS_KEY)
output "storage_account_sas_key" {
  sensitive = true
  value = data.azurerm_storage_account_sas.web.sas
}

resource "azurerm_storage_share" "web" {
  name                 = "geoserver"
  storage_account_name = azurerm_storage_account.web.name
  quota                = 10 //10gb
}

#sql
data "azurerm_mssql_server" "spoke" {
  name                = var.dbServerName
  resource_group_name = var.databaseResourceGroup
}

data "azurerm_mssql_elasticpool" "spoke" {
  name                = var.elasticPoolName
  resource_group_name = var.databaseResourceGroup
  server_name         = var.dbServerName
}

resource "azurerm_mssql_database" "database" {
	name           = var.databaseName
  server_id      = data.azurerm_mssql_server.spoke.id
  collation      = "SQL_Latin1_General_CP1_CI_AS"
  license_type   = "LicenseIncluded"
  max_size_gb    = 250
  read_scale     = false
  sku_name       = var.databaseTier
  zone_redundant = false
  elastic_pool_id = data.azurerm_mssql_elasticpool.spoke.id
  enclave_type = "VBS"

  long_term_retention_policy {
    weekly_retention  = "P3M"
    monthly_retention = "P1Y"
    yearly_retention  = "P3Y"
    week_of_year      = 7
  }

  short_term_retention_policy {
    retention_days = 30
  }
	tags = local.tags
}

output "database_id" {
  value = azurerm_mssql_database.database.id
}

### BEGIN API Sql user/login ###
resource "random_password" "sqlApiPassword" {
  length           = 16
  special          = true
  override_special = "!+-"
  min_lower        = 3
  min_upper        = 3
  min_special      = 3
  min_numeric      = 3
  keepers = {
    amd_id = var.amd_id
  }
}

output "sql_api_password" {
  sensitive = true
  value = random_password.sqlApiPassword.result
  depends_on = [
    random_password.sqlApiPassword
  ]
}

### END API Sql user/login ###


### BEGIN Geoserver Sql user/login ###
resource "random_password" "geoserverAdminPassword" {
  length           = 16
  special          = true
  override_special = "!+-"
  min_lower        = 3
  min_upper        = 3
  min_special      = 3
  min_numeric      = 3
  keepers = {
    amd_id = var.amd_id
  }
}

output "geoserver_admin_password" {
  sensitive = true
  value = random_password.geoserverAdminPassword.result
  depends_on = [
    random_password.geoserverAdminPassword
  ]
}


resource "random_password" "sqlGeoserverPassword" {
  length           = 16
  special          = true
  override_special = "!+-"
  min_lower        = 3
  min_upper        = 3
  min_special      = 3
  min_numeric      = 3
  keepers = {
    amd_id = var.amd_id
  }
}

output "sql_geoserver_password" {
  sensitive = true
  value = random_password.sqlGeoserverPassword.result
  depends_on = [
    random_password.sqlGeoserverPassword
  ]
}

### END Geoserver Sql user/login ###

### BEGIN Hangfire password ###
resource "random_password" "hangfirePassword" {
  length           = 16
  special          = true
  override_special = "!*-_"
  min_special      = 1
  min_lower        = 1
  min_upper        = 1
  min_numeric      = 1
  keepers = {
    amd_id = var.amd_id
  }
}

output "hangfire_password" {
  sensitive = true
  value = random_password.hangfirePassword.result
  depends_on = [
    random_password.hangfirePassword
  ]
}
### END Hangfire password ###


#key vault was created prior to terraform run
resource "azurerm_key_vault" "web" {
  name                         = var.keyVaultName
	location                     = azurerm_resource_group.web.location
 
  resource_group_name          = azurerm_resource_group.web.name
	soft_delete_retention_days   = 7
  purge_protection_enabled     = false
  tenant_id                    = data.azurerm_client_config.current.tenant_id
  tags                         = local.tags

  # NPT-1112: RBAC authorization — data-plane access via role assignments (the
  # pipeline SP's Secrets Officer grant, the workload identity, and the H2O
  # groups; see the workload-identity section below). PREREQ (manual,
  # out-of-band, per environment): the pipeline SP holds "Role Based Access
  # Control Administrator" on this vault BEFORE the apply — the SP's Contributor
  # covers flipping enable_rbac_authorization (vaults/write) but NOT
  # Microsoft.Authorization/roleAssignments/write, which it needs to create the
  # Secrets Officer/User/group assignments below (else those, and its own secret
  # writes, 403). This is the same grant used on the other ESA apps (riparis,
  # wave-runup). Do the QA vault first, verify, then prod.
  enable_rbac_authorization = true

  sku_name = "standard"
}

resource "azurerm_key_vault_secret" "sqlAdminPass" {
  name                         = "sqlAdministratorPassword"
  value                        = var.sqlPassword
  key_vault_id                 = azurerm_key_vault.web.id

  tags                         = local.tags
  depends_on = [
    time_sleep.kv_rbac_propagation
  ]
}
 
resource "azurerm_key_vault_secret" "sqlAdminUser" {
  name                         = "sqlAdministratorUsername"
  value                        = var.sqlUsername
  key_vault_id                 = azurerm_key_vault.web.id

  tags                         = local.tags
  depends_on = [
    time_sleep.kv_rbac_propagation
  ]
}
 
resource "azurerm_key_vault_secret" "sqlApiUsername" {
  name                         = "sqlApiUsername"
  value                        = var.sqlApiUsername
  key_vault_id                 = azurerm_key_vault.web.id

  tags                         = local.tags
  depends_on = [
    time_sleep.kv_rbac_propagation
  ]
}

resource "azurerm_key_vault_secret" "sqlApiPassword" {
  name                         = "sqlApiPassword"
  value                        = random_password.sqlApiPassword.result
  key_vault_id                 = azurerm_key_vault.web.id

  tags                         = local.tags
  depends_on = [
    time_sleep.kv_rbac_propagation
  ]
}

resource "azurerm_key_vault_secret" "sqlApiConnectionString" {
  name                         = "sqlApiConnectionString"
  value                        = "Data Source=tcp:${data.azurerm_mssql_server.spoke.fully_qualified_domain_name},1433;Initial Catalog=${var.databaseName};Persist Security Info=True;User ID=${var.sqlApiUsername};Password=${random_password.sqlApiPassword.result}"
  key_vault_id                 = azurerm_key_vault.web.id

  tags                         = local.tags
  depends_on = [
    time_sleep.kv_rbac_propagation
  ]
}

resource "azurerm_key_vault_secret" "sqlGeoserverUsername" {
  name                         = "sqlGeoserverUsername"
  value                        = var.sqlGeoserverUsername
  key_vault_id                 = azurerm_key_vault.web.id

  tags                         = local.tags
  depends_on = [
    time_sleep.kv_rbac_propagation
  ]
}

resource "azurerm_key_vault_secret" "sqlGeoserverPassword" {
  name                         = "sqlGeoserverPassword"
  value                        = random_password.sqlGeoserverPassword.result
  key_vault_id                 = azurerm_key_vault.web.id

  tags                         = local.tags
  depends_on = [
    time_sleep.kv_rbac_propagation
  ]
}

resource "azurerm_key_vault_secret" "sqlGeoserverConnectionString" {
  name                         = "sqlGeoserverConnectionString"
  value                        = "Data Source=tcp:${data.azurerm_mssql_server.spoke.fully_qualified_domain_name},1433;Initial Catalog=${var.databaseName};Persist Security Info=True;User ID=${var.sqlGeoserverUsername};Password=${random_password.sqlGeoserverPassword.result}"
  key_vault_id                 = azurerm_key_vault.web.id

  tags                         = local.tags
  depends_on = [
    time_sleep.kv_rbac_propagation
  ]
}

resource "azurerm_key_vault_secret" "geoserverAdminPassword" {
  name                         = "geoserverAdminPassword"
  value                        = random_password.geoserverAdminPassword.result
  key_vault_id                 = azurerm_key_vault.web.id

  tags                         = local.tags
  depends_on = [
    time_sleep.kv_rbac_propagation
  ]
}

resource "azurerm_key_vault_secret" "hangfirePassword" {
  # NPT-1112: this secret doubles as the runtime HangfirePassword config value —
  # KV secret names and .NET config keys are both case-insensitive, so do NOT
  # seed a second HangfirePassword secret (it would be the SAME underlying vault
  # secret managed by two Terraform resources).
  name                         = "hangfirePassword"
  value                        = random_password.hangfirePassword.result
  key_vault_id                 = azurerm_key_vault.web.id

  tags                         = local.tags
  depends_on = [
    time_sleep.kv_rbac_propagation
  ]
}

# =============================================================================
# NPT-1112: workload identity + Key Vault runtime config
# =============================================================================
# QA/prod neptune-api and neptune-externalapi pods carry no Kubernetes secret:
# they authenticate as the user-assigned identity below (federated to their
# ServiceAccounts via the cluster OIDC issuer) and read config from this env's
# Key Vault at startup (KeyVaultName -> AddAzureKeyVault(DefaultAzureCredential)
# in Neptune.{API,ExternalAPI} Program.cs). SQL auth for the API is
# 'Authentication=Active Directory Default' — no SQL usernames/passwords.
# Mirrors wave-runup WAVE-30.
#
# GeoServer, OverlayAPI, GDALAPI and the nereid services stay on SQL auth and
# keep reading their password-based secrets (sqlApi*/sqlGeoserver*/geoserver*)
# above — they are intentionally NOT federated here.

# The pipeline SP writes/seeds secrets via RBAC once the vault flips.
resource "azurerm_role_assignment" "pipeline_kv_secrets_officer" {
  scope                = azurerm_key_vault.web.id
  role_definition_name = "Key Vault Secrets Officer"
  principal_id         = data.azurerm_client_config.current.object_id
}

# The blob-restore pipeline steps (restore-dev-blob + the deploy pipeline's env restore) run as this
# service connection and azcopy blobs with Entra auth (no keys). The pipeline SP needs blob data access on
# this env's web account, least-privilege by env: NON-prod is a copy DESTINATION (the env's refresh writes
# here) -> Contributor; PROD is only a SOURCE the dev/qa restores read from (the pipeline SP never writes
# prod blobs - app runtime writes go through the app managed identity) -> Reader. Same connection applies
# this and runs the restore, so data.azurerm_client_config.current is the right principal here.
resource "azurerm_role_assignment" "pipeline_blob_contributor" {
  scope                = azurerm_storage_account.web.id
  role_definition_name = local.is_prod ? "Storage Blob Data Reader" : "Storage Blob Data Contributor"
  principal_id         = data.azurerm_client_config.current.object_id
}

# The QA blob restore runs under the Dev/Test connection (SP c85db245), which can't reach the prod
# subscription - so grant it read-only on the prod SOURCE account. Prod env ONLY: here
# azurerm_storage_account.web IS the prod account (the source every lower env reads from). The prod deploy
# pipeline that applies this has role-assignment rights in the prod sub. Reader = read-only source.
resource "azurerm_role_assignment" "qa_restore_source_reader" {
  count                = var.qaRestoreSpObjectId != "" && local.is_prod ? 1 : 0
  scope                = azurerm_storage_account.web.id
  role_definition_name = "Storage Blob Data Reader"
  principal_id         = var.qaRestoreSpObjectId
}

# RBAC role assignments take seconds-to-minutes to propagate; secret writes in
# the same apply 403 without this buffer. Worst case the apply is re-runnable.
resource "time_sleep" "kv_rbac_propagation" {
  depends_on      = [azurerm_role_assignment.pipeline_kv_secrets_officer]
  create_duration = "120s"
}

resource "azurerm_user_assigned_identity" "neptune" {
  # var.environment is already lowercase; the pipeline's db-aad-user step must
  # reference this exact name/casing (neptune-<env>-identity).
  name                = "neptune-${var.environment}-identity"
  location            = azurerm_resource_group.web.location
  resource_group_name = azurerm_resource_group.web.name
  tags                = local.tags
}

locals {
  # ServiceAccount names = helm fullname = "<release>-<chart>". The release is
  # 'neptune' and the subchart Chart.yaml names are the BARE words
  # api/externalapi/overlayapi (the neptune-* directory names are just folders),
  # so the fullnames are neptune-api etc. — renaming a subchart's Chart.yaml
  # name would change its ServiceAccount name and break federation. These three
  # .NET pods read the DB/secrets from Azure. GDALAPI is excluded (no DB — blob
  # only); GeoServer stays on SQL auth (Java/kartoza image, no DefaultAzureCredential);
  # web is static.
  workload_identity_subjects = [
    "neptune-api",
    "neptune-externalapi",
    "neptune-overlayapi",
  ]

  is_prod = var.environment == "prod"
}

resource "azurerm_federated_identity_credential" "neptune" {
  for_each            = toset(local.workload_identity_subjects)
  name                = each.value
  resource_group_name = azurerm_resource_group.web.name
  audience            = ["api://AzureADTokenExchange"]
  issuer              = var.clusterOidcIssuerUrl
  parent_id           = azurerm_user_assigned_identity.neptune.id
  subject             = "system:serviceaccount:${var.aksNamespace}:${each.value}"
}

# The workload identity reads secrets at pod startup.
resource "azurerm_role_assignment" "identity_kv_secrets_user" {
  scope                = azurerm_key_vault.web.id
  role_definition_name = "Key Vault Secrets User"
  principal_id         = azurerm_user_assigned_identity.neptune.principal_id
}

# --- H2O group access matrix -------------------------------------------------
# The environment is the boundary:
#
#   H2O Prod     prod only              -- read and write
#   H2O QA       QA and dev             -- read and write, no prod access
#   H2O Readers  QA                     -- read, no prod access
#
# PREREQUISITE: H2O Prod is NESTED INSIDE H2O QA. Prod staff therefore reach QA and
# dev through the H2O QA grants and Azure RBAC's transitive membership resolution,
# not through grants of their own -- which is why H2O Prod is prod-only below. The
# pipeline's database matrix relies on the same nesting.
#
# Un-nesting the groups removes prod staff's non-prod access, in Azure and in SQL,
# with no code change to warn anybody. Re-add the non-prod grants at the same time.
# The redundant grants were deliberately dropped rather than kept as insurance:
# keeping them would hide an un-nesting instead of surviving it, and would leave this
# file hedging against something the database matrix already assumes.
#
# The same matrix governs database access, granted as contained users by the
# 'Grant DB access to H2O Entra groups' step in Build/azure-pipelines.yml. Change
# both together or the boundary is fiction: an earlier pass removed the prod vault
# grant and left the prod database grant behind, which is worse than either
# consistent state.
#
# Each grant needs BOTH halves to be useful, which is the usual Azure trip-up:
# Reader at resource-group scope makes the resources visible in the portal but
# grants no blob access whatsoever, and the Storage Blob Data roles grant blob
# access but do not make the account visible. Neither implies the other, and
# Contributor on a storage account still cannot read a blob over Entra auth.
#
# Guarded on a non-empty object id so a group that does not exist yet can be
# skipped by clearing its variable.
#
# --- Key Vault ---
resource "azurerm_role_assignment" "h2o_prod_group_kv_secrets_officer" {
  count                = var.h2oProdGroupObjectId != "" && local.is_prod ? 1 : 0
  scope                = azurerm_key_vault.web.id
  role_definition_name = "Key Vault Secrets Officer"
  principal_id         = var.h2oProdGroupObjectId
}

resource "azurerm_role_assignment" "h2o_qa_group_kv_secrets_officer" {
  count                = var.h2oQaGroupObjectId != "" && !local.is_prod ? 1 : 0
  scope                = azurerm_key_vault.web.id
  role_definition_name = "Key Vault Secrets Officer"
  principal_id         = var.h2oQaGroupObjectId
}

resource "azurerm_role_assignment" "h2o_readers_group_kv_secrets_user" {
  count                = var.h2oReadersGroupObjectId != "" && !local.is_prod ? 1 : 0
  scope                = azurerm_key_vault.web.id
  role_definition_name = "Key Vault Secrets User"
  principal_id         = var.h2oReadersGroupObjectId
}

# --- Resource group: makes the environment's resources visible at all ---
# Reader here is management-plane only: it makes the resources visible and grants
# no blob access at all. The Storage Blob Data roles below are the other half, and
# neither implies the other -- Contributor on a storage account still cannot read
# a blob over Entra auth.
resource "azurerm_role_assignment" "h2o_prod_group_rg_reader" {
  count                = var.h2oProdGroupObjectId != "" && local.is_prod ? 1 : 0
  scope                = azurerm_resource_group.web.id
  role_definition_name = "Reader"
  principal_id         = var.h2oProdGroupObjectId
}

resource "azurerm_role_assignment" "h2o_qa_group_rg_reader" {
  count                = var.h2oQaGroupObjectId != "" && !local.is_prod ? 1 : 0
  scope                = azurerm_resource_group.web.id
  role_definition_name = "Reader"
  principal_id         = var.h2oQaGroupObjectId
}

resource "azurerm_role_assignment" "h2o_readers_group_rg_reader" {
  count                = var.h2oReadersGroupObjectId != "" && !local.is_prod ? 1 : 0
  scope                = azurerm_resource_group.web.id
  role_definition_name = "Reader"
  principal_id         = var.h2oReadersGroupObjectId
}

# --- Storage blobs: the data plane ---
# Scoped to the application storage account. The count-conditional "dev" account
# some of these stacks declare is deliberately left alone: it is the throwaway
# mirror restore-dev-blob.yml populates rather than application data, and it
# exists only when storageAccountDevApplicationName is set.
resource "azurerm_role_assignment" "h2o_prod_group_blob_contributor" {
  count                = var.h2oProdGroupObjectId != "" && local.is_prod ? 1 : 0
  scope                = azurerm_storage_account.web.id
  role_definition_name = "Storage Blob Data Contributor"
  principal_id         = var.h2oProdGroupObjectId
}

resource "azurerm_role_assignment" "h2o_qa_group_blob_contributor" {
  count                = var.h2oQaGroupObjectId != "" && !local.is_prod ? 1 : 0
  scope                = azurerm_storage_account.web.id
  role_definition_name = "Storage Blob Data Contributor"
  principal_id         = var.h2oQaGroupObjectId
}

resource "azurerm_role_assignment" "h2o_readers_group_blob_reader" {
  count                = var.h2oReadersGroupObjectId != "" && !local.is_prod ? 1 : 0
  scope                = azurerm_storage_account.web.id
  role_definition_name = "Storage Blob Data Reader"
  principal_id         = var.h2oReadersGroupObjectId
}

# --- Runtime app secrets (read by Neptune.API + Neptune.ExternalAPI) ---------
# KV secret names map 1:1 onto Neptune's flat PascalCase config keys (see
# NeptuneKeyVaultSecretManager) — no name transformation. Case-insensitive, so
# the hangfirePassword secret above already satisfies the HangfirePassword
# config key; a second one is not seeded.
resource "azurerm_key_vault_secret" "appDatabaseConnectionString" {
  name = "DatabaseConnectionString"
  # AAD-based auth. Pods authenticate via DefaultAzureCredential (workload
  # identity -> azurerm_user_assigned_identity.neptune). The DB user for the
  # identity is created by the pipeline's db-aad-user.yml@BuildTemplates step
  # after every DacPac deploy (CREATE USER FROM EXTERNAL PROVIDER); the shared
  # spoke SQL server's Entra admin is already configured. The legacy sqlApi*
  # secrets + SQL login are retained during the transition; remove them once
  # nothing reads the password path.
  value        = "Server=tcp:${data.azurerm_mssql_server.spoke.fully_qualified_domain_name},1433;Database=${var.databaseName};Authentication=Active Directory Default;Encrypt=True;"
  key_vault_id = azurerm_key_vault.web.id
  tags         = local.tags
  depends_on   = [time_sleep.kv_rbac_propagation]
}

resource "azurerm_key_vault_secret" "appBlobConnectionString" {
  name         = "AzureBlobStorageConnectionString"
  value        = azurerm_storage_account.web.primary_connection_string
  key_vault_id = azurerm_key_vault.web.id
  tags         = local.tags
  depends_on   = [time_sleep.kv_rbac_propagation]
}

# Only seed when a value was supplied (avoids writing empty secrets).
resource "azurerm_key_vault_secret" "appSendGridApiKey" {
  count        = var.sendGridApiKey != "" ? 1 : 0
  name         = "SendGridApiKey"
  value        = var.sendGridApiKey
  key_vault_id = azurerm_key_vault.web.id
  tags         = local.tags
  depends_on   = [time_sleep.kv_rbac_propagation]
}

resource "azurerm_key_vault_secret" "appAnthropicApiKey" {
  count        = var.anthropicApiKey != "" ? 1 : 0
  name         = "AnthropicApiKey"
  value        = var.anthropicApiKey
  key_vault_id = azurerm_key_vault.web.id
  tags         = local.tags
  depends_on   = [time_sleep.kv_rbac_propagation]
}

output "workload_identity_client_id" {
  value = azurerm_user_assigned_identity.neptune.client_id
}

output "workload_identity_tenant_id" {
  value = azurerm_user_assigned_identity.neptune.tenant_id
}

resource "datadog_synthetics_test" "api_test" {
  type    = "api"
  subtype = "http"
  request_definition {
    method = "GET"
    url    = "https://${var.domainApi}/healthz"
  }
  request_headers = {
    Content-Type   = "application/json"
  }
  assertion {
    type     = "statusCode"
    operator = "is"
    target   = "200"
  }
  locations = ["aws:us-west-1","aws:us-east-1"]
  options_list {
    tick_every = 900

    retry {
      count    = 2
      interval = 30000
    }

    monitor_options {
      renotify_interval = 120
    }
  }
  name    = "${var.environment} - https://${var.domainApi}/healthz API test"
  message = "Notify @rlee@esassoc.com @sgordon@esassoc.com @team-${var.team}${var.environment == "qa" ? "-qa" : ""}"
  tags    = ["env:${var.environment}", "managed:terraformed", "team:${var.team}"]

  status = "live"
}

resource "datadog_synthetics_test" "externalapi_test" {
  type    = "api"
  subtype = "http"
  request_definition {
    method = "GET"
    url    = "https://${var.domainExternalApi}/healthz"
  }
  request_headers = {
    Content-Type   = "application/json"
  }
  assertion {
    type     = "statusCode"
    operator = "is"
    target   = "200"
  }
  locations = ["aws:us-west-1","aws:us-east-1"]
  options_list {
    tick_every = 900

    retry {
      count    = 2
      interval = 30000
    }

    monitor_options {
      renotify_interval = 120
    }
  }
  name    = "${var.environment} - https://${var.domainExternalApi}/healthz ExternalAPI test"
  message = "Notify @rlee@esassoc.com @sgordon@esassoc.com @team-${var.team}${var.environment == "qa" ? "-qa" : ""}"
  tags    = ["env:${var.environment}", "managed:terraformed", "team:${var.team}"]

  status = "live"
}

resource "datadog_synthetics_test" "web_test" {
  type    = "api"
  subtype = "http"
  request_definition {
    method = "GET"
    url    = "https://${var.domainWeb}"
  }
  request_headers = {
    Content-Type   = "application/json"
  }
  assertion {
    type     = "statusCode"
    operator = "is"
    target   = "200"
  }
  locations = ["aws:us-west-1","aws:us-east-1"]
  options_list {
    tick_every = 900

    retry {
      count    = 2
      interval = 30000
    }

    monitor_options {
      renotify_interval = 120
    }
  }
  name    = "${var.environment} - ${var.domainWeb} Web test"
  message = "Notify @rlee@esassoc.com @sgordon@esassoc.com @team-${var.team}${var.environment == "qa" ? "-qa" : ""}"
  tags    = ["env:${var.environment}", "managed:terraformed", "team:${var.team}"]

  status = "live"
}

resource "datadog_synthetics_test" "geoserver_test" {
  type    = "api"
  subtype = "http"
  request_definition {
    method = "GET"
    url    = "https://${var.domainGeoserver}/geoserver/web/wicket/resource/org.geoserver.web.GeoServerBasePage/img/logo.png"
  }
  request_headers = {
    Content-Type   = "application/json"
  }
  assertion {
    type     = "statusCode"
    operator = "is"
    target   = "200"
  }
  locations = ["aws:us-west-1","aws:us-east-1"]
  options_list {
    tick_every = 900

    retry {
      count    = 2
      interval = 30000
    }

    monitor_options {
      renotify_interval = 120
    }
  }
  name    = "${var.environment} - https://${var.domainWeb} Geoserver test"
  message = "Notify @rlee@esassoc.com @sgordon@esassoc.com  @team-${var.team}${var.environment == "qa" ? "-qa" : ""}"
  tags    = ["env:${var.environment}", "managed:terraformed", "team:${var.team}"]
  status = "live"
}

