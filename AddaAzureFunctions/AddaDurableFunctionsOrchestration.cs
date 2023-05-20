using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.TeamFoundation.Core.WebApi;
using ADDA.Common;
using System.Linq;

namespace ADDA.Functions
{
    public static class AddaDurableFunctionsOrchestration
    {
        [FunctionName("AddaDurableFunctionsOrchestration")]
        public static async Task RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            // Orchestrate the activities
            var successAzdo = await context.CallActivityAsync<bool>(nameof(AddaActivityGetAzureDevOpsProjects.GetAzdoProjects), null);
            await context.CallActivityAsync<PagedList<TeamProjectReference>>(nameof(AddaActivityGetAzureDevOpsWorkItems.GetAzdoWorkItems), successAzdo);

            var successJira = await context.CallActivityAsync<bool>(nameof(AddaActivityGetJiraProjects.GetJiraProjects), null);
        }
    }
}