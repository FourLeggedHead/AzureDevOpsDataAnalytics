Feature: Azure DevOps Project

    As <role> I can extract and store the list of all the Azure DevOps projects from our organization
    so that I can select the ones I want to be accouted for to generate the financial reports.

    Scenario: Verify that the user can get projects from the Azure DevOps organization
        Given the Azure DevOps organization is Melchisedek
        When I get the projects
        Then the count of projects is at least 1

    Scenario: Record DevOps projects in an Azure Table
        Given a list of Azure DevOps projects
        And an empty Azure table
        When the projects are recorded in the Azure table
        Then the count of entities in the Azure table is greater than zero