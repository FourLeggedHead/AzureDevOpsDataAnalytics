using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Azure.Data.Tables;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Helper;
using ADDA.Common;

namespace ADDA.Functions
{
    public static class AddaActivityGetProjects
    {
        [FunctionName(nameof(GetAzdoProjects))]
        [StorageAccount("DevOpsDataStorageAppSetting")]
        public static IPagedList<TeamProjectReference> GetAzdoProjects([ActivityTrigger] IAddaDevOpsOrganization organization,
                                                                        [Table("DevOpsProjectsData")] TableClient tableClient,
                                                                        ILogger log)
        {
            log.LogInformation($"Getting projects for {organization.OrganizationUri.ToString()}.");

            try
            {
                var projects = GetProjectsFromDevOps(organization);
                log.LogInformation($"{projects.Count} projects in the {organization.OrganizationUri.AbsoluteUri} organization.");

                var projectsCounts = AddUpdateProjectsToTable(tableClient, projects);
                log.LogInformation($"{projectsCounts.added} {(projectsCounts.added > 1 ? "projects" : "project")} were added.");
                log.LogInformation($"{projectsCounts.updated} {(projectsCounts.updated > 1 ? "projects" : "project")} were updated.");

                var deletedProjectCount = SoftDeleteProjectsFromTable(tableClient, projects);
                log.LogInformation($"{deletedProjectCount} {(deletedProjectCount > 1 ? "projects" : "project")} were deleted.");

                return projects;
            }
            catch (Exception ex)
            {
                log.LogInformation(ex.Message);
                log.LogInformation(ex.InnerException.ToString());

                return null;
            }
        }

        // List all of the Azure DevOps projects of the organization
        public static IPagedList<TeamProjectReference> GetProjectsFromDevOps(IAddaDevOpsOrganization organization)
        {
            try
            {
                var credential = organization.GetCredential();

                using (var projectClient = new ProjectHttpClient(organization.OrganizationUri, credential))
                {
                    return projectClient.GetProjects().Result;
                }
            }
            catch (System.Exception ex)
            {
                throw new Exception($"Could not get projects for {organization.OrganizationUri.ToString()}.", ex);
            }
        }

        #region List nodes

        // List all nodes from a classification node
        public static IEnumerable<WorkItemClassificationNode> ListAllNodesFromClassificationNode(WorkItemClassificationNode node)
        {
            return Tree.ListAllNodes(node, n => n.Children);
        }

        // List all nodes of a specified type for a project and given a preset depth
        public static async Task<IEnumerable<WorkItemClassificationNode>> ListAllNodesForProject(WorkItemTrackingHttpClient workItemClient,
                                Guid projectId, TreeStructureGroup type, int depth)
        {
            var iterationNode = await workItemClient.GetClassificationNodeAsync(projectId, type, depth: depth);
            return ListAllNodesFromClassificationNode(iterationNode);
        }

        // List all iteration nodes for a project and given a preset depth
        public static async Task<IEnumerable<WorkItemClassificationNode>> ListAllIterationNodesForProject(WorkItemTrackingHttpClient workItemClient,
                                Guid projectId, int depth)
        {
            return await ListAllNodesForProject(workItemClient, projectId, TreeStructureGroup.Iterations, depth);
        }

        // List all area nodes for a project and given a preset depth
        public static async Task<IEnumerable<WorkItemClassificationNode>> ListAllAreaNodesForProject(WorkItemTrackingHttpClient workItemClient,
                                Guid projectId, int depth)
        {
            return await ListAllNodesForProject(workItemClient, projectId, TreeStructureGroup.Areas, depth);
        }

        // Verify that the project has the specified node within a given depth
        public static async Task<bool> ProjectHasNode(WorkItemTrackingHttpClient workItemsClient,
                                    Guid project, TreeStructureGroup type, string node, int depth)
        {
            var nodes = await ListAllNodesForProject(workItemsClient, project, type, depth: depth);
            return nodes.Any(i => i.Name == node);
        }

        // Verify that the project has the specified iteration node within a given depth
        public static async Task<bool> ProjectHasIterationNode(WorkItemTrackingHttpClient workItemsClient, Guid project, string iterationNode, int depth)
        {
            return await ProjectHasNode(workItemsClient, project, TreeStructureGroup.Iterations, iterationNode, depth);
        }

        // Verify that the project has the specified area node within a given depth
        public static async Task<bool> ProjectHasAreaNode(WorkItemTrackingHttpClient workItemsClient, Guid project, string areaNode, int depth)
        {
            return await ProjectHasNode(workItemsClient, project, TreeStructureGroup.Areas, areaNode, depth);
        }

        #endregion

        #region Compute paths

        // Compute all paths from a classification node
        public static IEnumerable<IEnumerable<string>> ComputePathsFromClassificationNode(WorkItemClassificationNode node)
        {
            return Tree.ComputeAllPathsFromNode(node, n => n.Children).Select(p => p.Select(n => n.Name));
        }

