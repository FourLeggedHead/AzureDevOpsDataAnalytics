# Creates state function app storage account
resource "azurerm_storage_account" "bactool_func_sa" {
  name                     = "adda${lower(var.env)}funcsa"
  resource_group_name      = azurerm_resource_group.bactool_rg.name
  location                 = azurerm_resource_group.bactool_rg.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
  tags                     = local.tags
}

# Creates service plan for function app
/*
resource "azurerm_service_plan" "bactool_service_plan" {
  name                = "adda${lower(var.env)}funcsrp"
  resource_group_name = azurerm_resource_group.bactool_rg.name
  location            = azurerm_resource_group.bactool_rg.location
  os_type             = "Linux"
  sku_name            = "Y1"
}

# Creates function app
resource "azurerm_linux_function_app" "bactool_func_app" {
  name                = "adda${lower(var.env)}funcapp"
  resource_group_name = azurerm_resource_group.bactool_rg.name
  location            = azurerm_resource_group.bactool_rg.location

  storage_account_name       = azurerm_storage_account.bactool_func_sa.name
  storage_account_access_key = azurerm_storage_account.bactool_func_sa.primary_access_key
  service_plan_id            = azurerm_service_plan.bactool_service_plan.id

  site_config {}
}*/

resource "azurerm_app_service_plan" "bactool_service_plan" {
  name                = "adda${lower(var.env)}funcsrp"
  location            = azurerm_resource_group.bactool_rg.location
  resource_group_name = azurerm_resource_group.bactool_rg.name
  kind                = "Linux"
  reserved            = true
  tags                = local.tags

  sku {
    tier = "Dynamic"
    size = "Y1"
  }
}

resource "azurerm_function_app" "bactool_func_app" {
  name                            = "adda${lower(var.env)}funcapp"
  location                        = azurerm_resource_group.bactool_rg.location
  resource_group_name             = azurerm_resource_group.bactool_rg.name
  app_service_plan_id             = azurerm_app_service_plan.bactool_service_plan.id
  storage_account_name            = azurerm_storage_account.bactool_func_sa.name
  storage_account_access_key      = azurerm_storage_account.bactool_func_sa.primary_access_key
  version                         = "~4"
  identity {
    type = "SystemAssigned"
  }
  #key_vault_reference_identity_id = azurerm_user_assigned_identity.bactool_func_app_uai.principal_id
  tags                            = local.tags

  app_settings = {
    "FUNCTIONS_WORKER_RUNTIME"              = "dotnet"
    "DevOpsDataStorageAppSetting"           = azurerm_storage_account.bactool_data_sa.primary_connection_string
    "BacToolKeyVaultUri"                    = azurerm_key_vault.bactool_kv.vault_uri
    "AzureDevOpsOrganizationUri"            = "https://pandora-jewelry.visualstudio.com"
    "APPLICATIONINSIGHTS_CONNECTION_STRING" = azurerm_application_insights.bactool_app_insights.connection_string
  }
}