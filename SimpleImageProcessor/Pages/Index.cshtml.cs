using Domain;
using Domain.Contracts;
using ImageEditingServices;
using LazyCache;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenALPRWrapper;
using Serilog;
using SimpleImageProcessor.Contracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleImageProcessor.Pages
{
    [ValidateAntiForgeryToken]
    public class IndexModel : PageModel
    {
        private readonly ILogger _logger;
        private readonly IAppCache _cache;
        private readonly IImageResizer _imageResizer;
        private readonly IOpenAlprRunner _openAlprRunner;

        [BindProperty]
        public IEnumerable<IFormFile> Files { get; set; } = Enumerable.Empty<IFormFile>();

        [BindProperty]
        public bool HidePlates { get; set; } = true;

        [BindProperty]
        public int? SizeLimitInBytes { get; set; }

        [BindProperty]
        public int? SizeLimitInPixels { get; set; }

        [BindProperty]
        public ResizeMode ResizeMode { get; set; } = ResizeMode.InMB;

        public IEnumerable<ProcessedImageMetadata> ProcessedImageIds { get; private set; } = Enumerable.Empty<ProcessedImageMetadata>();

        public IndexModel(ILogger logger, IAppCache cache, IImageResizer imageResizer, IOpenAlprRunner openAlprRunner)
        {
            _logger = logger;
            _cache = cache;
            _imageResizer = imageResizer;
            _openAlprRunner = openAlprRunner;
        }

        public void OnGet()
        {

        }

        public async Task OnPost()
        {
            var hasErrors = false;
            var count = Files.Count();

            if (count < 1 || count > 10)
            {
                ModelState.AddModelError(nameof(Files), "Minim 1, maxim 10 imagini!");
                hasErrors = true;
            }

            var badFiles = Files.Where(f => !ImageUtilities.CanProcessFile(f.FileName));
            if (badFiles.Any())
            {
                ModelState.AddModelError(nameof(Files), $"Următoarele fișiere nu pot fi procesate: {string.Join(", ", badFiles.Select(f => f.FileName))}. Numai următoarele formate sunt acceptate: {string.Join(", ", ImageUtilities.AllowedFormats)}");
                hasErrors = true;
            }

            if (ResizeMode != ResizeMode.None && SizeLimitInBytes is null && SizeLimitInPixels is null)
            {
                ModelState.AddModelError(nameof(ResizeMode), "Alege dimensiunea dorită pentru redimensionare!");
                hasErrors = true;
            }

            if (ResizeMode == ResizeMode.None && !HidePlates)
            {
                ModelState.AddModelError(nameof(ResizeMode), "Nici o acțiune selectată!");
                hasErrors = true;
            }
   
            if (hasErrors)
            {
                return;
            }
            
            try
            {
                ProcessedImageIds = await Task.WhenAll(Files.Select(async file => 
                {
                    using var input = file.OpenReadStream();
                    Stream? output = null;
                    Resolution? oldResolution, newResolution;
                    ResizedImage? resizeResult = null;

                    if (HidePlates == true)
                    {
                        output = await _openAlprRunner.ProcessImage(input, file.FileName);
                    }

                    if (SizeLimitInBytes.HasValue)
                    {
                        resizeResult = _imageResizer.ResizeImage(output ?? input, file.FileName, newSizeInBytes: SizeLimitInBytes.Value * Constants.OneMB);
                    }
                    else if (SizeLimitInPixels.HasValue)
                    {
                        resizeResult = _imageResizer.ResizeImage(output ?? input, file.FileName, longestSideInPixels: SizeLimitInPixels.Value);
                    }

                    if (resizeResult is not null)
                    {
                        output = resizeResult.Content;
                        oldResolution = resizeResult.OldResolution;
                        newResolution = resizeResult.NewResolution;
                    }
                    else
                    {
                        oldResolution = newResolution = _imageResizer.GetImageSize(input);
                    }
                    output ??= input;

                    var imageId = Guid.NewGuid();
                    using var ms = new MemoryStream();
                    await output.CopyToAsync(ms);
                    var img = new ProcessedImage
                    {
                        Contents = ms.GetBuffer(),
                        FileName = file.FileName,
                        MimeType = file.ContentType
                    };
                    _cache.Add(imageId.ToString(), img, TimeSpan.FromMinutes(1));
                    return new ProcessedImageMetadata(imageId, file.ContentType, oldResolution, newResolution, input.Length, output.Length);
                }));
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "web pages error");
                ModelState.AddModelError(nameof(Files), $"A intervenit o eroare, te rugăm să încerci mai târziu.");
            }
        }
    }
}
