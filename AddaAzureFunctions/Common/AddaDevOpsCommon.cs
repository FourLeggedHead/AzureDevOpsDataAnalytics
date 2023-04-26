using System;
using Azure;
using Azure.Data.Tables;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.VisualStudio.Services.Common;

namespace ADDA.Common
{
    public interface IAddaDevOpsOrganization
    {
        Uri OrganizationUri { get; set; }
        public void GetOrganizationUri();
        public VssBasicCredential GetCredential();
    }

    public class AddaDevOpsOrganization : IAddaDevOpsOrganization
    {
        public Uri OrganizationUri { get; set; }

        public static string GetEnvironmentVariable(string name)
        {
            return Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
        }

        public void GetOrganizationUri()
        {
            var environmentVariable = "AzureDevOpsOrganizationUri";
            var organizationUri = GetEnvironmentVariable(environmentVariable);
            if (string.IsNullOrEmpty(organizationUri))
            {
                throw new ArgumentNullException($"{environmentVariable} is null or empty");
            }
            OrganizationUri = new Uri(organizationUri);
        }

        public VssBasicCredential GetCredential()
        {
            var environmentVariable = "AzureDevOpsPersonalAccessToken";
            var accessToken = GetEnvironmentVariable(environmentVariable);
            if (string.IsNullOrEmpty(accessToken))
            {
                throw new ArgumentNullException($"{environmentVariable} is null or empty");
            }
            return new VssBasicCredential(string.Empty, accessToken);
        }
    }

    // Class to hold project info
    public class DevOpsProject : ITableEntity
    {
        public const string DevOpsProjectPartitionKey = "AzureDevOpsProject";

        public string Name { get; set; }
        public bool Selected { get; set; } = default;
        public bool Deleted { get; set; } = default;
        public DateTimeOffset? Timestamp { get; set; }
        public string PartitionKey { get; set; }
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