namespace ADDA.Functions.Tests;

[Binding]
public class AddaAzureFunctionsDevOpsProjectsSteps
{
    private readonly ScenarioContext _scenarioContext;
    private readonly FeatureContext _featureContext;

    private const string DevOpsOrganization = "DevOpsOrganization";
    private const string AzureDevOpsProjectList = "AzureDevOpsProjectList";
    private const string MockedProjectsTableClient = "MockedProjectsTableClient";
    private const string MockedDevOpsProjectsTable = "MockedDevOpsProjectsTable";
    private const string MockedDevOpsProjects = "MockedDevOpsProjects";
    private const string DevOpsProjectsCounts = "DevOpsProjectsCounts";

    public AddaAzureFunctionsDevOpsProjectsSteps(ScenarioContext scenarioContext, FeatureContext featureContext)
    {
        // Create a DevOpsProjectCollection object and get the project collection URI
        var addaDevOpsOrganization = new AddaDevOpsOrganization();
        addaDevOpsOrganization.GetOrganizationUri();

        _featureContext = featureContext;
        _featureContext.Set(addaDevOpsOrganization, DevOpsOrganization);

        _scenarioContext = scenarioContext;
    }

    #region Givens

    [Given(@"the Azure DevOps organization is (.*)")]
    public void GivenTheAzureDevOpsOrganizationIs(string organization)
    {
        var addaDevOpsOrganization = _featureContext.Get<AddaDevOpsOrganization>(DevOpsOrganization);
        StringAssert.Contains(addaDevOpsOrganization.OrganizationUri.ToString(), organization);
    }

    [Given(@"a list of (.*) Azure DevOps projects")]
    public void GivenListAzureDevOpsProjects(int numberOfProjects)
    {
        var mockedProjectList = new PagedList<TeamProjectReference>();
        for (int i = 0; i < numberOfProjects; i++)
        {
            mockedProjectList.Add(new TeamProjectReference()
            {
                Id = Guid.NewGuid(),
                Name = $"Project {i}"
            });
        }
        _scenarioContext.Set<PagedList<TeamProjectReference>>(mockedProjectList, MockedDevOpsProjects);
    }

    [Given(@"an empty Azure table")]
    public void AnEmptyAzureTable()
    {
        // Create a mock table client to simulate the empty table. It only needs 2 methods: GetEntityIfExists and AddEntity
        var mockedTableClient = new Mock<TableClient>();
        mockedTableClient.SetupGet(t => t.Name).Returns(MockedDevOpsProjectsTable);

        // This response is used when the table is empty: GetEntityIfExists returns false since the table is empty
        var mockNullableResponse = new Mock<Azure.NullableResponse<DevOpsProject>>();
        mockNullableResponse.SetupGet(r => r.HasValue).Returns(false);
        mockedTableClient.Setup(t => t.GetEntityIfExists<DevOpsProject>(It.IsAny<string>(), It.IsAny<string>(), default, default))
            .Returns(mockNullableResponse.Object);

        // We assume AddEntity works properly and retunns no error
        var mockResponse = new Mock<Azure.Response>();
        mockResponse.SetupGet(r => r.IsError).Returns(false);

        mockedTableClient.Setup(t => t.AddEntity<DevOpsProject>(It.IsAny<DevOpsProject>(), default))
            .Returns(mockResponse.Object);
        _scenarioContext.Set<Mock<TableClient>>(mockedTableClient, MockedProjectsTableClient);
    }

