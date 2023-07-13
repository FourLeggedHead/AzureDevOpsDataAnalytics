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
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using ADDA.Common.Helper;

namespace ADDA.Functions
{

    public static class AddaActivityGetAzureDevOpsWorkItems
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
            "Microsoft.VSTS.Scheduling.CompletedWork"
        };

        [FunctionName(nameof(GetAzdoWorkItems))]
        [StorageAccount("DevOpsDataStorageAppSetting")]
        public static async Task GetAzdoWorkItems([ActivityTrigger] bool success,
                            [Table("DevOpsProjects")] TableClient tableClient,
                            [Blob("bronze/tasks/tasks_{sys.utcnow}.json", FileAccess.Write)] Stream tasksData,
                            [Blob("bronze/pbis/pbis_{sys.utcnow}.json", FileAccess.Write)] Stream pbisData,
                            [Blob("bronze/features/features_{sys.utcnow}.json", FileAccess.Write)] Stream featuresData,
                            [Blob("bronze/epics/epics_{sys.utcnow}.json", FileAccess.Write)] Stream epicsData, ILogger log)
        {
            log.LogInformation($"BACT Activity trigger function executed at: {DateTime.Now}");

            // Terminates the function if the previous activity failed
            if (!success)
            {
                log.LogInformation($"Update of DevOps projects list failed.");
                return;
            }

            // Initialize the project collection and get the uri
            var organization = new AddaDevOpsOrganization();
            try
            {
                organization.GetUri();
            }
            catch (Exception ex)
            {
                log.LogInformation(ex.Message);
                log.LogInformation(ex.InnerException.ToString());
            }

            log.LogInformation($"Getting the list of work items for project collection {organization.Uri}.");

            // List the DevOps projects selected to get the work items from
            var selectedProjects = tableClient.Query<ProjectEntity>(
                                                p => p.PartitionKey == ProjectEntity.DevOpsProjectPartitionKey && p.Selected == true)
                                                .Select(p => (Id: Guid.Parse(p.RowKey), Name: p.Name)).ToList();

            // Terminates the function if no project is selected
            if (selectedProjects.Count == 0)
            {
                log.LogInformation($"No project selected for {organization.Uri}.");
                return;
            }

            log.LogInformation($"Selected projects: {string.Join(", ", selectedProjects.Select(p => p.Name))}.");

            using var workItemsClient = new WorkItemTrackingHttpClient(organization.Uri, await organization.GetCredential());

            // Get done tasks from selected projects
            var iterationNode = "2023";

            log.LogInformation($"Fetch done tasks in selected projects, under iteration node(s) {iterationNode}.");
            var doneTasks = await GetDoneTasks(workItemsClient, selectedProjects, iterationNode, log);
            log.LogInformation($"Listed {doneTasks.Count()} done Tasks.");
            await JsonSerializer.SerializeAsync(tasksData, doneTasks);
            log.LogInformation("Stored done tasks in Azure as json.");

            log.LogInformation($"Fetch parent PBIs in selected projects, under iteration node(s) {iterationNode}.");
            var parentPbis = await GetParentWorkItems(workItemsClient, iterationNode, doneTasks).ConfigureAwait(false);
            log.LogInformation($"Listed {parentPbis.Count()} parent PBIs.");
            await JsonSerializer.SerializeAsync(pbisData, parentPbis);
            log.LogInformation("Stored parent PBIs in Azure as json.");

            log.LogInformation($"Fetch parent Features in selected projects, under iteration node(s) {iterationNode}.");
            var parentFeatures = await GetParentWorkItems(workItemsClient, iterationNode, parentPbis).ConfigureAwait(false);
            log.LogInformation($"Listed {parentFeatures.Count()} parent Features.");
            await JsonSerializer.SerializeAsync(featuresData, parentFeatures);
            log.LogInformation("Stored parent Features in Azure as json.");

            log.LogInformation($"Fetch parent Epics in selected projects, under iteration node(s) {iterationNode}.");
            var parentEpics = await GetParentWorkItems(workItemsClient, iterationNode, parentFeatures).ConfigureAwait(false);
            log.LogInformation($"Listed {parentEpics.Count()} parent Epics.");
            await JsonSerializer.SerializeAsync(epicsData, parentEpics);
            log.LogInformation("Stored parent Epics in Azure as json.");
        }

        // Query all the tasks in state Done that belong to the selected project and are under the specified iteration node
        public static async Task<IEnumerable<WorkItem>> GetDoneTasks(WorkItemTrackingHttpClient workItemsClient,
                                        IEnumerable<(Guid Id, string Name)> projects,
                                        string iterationNode, ILogger log)
        {
            var workItemIds = new List<int>();
            const int depth = 5;

            foreach (var project in projects)
            {
                try
                {
                    if (await AddaActivityGetAzureDevOpsProjects
                                .ProjectHasIterationNode(workItemsClient, project.Id, iterationNode, depth) == false)
                    {
                        log.LogInformation($"Project {project} does not have iteration node {iterationNode}.");
                        continue;
                    }

                    var iterationPaths = await AddaActivityGetAzureDevOpsProjects
                                .GetAllIterationPathsForProjectEndingWithNode(workItemsClient, project.Id, iterationNode, depth);

                    foreach (var iterationPath in iterationPaths)
                    {
                        var iterationPathString = String.Join("\\", iterationPath);

                        var wiql = new Wiql()
                        {
                            Query = "Select [Id] " +
                                        "From WorkItems " +
                                        "Where [Work Item Type] = 'Task' " +
                                        "And [State] In ('Closed', 'Done') " +
                                        $"And [Iteration Path] Under '{iterationPathString}' " +
                                        $"And [Team Project] = '{project.Name}'"
                        };

                        workItemIds.AddRange(await QueryWorkItemsIds(workItemsClient, wiql));
                    }
                }
                catch (System.Exception ex)
                {
                    throw new Exception($"Could not get closed Tasks ids for project {project} under itration node {iterationNode}", ex);
                }
            }

            try
            {
                return await GetWorkItemsWithIdsAsync(workItemsClient, workItemIds).ConfigureAwait(false);
            }
            catch (System.Exception ex)
            {
                throw new Exception($"Could not get closed Tasks details", ex);
            }
        }

        /// Query the work item ids based on a Work Item query
        public static async Task<List<int>> QueryWorkItemsIds(WorkItemTrackingHttpClient workItemsClient, Wiql query)
        {
            try
            {
                var result = await workItemsClient.QueryByWiqlAsync(query);
                return result.WorkItems.Select(item => item.Id).ToList();
            }
            catch (VssException ex)
            {
                throw new Exception("Could not get Work Items.", ex);
            }
        }

        /// Get work items and chosen fields based on ids and a list of fields to retrieve
        /// Will slice the ids into batches of 200 to pass to the api
        /// The methods takes care of prefixing the fields with"System." 
        public static async Task<IList<WorkItem>> GetWorkItemsWithIdsAsync(WorkItemTrackingHttpClient workItemsClient, ICollection<int> ids)
        {
            var windowedIds = ids.Batch(200);
            var workItemList = new List<WorkItem>();

            foreach (var idsWindow in windowedIds)
            {
                workItemList.AddRange(await workItemsClient.GetWorkItemsAsync(idsWindow, WorkItemFields).ConfigureAwait(false));
            }

            return workItemList;
        }

        // Query the parent work items for the list of work items provided
        public static async Task<IEnumerable<WorkItem>> GetParentWorkItems(WorkItemTrackingHttpClient workItemsClient, string iterationPath, IEnumerable<WorkItem> workItems)
        {
            try
            {
                var workItemIds = workItems.Where(w => w.Fields.ContainsKey("System.Parent"))
                                    .Select(w => Int32.Parse(w.Fields["System.Parent"].ToString())).Distinct().ToList();
                return await GetWorkItemsWithIdsAsync(workItemsClient, (ICollection<int>)workItemIds).ConfigureAwait(false);
            }
            catch (System.Exception ex)
            {
                throw new Exception($"Could not get parent work items under itration path {iterationPath}", ex);
            }
        }
    }
}