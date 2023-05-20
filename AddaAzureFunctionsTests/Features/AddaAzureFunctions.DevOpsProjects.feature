Feature: Azure DevOps Project

    As <role> I can extract and store the list of all the Azure DevOps projects from our organization
    so that I can select the ones I want to be accouted for to generate the financial reports.

    Scenario: Verify that the user can get projects from the Azure DevOps organization
        Given the Azure DevOps organization is Melchisedek
        When I get the projects
        Then the count of projects is at least 1

    Scenario: Record DevOps projects in an Azure Table
        Given a list of 4 Azure DevOps projects
        And an empty Azure table
        When the projects are recorded in the Azure table
        Then the count of pojects added in the Azure table is 4
        And the count of projects updated in the Azure table is 0

    Scenario: Add new projects to the Azure Table
        Given a list of 4 Azure DevOps projects
        And an Azure table containing 2 of the projects
        When the projects are recorded in the Azure table
        Then the count of pojects added in the Azure table is 2
        And the count of projects updated in the Azure table is 0

    Scenario: Update existing projects in the Azure Table
        Given a list of 4 Azure DevOps projects
        And an Azure table containing 4 of the projects
        And the 4th project in the list changed name
        When the projects are recorded in the Azure table
        Then the count of pojects added in the Azure table is 0
        And the count of projects updated in the Azure table is 1