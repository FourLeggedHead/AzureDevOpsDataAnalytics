namespace ADDA.Functions.Tests;

[Binding]
public class AddaAzureFunctionsDevOpsProjectsSteps
{
    private readonly ScenarioContext _scenarioContext;
    private readonly FeatureContext _featureContext;

    public AddaAzureFunctionsDevOpsProjectsSteps(ScenarioContext scenarioContext, FeatureContext featureContext)
    {
        _scenarioContext = scenarioContext;
        _featureContext = featureContext;
        _featureContext.Set(new AddaDevOpsOrganization());
        _featureContext.Get<AddaDevOpsOrganization>().GetOrganizationUri();
    }

    [Given(@"the Azure DevOps organization is (.*)")]
    public void GivenTheAzureDevOpsOrganizationIs(string organization)
    {
        StringAssert.Contains(_featureContext.Get<AddaDevOpsOrganization>().OrganizationUri.ToString(), organization);
    }

    [When(@"I get the projects")]
    public void WhenGettingTheProjects()
    {
        var projects = AddaActivityGetProjects.GetProjects(_featureContext.Get<AddaDevOpsOrganization>());
        _scenarioContext.Set(projects);
    }

    [Then(@"the count of projects is at least (.*)")]
    public void ThenTheCountOfProjectsIsAtLeast(int count)
    {
        Assert.IsTrue(_scenarioContext.Get<IPagedList<TeamProjectReference>>().Count() >= count);
    }
}