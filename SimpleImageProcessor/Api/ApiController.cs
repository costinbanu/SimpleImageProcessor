using Microsoft.AspNetCore.Mvc;
using OpenALPRWrapper;
using Serilog;
using SimpleImageProcessor.Contracts;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace SimpleImageProcessor.Api
{
    [Route("api")]
    public class ApiController : Controller
    {
        private readonly IImageProcessor _imageProcessor;
        private readonly ILogger _logger;

        public ApiController(IImageProcessor imageProcessor, ILogger logger)
        {
            _imageProcessor = imageProcessor;
            _logger = logger;
        }

        [HttpPost]
        [Route("process-image")]
        public async Task<IActionResult> ProcessImage([FromBody] ProcessImageRequest request)
        {
            if (request is null)
            {
                return BadRequest();
            }

            if (request.SizeLimit is not null && request.SizeLimit < 0)
            {
                return BadRequest();
            }

            if (request.SizeLimit is not null && request.SizeLimit < Constants.OneMB)
            {
                request.SizeLimit = Constants.OneMB;
            }

            try
            {
                var result = await _imageProcessor.ProcessImage(request.Contents, request.FileName, request.HideLicensePlates, request.SizeLimit);
                return File(result, request.MimeType, request.FileName);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "api error");
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }
    }
}
