namespace ADDA.Functions.Tests;

[Binding]
public class AddaAzureFunctionsDevOpsProjectsSteps
{
    private readonly ScenarioContext _scenarioContext;
    private readonly FeatureContext _featureContext;

    private const string DevOpsOrganization = "DevOpsOrganization";
    private const string ProjectName = "ProjectName";
    private const string ProjectRootNode = "ProjectRootNode";
    private const string ProjectNodes = "ProjectNodes";
    private const string Response = "Response";
    private const string ProjectIterationPaths = "ProjectIterationPaths";
    private const string AzureDevOpsProjectList = "AzureDevOpsProjectList";
    private const string MockedProjectsTableClient = "MockedProjectsTableClient";
    private const string MockedDevOpsProjectsTable = "MockedDevOpsProjectsTable";
    private const string MockedDevOpsProjects = "MockedDevOpsProjects";
    private const string DevOpsProjectsCounts = "DevOpsProjectsCounts";
    private const string MockedWorkClient = "MockedWorkClient";

    public AddaAzureFunctionsDevOpsProjectsSteps(ScenarioContext scenarioContext, FeatureContext featureContext)
    {
        // Create a DevOpsProjectCollection object and get the project collection URI
        var addaDevOpsOrganization = new AddaDevOpsOrganization();
        addaDevOpsOrganization.GetUri();

        _featureContext = featureContext;
        _featureContext.Set(addaDevOpsOrganization, DevOpsOrganization);

        _scenarioContext = scenarioContext;
    }

    #region Givens

