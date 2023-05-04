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
        And one of the 4 projects changed name
        When the projects are recorded in the Azure table
        Then the count of pojects added in the Azure table is 0
        And the count of projects updated in the Azure table is 1

    Scenario: Verify a given project has the expected Iteration node
        Given I have a project with the name FirstProject
        And the project has FirstProject\2023\Q1 as one of its Iteration Pathes
        When I get the Iteration nodes for the project
        Then the project has the expected Iteration node 2023

    Scenario: Verify a given project has the expected count of iteration paths
        Given I have a project with the name FirstProject
        And the project has the following Iteration nodes:
            | Node Name | Children Names                     |
            | 2023      | Q1, Q2, Q3, Q4                     |
            | Q1        | Sprint 1.1, Sprint 1.2, Sprint 1.3 |
            | Q2        | Sprint 2.1, Sprint 2.2, Sprint 2.3 |
        When I get all the Iteration paths for the project
        Then the project has 12 Iteration paths

    Scenario: Find all the iteration paths ending with a node of a given name
        Given I have a project with the name FirstProject
        And the project has the following Iteration nodes:
            | Node Name | Children Names |
            | 2023      | Q1, Q2, Q3, Q4 |
            | 2022      | Q1, Q2, Q3, Q4 |
            | 2021      | Q1, Q2, Q3, Q4 |
        When I get all the Iteration paths that ends with a node named Q1
        Then the project has 3 Iteration paths

    Scenario: Verify a given project has the expected node name
        Given I have a project with the name FirstProject
        And the project has the following Iteration nodes:
            | Node Name | Children Names                     |
            | Team 1    | 2021, 2022, 2023                   |
            | 2023      | Q1, Q2, Q3, Q4                     |
            | Q1        | Sprint 1.1, Sprint 1.2, Sprint 1.3 |
            | Q2        | Sprint 2.1, Sprint 2.2, Sprint 2.3 |
        When I check one of the nodes is named 2023
        Then the response is true