        // Get all paths of a specified type for a project and given a preset depth
        public static async Task<IEnumerable<IEnumerable<string>>> GetAllPathsForProject(WorkItemTrackingHttpClient workItemClient,
                                Guid projectId, TreeStructureGroup type, int depth)
        {
            var node = await workItemClient.GetClassificationNodeAsync(projectId, type, depth: depth);
            return ComputePathsFromClassificationNode(node);
        }

        // Get all iteration paths for a project and given a preset depth
        public static async Task<IEnumerable<IEnumerable<string>>> GetAllIterationPathsForProject(WorkItemTrackingHttpClient workItemClient,
                                Guid projectId, int depth)
        {
            return await GetAllPathsForProject(workItemClient, projectId, TreeStructureGroup.Iterations, depth);
        }

        // Get all area paths for a project and given a preset depth
        public static async Task<IEnumerable<IEnumerable<string>>> GetAllAreaPathsForProject(WorkItemTrackingHttpClient workItemClient,
                                Guid projectId, int depth)
        {
            return await GetAllPathsForProject(workItemClient, projectId, TreeStructureGroup.Areas, depth);
        }

        // Get all the pathes for a project that end with the specified node
        public static async Task<IEnumerable<IEnumerable<string>>> GetAllPathsForProjectEndingWithNode(WorkItemTrackingHttpClient workItemClient,
                                Guid projectId, TreeStructureGroup type, string node, int depth)
        {
            var paths = await GetAllPathsForProject(workItemClient, projectId, type, depth);
            return paths.Where(p => p.Last().Equals(node));
        }

        // Get all the iteration pathes for a project that end with the specified node
        public static async Task<IEnumerable<IEnumerable<string>>> GetAllIterationPathsForProjectEndingWithNode(WorkItemTrackingHttpClient workItemClient,
                                Guid projectId, string node, int depth)
        {
            return await GetAllPathsForProjectEndingWithNode(workItemClient, projectId, TreeStructureGroup.Iterations, node, depth);
        }

        // Get all the area pathes for a project that end with the specified node
        public static async Task<IEnumerable<IEnumerable<string>>> GetAllAreaPathsForProjectEndingWithNode(WorkItemTrackingHttpClient workItemClient,
                                Guid projectId, string node, int depth)
        {
            return await GetAllPathsForProjectEndingWithNode(workItemClient, projectId, TreeStructureGroup.Areas, node, depth);
        }

        #endregion

        // Add or update the list of DevOps projects in the Azure table
        public static (int added, int updated) AddUpdateProjectsToTable(TableClient tableClient, IPagedList<TeamProjectReference> projects)
        {
            (int added, int updated) projectsCounts = (0, 0);

            foreach (var project in projects)
            {
                var response = tableClient.GetEntityIfExists<DevOpsProject>(DevOpsProject.DevOpsProjectPartitionKey, project.Id.ToString());

                if (!response.HasValue)
                {
                    var entity = new DevOpsProject()
                    {
                        Name = project.Name,
                        Selected = false,
                        Deleted = false,
                        PartitionKey = DevOpsProject.DevOpsProjectPartitionKey,
                        RowKey = project.Id.ToString()
                    };
                    var addedResponse = tableClient.AddEntity<DevOpsProject>(entity);

                    if (!addedResponse.IsError) projectsCounts.added++;
                }
                else
                {
                    if (!response.Value.Name.Equals(project.Name))
                    {
                        response.Value.Name = project.Name;
                        var updatedResponse = tableClient.UpdateEntity<DevOpsProject>(response.Value, Azure.ETag.All);

                        if (!updatedResponse.IsError) projectsCounts.updated++;
                    }
                }
            }

            return projectsCounts;
        }

        // Delete (soft) projects from the table when they no longer exist in DevOps and unselect them
        public static int SoftDeleteProjectsFromTable(TableClient tableClient, IPagedList<TeamProjectReference> projects)
        {
            var projectEntityRowKeys = tableClient.Query<DevOpsProject>(
                                                e => e.PartitionKey == DevOpsProject.DevOpsProjectPartitionKey, 20, new[] { "RowKey" });

            var deletedProjectsCount = 0;

            foreach (var projectEntityRowKey in projectEntityRowKeys)
            {
                if (!projects.Any(p => p.Id.ToString() == projectEntityRowKey.RowKey))
                {
                    var projectEntity = tableClient.GetEntity<DevOpsProject>(DevOpsProject.DevOpsProjectPartitionKey, projectEntityRowKey.RowKey);

                    projectEntity.Value.Deleted = true;
                    projectEntity.Value.Selected = false;
                    var deletedResponse = tableClient.UpdateEntity<DevOpsProject>(projectEntity, Azure.ETag.All);

                    if (!deletedResponse.IsError) deletedProjectsCount++;
                }
            }

            return deletedProjectsCount;
        }
    }
}
