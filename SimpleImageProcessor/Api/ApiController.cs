using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using OpenALPRWrapper;
using Serilog;
using SimpleImageProcessor.Contracts;
using System;
using System.Net;
using System.Threading.Tasks;

namespace SimpleImageProcessor.Api
{
    [Route("api")]
    public class ApiController : Controller
    {
        private readonly IImageProcessor _imageProcessor;
        private readonly ILogger _logger;
        private readonly IConfiguration _config;

        public ApiController(IImageProcessor imageProcessor, ILogger logger, IConfiguration config)
        {
            _imageProcessor = imageProcessor;
            _logger = logger;
            _config = config;
        }

        [HttpPost]
        [Route("process-image")]
        public async Task<IActionResult> ProcessImage([FromForm] ProcessImageRequest request)
        {
            if (!Request.Headers.TryGetValue("X-API-Key", out var key) || key.ToString() != _config.GetValue<string>("ApiKey"))
            {
                return Unauthorized();
            }

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
                var result = await _imageProcessor.ProcessImage(request.File.OpenReadStream(), request.File.FileName, request.HideLicensePlates, request.SizeLimit);
                return File(result, request.File.ContentType, request.File.FileName);
            }
            catch (Exception ex)
            {
                var id = Guid.NewGuid();
                _logger.Error(ex, $"API exception id: {id:n}");
                return StatusCode((int)HttpStatusCode.InternalServerError, $"An API error occurred: '{ex.Message}'. ID: '{id:n}'");
            }
        }
    }
}
