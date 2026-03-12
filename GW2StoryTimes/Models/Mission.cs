using System.Collections.Generic;
using Newtonsoft.Json;

namespace GW2StoryTimes.Models
{
    public class Mission
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("story_id")]
        public int StoryId { get; set; }

        [JsonProperty("story_name")]
        public string StoryName { get; set; }

        [JsonProperty("group_name")]
        public string GroupName { get; set; }

        [JsonProperty("season_id")]
        public string SeasonId { get; set; }

        [JsonProperty("season_name")]
        public string SeasonName { get; set; }

        [JsonProperty("order")]
        public int Order { get; set; }

        [JsonProperty("races")]
        public List<string> Races { get; set; }

        [JsonProperty("times")]
        public MissionTimes Times { get; set; }

        public string Breadcrumb => $"{SeasonName} > {StoryName}";
    }

    public class MissionTimes
    {
        [JsonProperty("full")]
        public TimeEstimate Full { get; set; }

        [JsonProperty("speed")]
        public TimeEstimate Speed { get; set; }
    }
}
