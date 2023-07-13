# Creates key vault
resource "azurerm_key_vault" "bactool_kv" {
  name                        = "adda-${lower(var.env)}-kv"
  location                    = azurerm_resource_group.bactool_rg.location
  resource_group_name         = azurerm_resource_group.bactool_rg.name
  sku_name                    = "standard"
  tenant_id                   = data.azurerm_client_config.current.tenant_id
  enabled_for_disk_encryption = true
  tags                        = local.tags

  access_policy {
    tenant_id = data.azurerm_client_config.current.tenant_id
    object_id = data.azurerm_client_config.current.object_id

    key_permissions = [
      "Create",
      "Get",
      "List"
    ]

    secret_permissions = [
      "Set",
      "Get",
      "List",
      "Delete",
      "Purge",
      "Recover"
    ]
  }
}

# Creates a secret to hold AZDO access token
resource "azurerm_key_vault_secret" "bactool_azdo_secret" {
  name         = "AzureDevOpsAccessToken"
  value        = "ToBeDefined"
  key_vault_id = azurerm_key_vault.bactool_kv.id
}

# Creates access policy for the function app to read secrets
resource "azurerm_key_vault_access_policy" "bactool_function_app_ap" {
  key_vault_id = azurerm_key_vault.bactool_kv.id
  tenant_id    = data.azurerm_client_config.current.tenant_id
  object_id    = azurerm_function_app.bactool_func_app.identity[0].principal_id

  key_permissions = [
    "Get",
  ]

  secret_permissions = [
    "Get",
  ]
}