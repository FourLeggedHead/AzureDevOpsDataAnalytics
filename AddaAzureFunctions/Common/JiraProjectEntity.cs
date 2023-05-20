using System;
using Azure;
using Azure.Data.Tables;

namespace ADDA.Common
{
    public class JiraProjectEntity : ITableEntity
    {
        public const string JiraProjectPartitionKey = "JiraProject";

        public string Name { get; set; }
        public bool Selected { get; set; } = default;
        public bool Deleted { get; set; } = default;
        public string PartitionKey { get => JiraProjectPartitionKey; set => value = JiraProjectPartitionKey; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}