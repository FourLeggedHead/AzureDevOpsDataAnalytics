using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using Azure.Data.Tables;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ADDA.Common;

namespace ADDA.Functions
{
    public static class AddaActivityGetJiraProjects
    {
        [FunctionName(nameof(GetJiraProjects))]
        [StorageAccount("DevOpsDataStorageAppSetting")]
        public static async Task<bool> GetJiraProjects([ActivityTrigger] object trigger,
                                [Table("DevOpsProjects")] TableClient tableClient,
                                ILogger log)
        {
            log.LogInformation($"ADDA Activity trigger {nameof(GetJiraProjects)} function executed at: {DateTime.Now}");

            // Initialize the project collection and get the uri
            var projectCollection = new JiraProjectCollection();
            try
            {
                projectCollection.GetUri();
            }
            catch (Exception ex)
            {
                log.LogInformation(ex.Message);
                log.LogInformation(ex.InnerException.ToString());

                return false;
            }

            log.LogInformation($"Getting the list of Jira projects for project collection {projectCollection.Uri}.");

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = await projectCollection.GetCredential();
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                string apiUrl = $"{projectCollection.Uri}/rest/api/3/project/search";
                apiUrl = string.Concat(apiUrl, "?", "orderBy=+name");

                HttpResponseMessage response = await client.GetAsync(apiUrl);

                if (!response.IsSuccessStatusCode)
                {
                    log.LogInformation($"Error getting Jira projects for project collection {projectCollection.Uri}.");
                    return false;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var paginatedStringResponse = JsonConvert.DeserializeObject<PaginatedProjectsResponse>(responseContent);

                if (paginatedStringResponse.Projects.Count == 0)
                {
                    log.LogInformation($"No Jira projects found for project collection {projectCollection.Uri}.");
                    return false;
                }

                var jiraProject = new List<JiraProject>();
                jiraProject.AddRange(paginatedStringResponse.Projects);

                while (!paginatedStringResponse.IsLast)
                {
                    response = await client.GetAsync(paginatedStringResponse.NextPage);
                    responseContent = await response.Content.ReadAsStringAsync();
                    paginatedStringResponse = JsonConvert.DeserializeObject<PaginatedProjectsResponse>(responseContent);

                    jiraProject.AddRange(paginatedStringResponse.Projects);
                }
            }

            return true;
        }
    }
}