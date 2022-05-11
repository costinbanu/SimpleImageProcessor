using LazyCache;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenALPRWrapper;
using Serilog;
using SimpleImageProcessor.Contracts;
using System;
using System.Collections.Generic;
using System.Globalization;
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
        private readonly IImageProcessor _imageProcessor;

        [BindProperty]
        public IEnumerable<IFormFile> Files { get; set; } = Enumerable.Empty<IFormFile>();

        [BindProperty]
        public bool? HidePlates { get; set; }

        [BindProperty]
        public int? SizeLimit { get; set; }

        public IEnumerable<(Guid Id, string OriginalSize, string NewSize)> ProcessedImageIds { get; private set; } = Enumerable.Empty<(Guid, string, string)>();

        public IndexModel(ILogger logger, IAppCache cache, IImageProcessor imageProcessor)
        {
            _logger = logger;
            _cache = cache;
            _imageProcessor = imageProcessor;
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

            var badFiles = Files.Where(f => !f.ContentType.StartsWith("image"));
            if (badFiles.Any())
            {
                ModelState.AddModelError(nameof(Files), $"Următoarele fișiere nu sunt valide: {string.Join(',', badFiles.Select(f => f.FileName))} - toate fișierele trebuie să fie imagini.");
                hasErrors = true;
            }

            if (HidePlates is null)
            {
                ModelState.AddModelError(nameof(HidePlates), "Alege un tip de procesare");
                hasErrors = true;
            }

            if (SizeLimit is null)
            {
                ModelState.AddModelError(nameof(SizeLimit), "Alege dimensiunea");
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
                    using var result = await _imageProcessor.ProcessImage(input, file.FileName, HidePlates ?? false, (SizeLimit ?? 2) * Constants.OneMB);
                    var imageId = Guid.NewGuid();
                    using var ms = new MemoryStream();
                    await result.CopyToAsync(ms);
                    var img = new ProcessingImage
                    {
                        Contents = ms.GetBuffer(),
                        FileName = file.FileName,
                        MimeType = file.ContentType
                    };
                    _cache.Add(imageId.ToString(), img, TimeSpan.FromMinutes(1));
                    return (imageId, ReadableFileSize(file.Length), ReadableFileSize(result.Length));
                }));
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "web pages error");
                ModelState.AddModelError(nameof(Files), $"A intervenit o eroare, te rugăm să încerci mai târziu.");
            }
        }

        private string ReadableFileSize(long fileSizeInBytes)
        {
            var suf = new[] { "B", "KB", "MB", "GB", "TB", "PB", "EB" };
            if (fileSizeInBytes == 0)
            {
                return "0" + suf[0];
            }
            var bytes = Math.Abs(fileSizeInBytes);
            var place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            var num = Math.Round(bytes / Math.Pow(1024, place), 2);
            return $"{(Math.Sign(fileSizeInBytes) * num).ToString("##.##", CultureInfo.InvariantCulture)} {suf[place]}";
        }
    }
}
