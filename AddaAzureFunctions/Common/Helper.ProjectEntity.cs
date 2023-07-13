using System;
using Azure;
using Azure.Data.Tables;
using Microsoft.TeamFoundation.Core.WebApi;

namespace ADDA.Common.Helper
{
    public class ProjectEntity : ITableEntity
    {
        public const string DevOpsProjectPartitionKey = "AzureDevOpsProject";
        public const string JiraProjectPartitionKey = "JiraProject";

        public string Name { get; set; }
        public bool Selected { get; set; }
        public bool Deleted { get; set; }
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
        
        public ProjectEntity() { }

        public ProjectEntity(TeamProjectReference project)
        {
            Name = project.Name;
            PartitionKey = DevOpsProjectPartitionKey;
            RowKey = project.Id.ToString();
        }
    }
}