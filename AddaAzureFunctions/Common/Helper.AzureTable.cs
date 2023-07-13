using System.Collections.Generic;
using System.Linq;
using Azure.Data.Tables;

namespace ADDA.Common.Helper
{
    public static class AzureTable
    {
        // Add or update the list of projects in the Azure table
        public static (int added, int updated) AddUpdateProjectsToTable(TableClient tableClient, IEnumerable<ProjectInformations> projects, string projectsSource)
        {
            (int added, int updated) projectsCounts = (0, 0);

            foreach (var project in projects)
            {
                var response = tableClient.GetEntityIfExists<ProjectEntity>(projectsSource, project.Id.ToString());

                if (!response.HasValue)
                {
                    var entity = new ProjectEntity()
                    {
                        Name = project.Name,
                        Selected = false,
                        Deleted = false,
                        PartitionKey = projectsSource,
                        RowKey = project.Id.ToString()
                    };
                    var addedResponse = tableClient.AddEntity<ProjectEntity>(entity);

                    if (!addedResponse.IsError) projectsCounts.added++;
                }
                else
                {
                    if (!response.Value.Name.Equals(project.Name))
                    {
                        response.Value.Name = project.Name;
                        var updatedResponse = tableClient.UpdateEntity<ProjectEntity>(response.Value, Azure.ETag.All);

                        if (!updatedResponse.IsError) projectsCounts.updated++;
                    }
                }
            }

            return projectsCounts;
        }

        // Delete (soft) projects from the table when they no longer exist and unselect them
        public static int SoftDeleteProjectsFromTable(TableClient tableClient, IEnumerable<ProjectInformations> projects, string projectsSource)
        {
            var projectEntityRowKeys = tableClient.Query<ProjectEntity>(
                                                e => e.PartitionKey == projectsSource, 20, new[] { "RowKey" });

            var deletedProjectsCount = 0;

            foreach (var projectEntityRowKey in projectEntityRowKeys)
            {
                if (!projects.Any(p => p.Id.ToString() == projectEntityRowKey.RowKey))
                {
                    var projectEntity = tableClient.GetEntity<ProjectEntity>(projectsSource, projectEntityRowKey.RowKey);

                    projectEntity.Value.Deleted = true;
                    projectEntity.Value.Selected = false;
                    var deletedResponse = tableClient.UpdateEntity<ProjectEntity>(projectEntity, Azure.ETag.All);

                    if (!deletedResponse.IsError) deletedProjectsCount++;
                }
            }

            return deletedProjectsCount;
        }
    }
}