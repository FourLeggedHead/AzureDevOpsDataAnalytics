using System.Collections.Generic;
using Newtonsoft.Json;

namespace ADDA.Common
{
    public class JiraProject
    {
        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }
    }

    public class JiraPaginatedResponse<T>
    {
        [JsonProperty("nextPage")]
        public string NextPage { get; set; }

        [JsonProperty("total")]
        public int TotalItems { get; set; }

        [JsonProperty("isLast")]
        public bool IsLast { get; set; }
        
        [JsonProperty("values")]
        public List<T> Items { get; set; }
    }
}