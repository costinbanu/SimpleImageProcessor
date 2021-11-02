using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleImageProcessor.Contracts
{
    public class ProcessImageRequest : ProcessingImage
    {
        public long? SizeLimit { get; set; }
        public bool HideLicensePlates { get; set; }
    }
}
