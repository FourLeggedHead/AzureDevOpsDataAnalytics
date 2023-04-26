using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using Azure.Data.Tables;
using ADDA.Common;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.Work.WebApi;
using Microsoft.TeamFoundation.Core.WebApi.Types;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;

namespace ADDA.Functions
{

    public static class AddaActivityGetWorkItems
    {
        private static string[] WorkItemFields = new string[]
        {
            "System.ID",
            "System.Title",
            "System.State",
            "System.IterationPath",
            "System.AreaPath",
            "System.WorkItemType",
            "System.Parent",
            "Microsoft.VSTS.Common.ClosedDate",
            "Microsoft.VSTS.Scheduling.CompletedWork",
            "GlobalBI.Typeorprojectnr",
            "Custom.Capitalizable",
            "Custom.Category"
        };

        [FunctionName(nameof(GetAzdoWorkItems))]
        [StorageAccount("DevOpsDataStorageAppSetting")]
        public static async Task GetAzdoWorkItems([ActivityTrigger] IAddaDevOpsOrganization organization,
                            [Table("DevOpsProjects")] TableClient tableClient,
                            [Blob("bronze/tasks_{sys.utcnow}.json", FileAccess.Write)] Stream tasksData,
                            [Blob("bronze/pbis_{sys.utcnow}.json", FileAccess.Write)] Stream pbisData,
                            [Blob("bronze/features_{sys.utcnow}.json", FileAccess.Write)] Stream featuresData,
                            [Blob("bronze/epics_{sys.utcnow}.json", FileAccess.Write)] Stream epicsData, ILogger log)
        {
            log.LogInformation($"Getting the list of work items for project collection {organization.OrganizationUri}.");

            // List the DevOps projects selected to get the work items from
            var selectedProjects = tableClient.Query<DevOpsProject>(
                                                p => p.PartitionKey == DevOpsProject.DevOpsProjectPartitionKey && p.Selected == true)
                                                .Select(p => p.Name).ToList();

            // Terminates the function if no project is selected
            if (selectedProjects.Count == 0)
            {
                log.LogInformation($"No project selected for {organization.OrganizationUri}.");
                return;
            }

            log.LogInformation($"Selected projects: {string.Join(", ", selectedProjects)}.");

            organization.GetCredential();

            // Get done tasks from selected projects
            var iterationPath = "2023";

            log.LogInformation($"Fetch done tasks in selected projects, under iteration path {iterationPath}.");
            var doneTasks = await GetDoneTasks(organization, selectedProjects, iterationPath, log);
            log.LogInformation($"Listed {doneTasks.Count()} done Tasks.");
            await JsonSerializer.SerializeAsync(tasksData, doneTasks);
            log.LogInformation("Stored done tasks in Azure as json.");
        }

        // Verify that the project has the specified iteration path
        public static bool ProjectHasIterationPath(IAddaDevOpsOrganization organization, string project, string iterationPath)
        {
            try
            {
                var credential = organization.GetCredential();

                using (var workClient = new WorkHttpClient(organization.OrganizationUri, credential))
                {
                    var iterations = workClient.GetTeamIterationsAsync(new TeamContext(project)).Result;
                    return iterations.Any(i => i.Path.Contains(String.Concat(project, "\\", iterationPath)));
                }
            }
            catch (System.Exception ex)
            {
                throw new Exception($"Could not get iterations for project {project}", ex);
            }
        }

        // Query all the tasks in state Done that belong to the selected project and are under the specified iteration path
        public static async Task<IEnumerable<WorkItem>> GetDoneTasks(IAddaDevOpsOrganization organization, IEnumerable<string> projects, string iterationPath, ILogger log)
        {
            var workItemIds = new List<int>();

            foreach (var project in projects)
            {
                try
                {
                    if (ProjectHasIterationPath(organization, project, iterationPath) == false)
                    {
                        log.LogInformation($"Project {project} does not have iteration path {project}\\{iterationPath}.");
                        continue;
                    }

                    var wiql = new Wiql()
                    {
                        Query = "Select [Id] " +
                                    "From WorkItems " +
                                    "Where [Work Item Type] = 'Task' " +
                                    "And [State] = 'Done' " +
                                    $"And [Iteration Path] Under '{project}\\{iterationPath}' " +
                                    $"And [Team Project] = '{project}'"
                    };

                    workItemIds.AddRange(await QueryWorkItemsIds(organization, wiql));
                }
                catch (System.Exception ex)
                {
                    throw new Exception($"Could not get closed Tasks ids for project {project} under itration path {iterationPath}", ex);
                }
            }

            try
            {
                return await GetWorkItemsWithIdsAsync(organization, workItemIds).ConfigureAwait(false);
            }
            catch (System.Exception ex)
            {
                throw new Exception($"Could not get closed Tasks details", ex);
            }
        }

        /// Query the work item ids based on a Work Item query
        public static async Task<List<int>> QueryWorkItemsIds(IAddaDevOpsOrganization organization, Wiql query)
        {
            try
            {
                var credential = organization.GetCredential();

                using (var workItemsClient = new WorkItemTrackingHttpClient(organization.OrganizationUri, credential))
                {
                    var result = await workItemsClient.QueryByWiqlAsync(query);
                    return result.WorkItems.Select(item => item.Id).ToList();
                }
            }
            catch (VssException ex)
            {
                throw new Exception("Could not get Work Items.", ex);
            }
        }

        /// Get work items and chosen fields based on ids and a list of fields to retrieve
        /// Will slice the ids into batches of 200 to pass to the api
        /// The methods takes care of prefixing the fields with"System." 
        public static async Task<IList<WorkItem>> GetWorkItemsWithIdsAsync(IAddaDevOpsOrganization organization, ICollection<int> ids)
        {
            var windowedIds = ids.Batch(200);
            var workItemList = new List<WorkItem>();
            var credential = organization.GetCredential();

            using (var workItemsClient = new WorkItemTrackingHttpClient(organization.OrganizationUri, credential))
            {
                foreach (var idsWindow in windowedIds)
                {
                    workItemList.AddRange(await workItemsClient.GetWorkItemsAsync(idsWindow, WorkItemFields).ConfigureAwait(false));
                }
            }

            return workItemList;
        }
    }
}