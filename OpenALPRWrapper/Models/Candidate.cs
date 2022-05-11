using Newtonsoft.Json;

namespace OpenALPRWrapper.Models
{
    public class Candidate
    {
        [JsonProperty("plate")]
        public string Plate { get; set; } = string.Empty;

        [JsonProperty("confidence")]
        public double Confidence { get; set; }

        [JsonProperty("matches_template")]
        public int MatchesTemplate { get; set; }
    }
}
