using Microsoft.AspNetCore.Http;

namespace SimpleImageProcessor.Contracts
{
    public class ProcessImageRequest
    {
        public long? SizeLimit { get; set; }

        public bool HideLicensePlates { get; set; }

        public IFormFile File { get; set; }
    }
}
