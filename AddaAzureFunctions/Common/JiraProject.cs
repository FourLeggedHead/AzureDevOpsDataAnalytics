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
    }

    public class PaginatedProjectsResponse
    {
        [JsonProperty("nextPage")]
        public string NextPage { get; set; }

        [JsonProperty("total")]
        public int TotalProjects { get; set; }

        [JsonProperty("isLast")]
        public bool IsLast { get; set; }
        
        [JsonProperty("values")]
        public List<JiraProject> Projects { get; set; }
    }
}