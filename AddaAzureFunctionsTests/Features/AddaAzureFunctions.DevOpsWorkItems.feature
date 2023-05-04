Feature: Get work items from Azure DevOps

    As the Delivery Manager, I want to get the Task work items in Done state, their parent, grand parent and great grand parent work items from the selected projects in Azure DevOps,
    so that I can get report on the work done by the different teams within those projects.

    Scenario: Successfully querying done task work item ids
        Given the list of work items in the project
            | Id | Work Item Type       | Title            | State       | Iteration Path                  |
            | 1  | Task                 | First Work Item  | Done        | FirstProject\2023\Q1\Sprint 1.1 |
            | 2  | Task                 | Second Work Item | Done        | FirstProject\2023\Q1\Sprint 1.1 |
            | 3  | Task                 | Third Work Item  | Done        | FirstProject\2023\Q1\Sprint 1.1 |
            | 4  | Task                 | Fourth Work Item | In Progress | FirstProject\2023\Q1\Sprint 1.1 |
            | 5  | Product Backlog Item | Fifth Work Item  | Done        | FirstProject\2023\Q1\Sprint 1.1 |
        When I get the done tasks ids for the project
        Then the count of ids for done tasks is 3

    Scenario: An exception is returned because no matching work items ids are found
        Given the list of work items in the project
            | Id | Work Item Type       | Title            | State       | Iteration Path                  |
            | 1  | Task                 | First Work Item  | New        | FirstProject\2023\Q1\Sprint 1.1 |
            | 2  | Task                 | Second Work Item | New        | FirstProject\2023\Q1\Sprint 1.1 |
            | 3  | Task                 | Third Work Item  | New        | FirstProject\2023\Q1\Sprint 1.1 |
            | 4  | Task                 | Fourth Work Item | In Progress | FirstProject\2023\Q1\Sprint 1.1 |
            | 5  | Product Backlog Item | Fifth Work Item  | New        | FirstProject\2023\Q1\Sprint 1.1 |
        When I get the done tasks ids for the project
        Then an exception is thrown querying done tasks ids for the project