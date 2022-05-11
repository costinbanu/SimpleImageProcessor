using Newtonsoft.Json;
using System.Collections.Generic;

namespace OpenALPRWrapper.Models
{
    public class Result
    {
        [JsonProperty("plate")]
        public string Plate { get; set; } = string.Empty;

        [JsonProperty("confidence")]
        public double Confidence { get; set; }

        [JsonProperty("matches_template")]
        public int MatchesTemplate { get; set; }

        [JsonProperty("plate_index")]
        public int PlateIndex { get; set; }

        [JsonProperty("region")]
        public string Region { get; set; } = string.Empty;

        [JsonProperty("region_confidence")]
        public int RegionConfidence { get; set; }

        [JsonProperty("processing_time_ms")]
        public double ProcessingTimeMs { get; set; }

        [JsonProperty("requested_topn")]
        public int RequestedTopn { get; set; }

        [JsonProperty("coordinates")]
        public List<Coordinate> Coordinates { get; set; } = new List<Coordinate> { };

        [JsonProperty("candidates")]
        public List<Candidate> Candidates { get; set; } = new List<Candidate> { };
    }
}
