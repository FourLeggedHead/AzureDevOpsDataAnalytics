using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.Core.WebApi;
using ADDA.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace ADDA.Functions
{
    public static class AddaActivityGetProjects
    {
        [FunctionName(nameof(GetAzdoProjects))]
        public static IPagedList<TeamProjectReference> GetAzdoProjects([ActivityTrigger] AddaDevOpsOrganization organization, ILogger log)
        {
            log.LogInformation($"Getting projects for {organization.OrganizationUri.ToString()}.");
            return GetProjects(organization);
        }

        public static IPagedList<TeamProjectReference> GetProjects(AddaDevOpsOrganization organization)
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
    }
}
