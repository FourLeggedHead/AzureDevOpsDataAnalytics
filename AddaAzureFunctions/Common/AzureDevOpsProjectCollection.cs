using System;
using System.Threading.Tasks;
using Azure.Identity;
using Microsoft.VisualStudio.Services.Common;

namespace ADDA.Common
{
    // Class to hold project collection info
    public class AddaDevOpsOrganization : IProjectCollection<VssBasicCredential>
    {
        public Uri Uri { get; set; }

        public void GetUri()
        {
            var environmentVariable = "AzureDevOpsOrganizationUri";
            var organizationUri = Environment.GetEnvironmentVariable(environmentVariable);
            if (string.IsNullOrEmpty(organizationUri))
            {
                throw new ArgumentNullException($"{environmentVariable} is null or empty");
            }
            Uri = new Uri(organizationUri);
        }

        public Task<VssBasicCredential> GetCredential()
        {
            var environmentVariable = "AzureDevOpsPersonalAccessToken";
            var accessToken = Environment.GetEnvironmentVariable(environmentVariable);
            if (string.IsNullOrEmpty(accessToken))
            {
                throw new ArgumentNullException($"{environmentVariable} is null or empty");
            }
            return Task.FromResult(new VssBasicCredential(string.Empty, accessToken));
        }
    }
}