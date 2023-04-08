using System;

namespace ADDA.Common
{
    public class AddaDevOpsOrganization
    {
        public Uri OrganizationUri { get; set; }
        public string PersonalAccessToken { get; set; }

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
    }
}