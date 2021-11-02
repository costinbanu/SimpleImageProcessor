using Newtonsoft.Json;
using System.Collections.Generic;

namespace OpenALPRWrapper.Models
{
    public class ProcessingResult
    {
        [JsonProperty("version")]
        public int Version { get; set; }

        [JsonProperty("data_type")]
        public string DataType { get; set; }

        [JsonProperty("epoch_time")]
        public long EpochTime { get; set; }

        [JsonProperty("img_width")]
        public int ImgWidth { get; set; }

        [JsonProperty("img_height")]
        public int ImgHeight { get; set; }

        [JsonProperty("processing_time_ms")]
        public double ProcessingTimeMs { get; set; }

        [JsonProperty("regions_of_interest")]
        public List<object> RegionsOfInterest { get; set; }

        [JsonProperty("results")]
        public List<Result> Results { get; set; }
    }
}
