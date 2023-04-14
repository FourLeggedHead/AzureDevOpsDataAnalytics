using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using ADDA.Common;

namespace ADDA.Function
{
    public class OrchestratorTimerTrigger
    {
        [FunctionName("OrchestratorTimerTrigger")]
        public async Task Run([TimerTrigger("0 */5 * * * *")]TimerInfo myTimer,
        [DurableClient] IDurableOrchestrationClient starter, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            // Get the organization URI from the environment variable
            var addaDevOpsOrganization = new AddaDevOpsOrganization();
            addaDevOpsOrganization.GetOrganizationUri();
            addaDevOpsOrganization.GetCredential();
            log.LogInformation($"OrganizationUri: {addaDevOpsOrganization.OrganizationUri}");

            // Start the Azure Function orchestration
            string instanceId = await starter.StartNewAsync(
                            "AddaDurableFunctionsOrchestration", addaDevOpsOrganization);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");
        }
    }
}
