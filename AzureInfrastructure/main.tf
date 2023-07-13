data "azurerm_client_config" "current" {}

# Creates resource group
resource "azurerm_resource_group" "bactool_rg" {
  name     = "adda-${lower(var.env)}-rg"
  location = "West Europe"
  tags = local.tags
}