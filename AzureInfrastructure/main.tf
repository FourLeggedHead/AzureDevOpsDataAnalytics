data "azurerm_client_config" "current" {}

# Creates resource group
resource "azurerm_resource_group" "bactool_rg" {
  name     = "adda-${lower(var.env)}-rg"
  location = "West Europe"
  tags = local.tags
}

# Creates data storage account
resource "azurerm_storage_account" "bactool_data_sa" {
  name                     = "adda${lower(var.env)}datasa"
  resource_group_name      = azurerm_resource_group.bactool_rg.name
  location                 = azurerm_resource_group.bactool_rg.location
  account_kind             = "StorageV2"
  account_tier             = "Standard"
  account_replication_type = "LRS"
  is_hns_enabled           = true
  tags                     = local.tags
}

# Creates containers within data storage account
resource "azurerm_storage_container" "bactool_containers" {
  for_each             = toset(["bronze", "silver", "gold"])
  name                 = each.key
  storage_account_name = azurerm_storage_account.bactool_data_sa.name
}

# Creates the table to hold project data
resource "azurerm_storage_table" "bactool_projects_table" {
  name                 = "DevOpsProjects"
  storage_account_name = azurerm_storage_account.bactool_data_sa.name
}