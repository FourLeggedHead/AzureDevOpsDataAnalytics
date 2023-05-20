using System;
using Azure.Data.Tables;
using Azure;
using Microsoft.TeamFoundation.Core.WebApi;

namespace ADDA.Common
{
    // Class to hold project info
    public class DevOpsProject : ITableEntity
    {
        public const string DevOpsProjectPartitionKey = "AzureDevOpsProject";

        public string Name { get; set; }
        public bool Selected { get; set; } = default;
        public bool Deleted { get; set; } = default;
        public DateTimeOffset? Timestamp { get; set; }
        public string PartitionKey { get => DevOpsProjectPartitionKey; set => value = DevOpsProjectPartitionKey; }
        public string RowKey { get; set; }
        public ETag ETag { get; set; }

        public DevOpsProject() { }

        public DevOpsProject(TeamProjectReference project)
        {
            Name = project.Name;
            PartitionKey = DevOpsProjectPartitionKey;
            RowKey = project.Id.ToString();
        }
    }
}