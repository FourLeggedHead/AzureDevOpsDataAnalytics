using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ADDA.Common.Helper
{
    public static class Jira
    {
        // Generic method to get Jira items from an API url
        public static async Task<IEnumerable<T>> GetItemsFromJiraAsync<T>(HttpClient client, string apiUrl)
        {
            HttpResponseMessage response = await client.GetAsync(apiUrl);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Error getting Jira items of type {typeof(T)} from {apiUrl}.");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var paginatedStringResponse = JsonConvert.DeserializeObject<JiraPaginatedResponse<T>>(responseContent);

            if (paginatedStringResponse.TotalItems == 0)
            {
                throw new Exception($"No Jira items returned from {apiUrl}.");
            }

            return await ListItemsFromJiraPaginatedResponseAsync(client, paginatedStringResponse);
        }

        // Generic method to get all items from a paginated response
        public static async Task<IEnumerable<T>> ListItemsFromJiraPaginatedResponseAsync<T>(HttpClient client, JiraPaginatedResponse<T> paginatedResponse)
        {
            var items = new List<T>();
            items.AddRange(paginatedResponse.Items);

            while (!paginatedResponse.IsLast)
            {
                var response = await client.GetAsync(paginatedResponse.NextPage);
                var responseContent = await response.Content.ReadAsStringAsync();
                paginatedResponse = JsonConvert.DeserializeObject<JiraPaginatedResponse<T>>(responseContent);

                items.AddRange(paginatedResponse.Items);
            }

            return items;
        }
    }
}