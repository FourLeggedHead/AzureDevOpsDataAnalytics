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
        public static async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var outputs = new List<string>();

            // Orchestrate the activities
            var projects = await context.CallActivityAsync<PagedList<TeamProjectReference>>(nameof(AddaActivityGetProjects.GetAzdoProjects),
                                context.GetInput<AddaDevOpsOrganization>());
            outputs.AddRange(projects.Select(project => project.Name));

            await context.CallActivityAsync<PagedList<TeamProjectReference>>(nameof(AddaActivityGetWorkItems.GetAzdoWorkItems),
                                context.GetInput<AddaDevOpsOrganization>());

            return outputs;
        }
    }
}