namespace ADDA.Functions.Tests
{
    [Binding]
    public class AddaAzureFunctionsDevOpsWorkItemsSteps
    {
        #region Constant strings

        private const string DevOpsOrganization = "DevOpsOrganization";
        private const string WorkItemList = "WorkItemList";
        private const string WorkItemIdsList = "WorkItemIdsList";

        #endregion

        private readonly ScenarioContext _scenarioContext;

        public AddaAzureFunctionsDevOpsWorkItemsSteps(ScenarioContext scenarioContext)
        {
            _scenarioContext = scenarioContext;
        }

        #region Given section

        [Given(@"the list of work items in the project")]
        public void GivenTheListOfWorkItemsInTheProject(Table workItems)
        {
            var workItemsList = new List<WorkItem>();
            foreach (var row in workItems.Rows)
            {
                var workItem = new WorkItem()
                {
                    Id = int.Parse(row["Id"]),
                    Fields = new Dictionary<string, object>()
                    {
                        { "System.WorkItemType", row["Work Item Type"] },
                        { "System.Title", row["Title"] },
                        { "System.State", row["State"] },
                        { "System.IterationPath", row["Iteration Path"] }
                    }
                };
                workItemsList.Add(workItem);
            }

            _scenarioContext.Set<List<WorkItem>>(workItemsList, WorkItemList);
        }

        #endregion

        #region When section

        [When(@"I get the done tasks ids for the project")]
        public void WhenIgetTheWorkItemIdsForTheProject()
        {
            var result = new WorkItemQueryResult();
            result.WorkItems = _scenarioContext.Get<List<WorkItem>>(WorkItemList)
                        .Where(w => w.Fields["System.WorkItemType"].ToString().Equals("Task")
                                            && w.Fields["System.State"].ToString().Equals("Done"))
                        .Select(w => new WorkItemReference() { Id = (int)w.Id });

            var workItemsClientMoq = new Mock<WorkItemTrackingHttpClient>(null, null);
            workItemsClientMoq.Setup(w => w.QueryByWiqlAsync(It.IsAny<Wiql>(), default, default, default, default))
                                .ReturnsAsync(result);

            var workItemIdsList = AddaActivityGetWorkItems.QueryWorkItemsIds(workItemsClientMoq.Object, new Wiql()).Result;

            _scenarioContext.Set<List<int>>(workItemIdsList, WorkItemIdsList);
        }

        #endregion

        #region Then section

        [Then(@"the count of ids for done tasks is (.*)")]
        public void ThenTheCountOfIdsForDoneTasksIs(int count)
        {
            var workItemIdsList = _scenarioContext.Get<List<int>>(WorkItemIdsList);
            Assert.AreEqual(count, workItemIdsList.Count);
        }

        [Then(@"an exception is thrown querying done tasks ids for the project")]
        public void ThenAnExceptionIsThrownQueryingDoneTasksIds()
        {
            var workItemsClientMoq = new Mock<WorkItemTrackingHttpClient>(null, null);
            Assert.ThrowsException<AggregateException>(() => AddaActivityGetWorkItems.QueryWorkItemsIds(workItemsClientMoq.Object, new Wiql()).Result);
        }

        #endregion
    }
}