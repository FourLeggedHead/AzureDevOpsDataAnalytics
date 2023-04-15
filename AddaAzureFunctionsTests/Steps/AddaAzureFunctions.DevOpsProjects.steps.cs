namespace ADDA.Functions.Tests;

[Binding]
public class AddaAzureFunctionsDevOpsProjectsSteps
{
    private readonly ScenarioContext scenarioContext;
    private readonly FeatureContext featureContext;
    private readonly AddaDevOpsOrganization addaDevOpsOrganization;

    public AddaAzureFunctionsDevOpsProjectsSteps(ScenarioContext scenarioContext, FeatureContext featureContext)
    {
        this.scenarioContext = scenarioContext;
        this.featureContext = featureContext;
        addaDevOpsOrganization = new AddaDevOpsOrganization();
    }

    [Given(@"the Azure DevOps organization is (.*)")]
    public void GivenTheAzureDevOpsOrganizationIs(string organization)
    {
        addaDevOpsOrganization.GetOrganizationUri();
        StringAssert.Contains(addaDevOpsOrganization.OrganizationUri.ToString(), organization);
    }

    [When(@"I get the projects")]
    public void WhenGettingTheProjects()
    {
        addaDevOpsOrganization.GetCredential();
        scenarioContext.Pending();
    }

    [Then(@"the count of projects is at least (.*)")]
    public void ThenTheCountOfProjectsIsAtLeast(int count)
    {
        scenarioContext.Pending();
    }
}