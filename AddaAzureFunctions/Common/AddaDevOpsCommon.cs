using System;
using Microsoft.VisualStudio.Services.Common;

namespace ADDA.Common
{
    public class AddaDevOpsOrganization
    {
        public Uri OrganizationUri { get; set; }
        public VssBasicCredential Credential { get; set; }

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

        public void GetCredential()
        {
            var environmentVariable = "AzureDevOpsPersonalAccessToken";
            var accessToken = GetEnvironmentVariable(environmentVariable);
            if (string.IsNullOrEmpty(accessToken))
            {
                throw new ArgumentNullException($"{environmentVariable} is null or empty");
            }
            Credential = new VssBasicCredential(string.Empty, accessToken);
        }
    }
}