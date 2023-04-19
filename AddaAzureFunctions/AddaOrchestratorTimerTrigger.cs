using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using ADDA.Common;
using System.Net.Http;

namespace ADDA.Functions
{
    public class AddaOrchestratorTimerTrigger
    {
        [FunctionName("AddaOrchestratorTimerTrigger")]
        public async Task Run([TimerTrigger("0 */1 * * * *")]TimerInfo myTimer,
        [DurableClient] IDurableOrchestrationClient starter, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            try
            {
                // Get the organization URI from the environment variable
                var addaDevOpsOrganization = new AddaDevOpsOrganization();
                addaDevOpsOrganization.GetOrganizationUri();
                log.LogInformation($"OrganizationUri: {addaDevOpsOrganization.OrganizationUri}");
    
                // Start the Azure Function orchestration
                string instanceId = await starter.StartNewAsync(
                                "AddaDurableFunctionsOrchestration", addaDevOpsOrganization);
    
                log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

                var request = new HttpRequestMessage();
                var responseMessage = await starter.WaitForCompletionOrCreateCheckStatusResponseAsync(request, instanceId);
            }
            catch (System.Exception ex)
            {
                log.LogInformation($"Exception: {ex.Message}");
            }
        }
    }
}