    [Given(@"the Azure DevOps organization is (.*)")]
    public void GivenTheAzureDevOpsOrganizationIs(string organization)
    {
        var addaDevOpsOrganization = _featureContext.Get<AddaDevOpsOrganization>(DevOpsOrganization);
        StringAssert.Contains(addaDevOpsOrganization.Uri.ToString(), organization);
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

    [Given(@"I have a project with the name (.*)")]
    public void GivenIhaveaProjectWithWheName(string projectName)
    {
        _scenarioContext.Set<string>(projectName, ProjectName);
    }

    [Given(@"the project has (.*) as one of its Iteration Pathes")]
    public void GivenTheProjectHasInItsIterationPathes(string iterationPath)
    {
        var nodeNames = iterationPath.Split('\\');

        WorkItemClassificationNode? root = null;
        WorkItemClassificationNode? child = null;

        for (int i = nodeNames.Length; i > 0; i--)
        {
            root = new WorkItemClassificationNode()
            {
                Name = nodeNames[i - 1],
                Children = child == null ? null : new List<WorkItemClassificationNode>() { child }
            };

            child = root;
        }

        if (root != null) _scenarioContext.Set<WorkItemClassificationNode>(root, ProjectRootNode);
    }

    [Given(@"the project has the following Iteration nodes:")]
    public void GivenTheProjectHasTheFollowingIterationNodes(Table iterationNodes)
    {
        // List all the iteration nodes specified in the table
        var projectNodes = new List<WorkItemClassificationNode>();
        foreach (var row in iterationNodes.Rows)
        {
            var nodeName = row["Node Name"].Trim();
            var node = projectNodes.FirstOrDefault(n => n.Name == nodeName);
            if (node == null)
            {
                node = new WorkItemClassificationNode() { Name = nodeName };
                projectNodes.Add(node);
            }

            // Add the children to the node
            node.HasChildren = true;
            node.Children = new List<WorkItemClassificationNode>();
            foreach (var childName in row["Children Names"].Split(','))
            {
                var child = new WorkItemClassificationNode() { Name = childName.Trim() };
                projectNodes.Add(child);

                node.Children = node.Children.Append(child);
            }
        }

        // List the iteration nodes that have no parent
        var childrenNames = projectNodes.Where(n => n.Children != null).SelectMany(n => n.Children).Select(n => n.Name).ToList();
        var rootChildren = projectNodes.Where(n => !childrenNames.Contains(n.Name));

        // Create the root node and add the nodes that have no parent as children
        var projectName = _scenarioContext.Get<string>(ProjectName);
        var root = new WorkItemClassificationNode() { Name = ProjectName };
        root.HasChildren = true;
        root.Children = rootChildren.ToList();

        _scenarioContext.Set<WorkItemClassificationNode>(root, ProjectRootNode);
    }

    #endregion

    #region Whens

    [When(@"I get the projects")]
    public void WhenGettingTheProjects()
    {
        var addaDevOpsOrganization = _featureContext.Get<AddaDevOpsOrganization>(DevOpsOrganization);
        var projects = AddaActivityGetAzureDevOpsProjects.GetProjectsFromDevOps(addaDevOpsOrganization);
        _scenarioContext.Set(projects, AzureDevOpsProjectList);
    }

    [When(@"the projects are recorded in the Azure table")]
    public void ProjectsRecordedInAzureTable()
    {
        var mockedTableClient = _scenarioContext.Get<Mock<TableClient>>(MockedProjectsTableClient);
        var mockedProjectList = _scenarioContext.Get<PagedList<TeamProjectReference>>(MockedDevOpsProjects);

        var projectsCounts = AddaActivityGetAzureDevOpsProjects.AddUpdateProjectsToTable(mockedTableClient.Object, mockedProjectList);
        _scenarioContext.Set<(int added, int updated)>(projectsCounts, DevOpsProjectsCounts);
    }

    [When(@"I get the Iteration nodes for the project")]
    public async Task WhenIgetTheIterationPathesForTheProject()
    {
        var workItemsClientMoq = new Mock<WorkItemTrackingHttpClient>(null, null);
        workItemsClientMoq.Setup(w => w.GetClassificationNodeAsync(It.IsAny<Guid>(), It.IsAny<TreeStructureGroup>(),
                                    default, It.IsAny<int>(), default, default))
                        .ReturnsAsync(_scenarioContext.Get<WorkItemClassificationNode>(ProjectRootNode));

        var projectIterationNodes = await AddaActivityGetAzureDevOpsProjects
                                    .ListAllIterationNodesForProject(workItemsClientMoq.Object, new Guid(), 5);

        _scenarioContext.Set<IEnumerable<WorkItemClassificationNode>>(projectIterationNodes, ProjectNodes);
    }

    [When(@"I get all the Iteration paths for the project")]
    public async Task WhenIgetAllTheIterationPathesForTheProject()
    {
        var workItemsClientMoq = new Mock<WorkItemTrackingHttpClient>(null, null);
        workItemsClientMoq.Setup(w => w.GetClassificationNodeAsync(It.IsAny<Guid>(), It.IsAny<TreeStructureGroup>(),
                                    default, It.IsAny<int>(), default, default))
                        .ReturnsAsync(_scenarioContext.Get<WorkItemClassificationNode>(ProjectRootNode));

        var projectIterationPathes = await AddaActivityGetAzureDevOpsProjects
                                    .GetAllIterationPathsForProject(workItemsClientMoq.Object, new Guid(), 5);

        _scenarioContext.Set<IEnumerable<IEnumerable<string>>>(projectIterationPathes, ProjectIterationPaths);
    }

    [When(@"I get all the Iteration paths that ends with a node named (.*)")]
    public async Task WhenIgetAllTheIterationPathesEndingWithNode(string nodeName)
    {
        var workItemsClientMoq = new Mock<WorkItemTrackingHttpClient>(null, null);
        workItemsClientMoq.Setup(w => w.GetClassificationNodeAsync(It.IsAny<Guid>(), It.IsAny<TreeStructureGroup>(),
                                    default, It.IsAny<int>(), default, default))
                        .ReturnsAsync(_scenarioContext.Get<WorkItemClassificationNode>(ProjectRootNode));

        var projectIterationPathes = await AddaActivityGetAzureDevOpsProjects
                                    .GetAllIterationPathsForProjectEndingWithNode(workItemsClientMoq.Object, new Guid(), nodeName, 5);

        _scenarioContext.Set<IEnumerable<IEnumerable<string>>>(projectIterationPathes, ProjectIterationPaths);
    }

    [When(@"I check one of the nodes is named (.*)")]
    public async void WhenIcheckOneOfTheNodesIsNamed(string nodeName)
    {
        var workItemsClientMoq = new Mock<WorkItemTrackingHttpClient>(null, null);
        workItemsClientMoq.Setup(w => w.GetClassificationNodeAsync(It.IsAny<Guid>(), It.IsAny<TreeStructureGroup>(),
                                    default, It.IsAny<int>(), default, default))
                        .ReturnsAsync(_scenarioContext.Get<WorkItemClassificationNode>(ProjectRootNode));

        var hasNode = await AddaActivityGetAzureDevOpsProjects
                        .ProjectHasIterationNode(workItemsClientMoq.Object, new Guid(), nodeName, 5);

        _scenarioContext.Set<bool>(hasNode, Response);
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

    [Then(@"the project has the expected Iteration node (.*)")]
    public void ThenTheProjectHasTheExpectedIterationPath(string iterationNode)
    {
        Assert.IsTrue(_scenarioContext.Get<IEnumerable<WorkItemClassificationNode>>(ProjectNodes)
                        .Any(n => n.Name == iterationNode));
    }

    [Then(@"the project has (.*) Iteration paths")]
    public void ThenTheProjectHasIterationPathes(int countIterationPathes)
    {
        Assert.AreEqual(countIterationPathes, _scenarioContext.Get<IEnumerable<IEnumerable<string>>>(ProjectIterationPaths).Count());
    }

    [Then(@"the response is (.*)")]
    public void ThenTheResponseIs(bool expectedResponse)
    {
        Assert.AreEqual(expectedResponse, _scenarioContext.Get<bool>(Response));
    }

    #endregion
}