using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace ADDA.Function
{
    public class OrchestratorTimerTrigger
    {
        [FunctionName("OrchestratorTimerTrigger")]
        public async Task Run([TimerTrigger("0 */5 * * * *")]TimerInfo myTimer,
        [DurableClient] IDurableOrchestrationClient starter, ILogger log)
        {
            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("AddaDurableFunctionsOrchestration", null);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");
        }
    }
}
