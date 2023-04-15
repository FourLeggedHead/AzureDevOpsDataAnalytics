Feature: Azure DevOps Project

# Can get projects from the Azure DevOps organization
Scenario: Verify that the user can get projects from the Azure DevOps organization
Given the Azure DevOps organization is Melchisedek
When I get the projects
Then the count of projects is at least 1