    [Given(@"an Azure table containing (.*) of the projects")]
    public void AnAzureTableContainingProjects(int numberOfProjects)
    {
        // Create a mock table client to simulate the table.
        var mockedTableClient = new Mock<TableClient>();
        mockedTableClient.SetupGet(t => t.Name).Returns(MockedDevOpsProjectsTable);

        // Get the list of DevOps projects from the scenario context
        var mockedProjectList = _scenarioContext.Get<PagedList<TeamProjectReference>>(MockedDevOpsProjects);

        // Setup GetEntityIfExists to mock the first "mockedProjectList.Count - numberOfProjects" are in the mocked table
        // Response must have a value, as an instance of DevOpsProject  
        var mockNullableResponseTrue = new Mock<Azure.NullableResponse<DevOpsProject>>();
        mockNullableResponseTrue.SetupGet(r => r.HasValue).Returns(true);
        mockNullableResponseTrue.SetupGet(r => r.Value)
            .Returns(new Queue<DevOpsProject>(mockedProjectList.Take(mockedProjectList.Count - numberOfProjects)
                                                                .Select(p => new DevOpsProject(p))).Dequeue);

        var inMockedProjects = mockedProjectList.Take(mockedProjectList.Count - numberOfProjects).Select(p => p.Id.ToString());
        mockedTableClient.Setup(t => t.GetEntityIfExists<DevOpsProject>(DevOpsProject.DevOpsProjectPartitionKey,
                                        It.IsIn(inMockedProjects), default, default))
                            .Returns(mockNullableResponseTrue.Object);

        // Seting up GetEntityIfExists to mock the last "numberOfProjects" are not in the mocked table
        var mockNullableResponseFalse = new Mock<Azure.NullableResponse<DevOpsProject>>();
        mockNullableResponseFalse.SetupGet(r => r.HasValue).Returns(false);

        var notInMockedProjects = mockedProjectList.TakeLast(numberOfProjects).Select(p => p.Id.ToString());
        mockedTableClient.Setup(t => t.GetEntityIfExists<DevOpsProject>(DevOpsProject.DevOpsProjectPartitionKey,
                                        It.IsIn(notInMockedProjects), default, default))
                            .Returns(mockNullableResponseFalse.Object);

        // We assume AddEntity works properly and retunns no error
        var mockResponse = new Mock<Azure.Response>();
        mockResponse.SetupGet(r => r.IsError).Returns(false);

        mockedTableClient.Setup(t => t.AddEntity<DevOpsProject>(It.IsAny<DevOpsProject>(), default))
            .Returns(mockResponse.Object);
        _scenarioContext.Set<Mock<TableClient>>(mockedTableClient, MockedProjectsTableClient);
    }

    #endregion

    #region Whens

    [When(@"I get the projects")]
    public void WhenGettingTheProjects()
    {
        var addaDevOpsOrganization = _featureContext.Get<AddaDevOpsOrganization>(DevOpsOrganization);
        var projects = AddaActivityGetProjects.GetProjectsFromDevOps(addaDevOpsOrganization);
        _scenarioContext.Set(projects, AzureDevOpsProjectList);
    }

    [When(@"the projects are recorded in the Azure table")]
    public void ProjectsRecordedInAzureTable()
    {
        var mockedTableClient = _scenarioContext.Get<Mock<TableClient>>(MockedProjectsTableClient);
        var mockedProjectList = _scenarioContext.Get<PagedList<TeamProjectReference>>(MockedDevOpsProjects);

        var projectsCounts = AddaActivityGetProjects.AddUpdateProjectsToTable(mockedTableClient.Object, mockedProjectList);
        _scenarioContext.Set<(int added, int updated)>(projectsCounts, DevOpsProjectsCounts);
    }

    #endregion

    #region Thens

    [Then(@"the count of projects is at least (.*)")]
    public void ThenTheCountOfProjectsIsAtLeast(int count)
    {
        Assert.IsTrue(_scenarioContext.Get<IPagedList<TeamProjectReference>>(AzureDevOpsProjectList).Count > count);
    }

    [Then(@"the count of pojects added in the Azure table is (.*)")]
    public void AzureTableProjectsCountAdded(int countAddedProjects)
    {
        var projectsCounts = _scenarioContext.Get<(int added, int updated)>(DevOpsProjectsCounts);
        Assert.AreEqual(countAddedProjects, projectsCounts.added);
    }

    [Then(@"the count of projects updated in the Azure table is (.*)")]
    public void AzureTableProjectsCountUpdated(int countUpdatedProjects)
    {
        var projectsCounts = _scenarioContext.Get<(int added, int updated)>(DevOpsProjectsCounts);
        Assert.AreEqual(countUpdatedProjects, projectsCounts.updated);
    }

    #endregion
}