# providers.tf

terraform {
  required_version = ">= 1.0"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
    }
  }

  backend "azurerm" {
    resource_group_name   = "terraform-state-rg"          # Replace with your state resource group
    storage_account_name  = "securelensstorage"      # Replace with your storage account name
    container_name        = "tfstate"                    # Replace with your container name
    key                   = "terraform.tfstate"
  }
}

provider "azurerm" {
  features {
    resource_group {
      prevent_deletion_if_contains_resources = false
    }
  }
  subscription_id = var.subscription_id
}
