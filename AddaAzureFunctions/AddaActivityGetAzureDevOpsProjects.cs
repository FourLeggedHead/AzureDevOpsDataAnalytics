﻿using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Azure.Data.Tables;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using ADDA.Common;
using ADDA.Common.Helper;

namespace ADDA.Functions
{
    public static class AddaActivityGetAzureDevOpsProjects
    {
        [FunctionName(nameof(GetAzdoProjects))]
        [StorageAccount("DevOpsDataStorageAppSetting")]
        public static bool GetAzdoProjects([ActivityTrigger] object trigger,
                                            [Table("DevOpsProjectsData")] TableClient tableClient,
                                            ILogger log)
        {
            log.LogInformation($"ADDA Activity trigger function {nameof(GetAzdoProjects)} executed at: {DateTime.Now}");

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

                return false;
            }

            log.LogInformation($"Getting projects for {organization.Uri.ToString()}.");

            try
            {
                // Get the list of projects from DevOps
                var projects = GetProjectsFromDevOps(organization).Result;
                log.LogInformation($"{projects.Count} projects in the {organization.Uri.AbsoluteUri} organization.");

                // Add or update the list of DevOps projects in the Azure table
                var projectsCounts = AzureTable.AddUpdateProjectsToTable(tableClient,
                                                            projects.Select(p => new ProjectInformations()
                                                                {
                                                                    Id = p.Id.ToString(),
                                                                    Name = p.Name
                                                                }),
                                                            ProjectEntity.DevOpsProjectPartitionKey);
                log.LogInformation($"{projectsCounts.added} {(projectsCounts.added > 1 ? "projects" : "project")} were added.");
                log.LogInformation($"{projectsCounts.updated} {(projectsCounts.updated > 1 ? "projects" : "project")} were updated.");

                // Delete (soft) projects from the table when they no longer exist in DevOps and unselect them
                var deletedProjectCount = AzureTable.SoftDeleteProjectsFromTable(tableClient,
                                                            projects.Select(p => new ProjectInformations()
                                                                {
                                                                    Id = p.Id.ToString(),
                                                                    Name = p.Name
                                                                }),
                                                            ProjectEntity.DevOpsProjectPartitionKey);
                log.LogInformation($"{deletedProjectCount} {(deletedProjectCount > 1 ? "projects" : "project")} were deleted.");

                return true;
            }
            catch (Exception ex)
            {
                log.LogInformation(ex.Message);
                log.LogInformation(ex.InnerException.ToString());

                return false;
            }
        }

        // List all of the Azure DevOps projects of the organization
        public static async Task<IPagedList<TeamProjectReference>> GetProjectsFromDevOps(IProjectCollection<VssBasicCredential> organization)
        {
            try
            {
                using (var projectClient = new ProjectHttpClient(organization.Uri, await organization.GetCredential()))
                {
                    return projectClient.GetProjects().Result;
                }
            }
            catch (System.Exception ex)
            {
                throw new Exception($"Could not get projects for {organization.Uri.ToString()}.", ex);
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
    }
}
