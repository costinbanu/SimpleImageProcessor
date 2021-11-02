using Newtonsoft.Json;

namespace OpenALPRWrapper.Models
{
    public class Coordinate
    {
        [JsonProperty("x")]
        public int X { get; set; }

        [JsonProperty("y")]
        public int Y { get; set; }
    }
}
