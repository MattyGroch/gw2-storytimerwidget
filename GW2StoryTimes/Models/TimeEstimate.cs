using Newtonsoft.Json;

namespace GW2StoryTimes.Models
{
    public class TimeEstimate
    {
        [JsonProperty("seed_mins")]
        public double? SeedMins { get; set; }

        [JsonProperty("avg_mins")]
        public double? AvgMins { get; set; }

        [JsonProperty("submissions")]
        public int Submissions { get; set; }

        [JsonProperty("min_mins")]
        public double? MinMins { get; set; }

        [JsonProperty("max_mins")]
        public double? MaxMins { get; set; }

        public bool HasCommunityData => Submissions > 0;

        public string FormattedEstimate
        {
            get
            {
                if (AvgMins == null) return "No estimate";

                var mins = AvgMins.Value;
                if (mins < 60) return $"~{mins:F0} min";

                var hours = (int)(mins / 60);
                var remaining = (int)(mins % 60);
                return remaining > 0 ? $"~{hours}h {remaining}m" : $"~{hours}h";
            }
        }

        public string FormattedRange
        {
            get
            {
                if (MinMins == null || MaxMins == null || !HasCommunityData) return null;
                return $"{FormatShort(MinMins.Value)}–{FormatShort(MaxMins.Value)}";
            }
        }

        private static string FormatShort(double mins)
        {
            if (mins < 60) return $"{mins:F0}m";
            var h = (int)(mins / 60);
            var m = (int)(mins % 60);
            return m > 0 ? $"{h}h{m}m" : $"{h}h";
        }
    }
}
