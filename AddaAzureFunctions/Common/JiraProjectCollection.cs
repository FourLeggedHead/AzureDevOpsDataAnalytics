using System;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace ADDA.Common
{
    public class JiraProjectCollection : IProjectCollection<AuthenticationHeaderValue>
    {
        public Uri Uri { get; set; }

        public async Task<AuthenticationHeaderValue> GetCredential()
        {
            try
            {
                var keyValutUri = Environment.GetEnvironmentVariable("AddaKeyVaultUri");
                if (keyValutUri != null)
                {
                    var client = new SecretClient(new Uri(keyValutUri), new DefaultAzureCredential());
                    var username = await client.GetSecretAsync("JiraUsername");
                    var token = await client.GetSecretAsync("JiraApiToken");
                    return new AuthenticationHeaderValue("Basic",
                                        Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{username}:{token}")));
                }
                else
                {
                    
                    var username = Environment.GetEnvironmentVariable("JiraUsername");
                    var token = Environment.GetEnvironmentVariable("JiraApiToken");
                    return new AuthenticationHeaderValue("Basic",
                                        Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{username}:{token}")));
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Could not get credential for Jira project collection", ex);
            }
        }

        public void GetUri()
        {
            try
            {
                Uri = new Uri(Environment.GetEnvironmentVariable("JiraOrganizationUri"));
            }
            catch (Exception ex)
            {
                throw new Exception("Could not get Jira project collection Uri", ex);
            }
        }
    }
}