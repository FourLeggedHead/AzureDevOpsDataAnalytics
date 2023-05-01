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
            "Microsoft.VSTS.Scheduling.CompletedWork"
        };

        [FunctionName(nameof(GetAzdoWorkItems))]
        [StorageAccount("DevOpsDataStorageAppSetting")]
        public static async Task GetAzdoWorkItems([ActivityTrigger] IAddaDevOpsOrganization organization,
                            [Table("DevOpsProjects")] TableClient tableClient,
                            [Blob("bronze/tasks/tasks_{sys.utcnow}.json", FileAccess.Write)] Stream tasksData,
                            [Blob("bronze/pbis/pbis_{sys.utcnow}.json", FileAccess.Write)] Stream pbisData,
                            [Blob("bronze/features/features_{sys.utcnow}.json", FileAccess.Write)] Stream featuresData,
                            [Blob("bronze/epics/epics_{sys.utcnow}.json", FileAccess.Write)] Stream epicsData, ILogger log)
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

            var credential = organization.GetCredential();
            using var workClient = new WorkHttpClient(organization.OrganizationUri, credential);
            using var workItemsClient = new WorkItemTrackingHttpClient(organization.OrganizationUri, credential);

            // Get done tasks from selected projects
            var iterationPath = "2023";

            log.LogInformation($"Fetch done tasks in selected projects, under iteration path {iterationPath}.");
            var doneTasks = await GetDoneTasks(workClient, workItemsClient, organization, selectedProjects, iterationPath, log);
            log.LogInformation($"Listed {doneTasks.Count()} done Tasks.");
            await JsonSerializer.SerializeAsync(tasksData, doneTasks);
            log.LogInformation("Stored done tasks in Azure as json.");

            log.LogInformation($"Fetch parent PBIs in selected projects, under iteration path {iterationPath}.");
            var parentPbis = await GetParentWorkItems(workItemsClient, iterationPath, doneTasks).ConfigureAwait(false);
            log.LogInformation($"Listed {parentPbis.Count()} parent PBIs.");
            await JsonSerializer.SerializeAsync(pbisData, parentPbis);
            log.LogInformation("Stored parent PBIs in Azure as json.");

            log.LogInformation($"Fetch parent Features in selected projects, under iteration path {iterationPath}.");
            var parentFeatures = await GetParentWorkItems(workItemsClient, iterationPath, parentPbis).ConfigureAwait(false);
            log.LogInformation($"Listed {parentFeatures.Count()} parent Features.");
            await JsonSerializer.SerializeAsync(featuresData, parentFeatures);
            log.LogInformation("Stored parent Features in Azure as json.");

            log.LogInformation($"Fetch parent Epics in selected projects, under iteration path {iterationPath}.");
            var parentEpics = await GetParentWorkItems(workItemsClient, iterationPath, parentFeatures).ConfigureAwait(false);
            log.LogInformation($"Listed {parentEpics.Count()} parent Epics.");
            await JsonSerializer.SerializeAsync(epicsData, parentEpics);
            log.LogInformation("Stored parent Epics in Azure as json.");
        }

        // Verify that the project has the specified iteration path
        public static async Task<bool> ProjectHasIterationPath(WorkHttpClient workClient, string project, string iterationPath)
        {
            try
            {
                var iterations = await workClient.GetTeamIterationsAsync(new TeamContext(project));
                return iterations.Any(i => i.Path.Contains(String.Concat(project, "\\", iterationPath)));
            }
            catch (Exception ex)
            {
                throw new Exception($"Could not get iterations for project {project}", ex);
            }
        }

        // Query all the tasks in state Done that belong to the selected project and are under the specified iteration path
        public static async Task<IEnumerable<WorkItem>> GetDoneTasks(WorkHttpClient workClient, WorkItemTrackingHttpClient workItemsClient,
                                        IAddaDevOpsOrganization organization, IEnumerable<string> projects, string iterationPath, ILogger log)
        {
            var workItemIds = new List<int>();

            foreach (var project in projects)
            {
                try
                {
                    if (await ProjectHasIterationPath(workClient, project, iterationPath) == false)
                    {
                        log.LogInformation($"Project {project} does not have iteration path {project}\\{iterationPath}.");
                        continue;
                    }

                    var wiql = new Wiql()
                    {
                        Query = "Select [Id] " +
                                    "From WorkItems " +
                                    "Where [Work Item Type] = 'Task' " +
                                    "And [State] In ('Closed', 'Done') " +
                                    $"And [Iteration Path] Under '{project}\\{iterationPath}' " +
                                    $"And [Team Project] = '{project}'"
                    };

                    workItemIds.AddRange(await QueryWorkItemsIds(workItemsClient, wiql));
                }
                catch (System.Exception ex)
                {
                    throw new Exception($"Could not get closed Tasks ids for project {project} under itration path {iterationPath}", ex);
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
                var workItemIds = workItems.Select(w => Int32.Parse(w.Fields["System.Parent"].ToString())).Distinct().ToList();
                return await GetWorkItemsWithIdsAsync(workItemsClient, (ICollection<int>)workItemIds).ConfigureAwait(false);
            }
            catch (System.Exception ex)
            {
                throw new Exception($"Could not get parent work items under itration path {iterationPath}", ex);
            }
        }
    }
}