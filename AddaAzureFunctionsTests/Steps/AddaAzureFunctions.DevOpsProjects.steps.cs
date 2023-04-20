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

    #region Givens

    [Given(@"the Azure DevOps organization is (.*)")]
    public void GivenTheAzureDevOpsOrganizationIs(string organization)
    {
        StringAssert.Contains(((AddaDevOpsOrganization)_featureContext["DevOpsOrganization"]).OrganizationUri.ToString(), organization);
    }

    [Given(@"a list of Azure DevOps projects")]
    public void GivenListAzureDevOpsProjects()
    {
        _scenarioContext.Pending();
    }

    #endregion

    #region Whens

    [When(@"I get the projects")]
    public void WhenGettingTheProjects()
    {
        var projects = AddaActivityGetProjects.GetProjectsFromDevOps(_featureContext.Get<AddaDevOpsOrganization>());
        _scenarioContext["DevOpsProjects"] = projects;
    }

    #endregion

    #region Thens

    [Then(@"the count of projects is at least (.*)")]
    public void ThenTheCountOfProjectsIsAtLeast(int count)
    {
        Assert.IsTrue(((IPagedList<TeamProjectReference>)_scenarioContext["DevOpsProjects"]).Count() >= count);
    }

    #endregion
}