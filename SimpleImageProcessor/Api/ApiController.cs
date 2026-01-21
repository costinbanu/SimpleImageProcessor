using Domain;
using Domain.Contracts;
using ImageEditingServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
        private readonly IImageResizer _imageResizer;
        //private readonly IOpenAlprRunner _openAlprRunner;
        private readonly ILogger<ApiController> _logger;
        private readonly IConfiguration _config;

        public ApiController(IImageResizer imageResizer, /*IOpenAlprRunner openAlprRunner,*/ ILogger<ApiController> logger, IConfiguration config)
        {
            _imageResizer = imageResizer;
            // _openAlprRunner = openAlprRunner;
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
                return BadRequest("Request body is missing.");
            }

            if (request.SizeLimit is not null && request.SizeLimit < 0)
            {
                return BadRequest($"Invalid value for {nameof(request.SizeLimit)}.");
            }

            if (request.SizeLimit is not null && request.SizeLimit < Constants.OneMB)
            {
                request.SizeLimit = Constants.OneMB;
                _logger.LogWarning($"Value for {nameof(request.SizeLimit)} normalized to 1MB because it was too low.");
            }

            if (request.ResolutionLimit is not null && request.ResolutionLimit < 0)
            {
                return BadRequest($"Invalid value for {nameof(request.ResolutionLimit)}");
            }

            if (request.SizeLimit is not null && request.ResolutionLimit is not null)
            {
                _logger.LogWarning($"Received request with both {nameof(request.SizeLimit)} and {nameof(request.ResolutionLimit)} set. {nameof(request.SizeLimit)} wins.");
            }

            try
            {
                Stream? output = null;
                var input = request.File.OpenReadStream();
                ResizedImage? resizeResult = null;

                // if (request.HideLicensePlates)
                // {
                //     output = await _openAlprRunner.ProcessImage(input, request.File.FileName);
                // }

                if (request.SizeLimit is not null)
                {
                    resizeResult = await _imageResizer.ResizeImageByFileSize(output ?? input, request.File.FileName, newSizeInBytes: request.SizeLimit.Value);
                }
                else if (request.ResolutionLimit is not null)
                {
                    resizeResult = await _imageResizer.ResizeImageByResolution(output ?? input, request.File.FileName, longestSideInPixels: request.ResolutionLimit.Value);
                }

                if (resizeResult is not null)
                {
                    output = resizeResult.Content;
                }

                return File(output ?? input, request.File.ContentType, request.File.FileName);
            }
            catch (Exception ex)
            {
                var id = Guid.NewGuid();
                _logger.LogError(ex, $"API exception id: {id:n}");
                return StatusCode((int)HttpStatusCode.InternalServerError, $"An API error occurred. ID: '{id:n}'");
            }
        }
    }
}
