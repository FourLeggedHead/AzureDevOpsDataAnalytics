using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using ADDA.Common;

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
            outputs.Add(await context.CallActivityAsync<string>(nameof(SayHello), "Tokyo"));
            outputs.Add(await context.CallActivityAsync<string>(nameof(SayHello), "Seattle"));
            outputs.Add(await context.CallActivityAsync<string>(nameof(SayHello), "London"));
            outputs.Add(await context.CallActivityAsync<string>(nameof(SayHello),
                                context.GetInput<AddaDevOpsOrganization>().OrganizationUri.ToString()));
            outputs.Add(await context.CallActivityAsync<string>(nameof(SayHello),
                                context.GetInput<AddaDevOpsOrganization>().Credential.CredentialType.ToString()));

            return outputs;
        }

        [FunctionName(nameof(SayHello))]
        public static string SayHello([ActivityTrigger] string name, ILogger log)
        {
            log.LogInformation("Saying hello to {name}.", name);
            return $"Hello {name}!";
        }
    }
}