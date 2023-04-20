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
        _featureContext["DevOpsOrganization"] = new AddaDevOpsOrganization();
        ((AddaDevOpsOrganization)_featureContext["DevOpsOrganization"]).GetOrganizationUri();
    }

    [Given(@"the Azure DevOps organization is (.*)")]
    public void GivenTheAzureDevOpsOrganizationIs(string organization)
    {
        StringAssert.Contains(((AddaDevOpsOrganization)_featureContext["DevOpsOrganization"]).OrganizationUri.ToString(), organization);
    }

    [When(@"I get the projects")]
    public void WhenGettingTheProjects()
    {
        var projects = AddaActivityGetProjects.GetProjects(_featureContext.Get<AddaDevOpsOrganization>());
        _scenarioContext["DevOpsProjects"] = projects;
    }

    [Then(@"the count of projects is at least (.*)")]
    public void ThenTheCountOfProjectsIsAtLeast(int count)
    {
        Assert.IsTrue(((IPagedList<TeamProjectReference>)_scenarioContext["DevOpsProjects"]).Count() >= count);
    }
}