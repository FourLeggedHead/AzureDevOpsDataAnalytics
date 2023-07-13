# Local, derivative variables.
# Use this for parsing or setting default values to be passed down to child resource.
locals {
  tags = {
    Environment    = upper(var.env)
  }
}