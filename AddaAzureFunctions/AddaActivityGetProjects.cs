using System;
using System.Linq;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
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
