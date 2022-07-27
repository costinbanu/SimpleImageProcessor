using Microsoft.AspNetCore.Http;
using System.IO;

namespace SimpleImageProcessor.Contracts
{
    public class ProcessImageRequest
    {
        public long? SizeLimit { get; set; }

        public int? ResolutionLimit { get; set; }

        public bool HideLicensePlates { get; set; }

        public IFormFile File { get; set; } = new FormFile(new MemoryStream(), 0, 0, string.Empty, string.Empty);
    }
}
