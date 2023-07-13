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