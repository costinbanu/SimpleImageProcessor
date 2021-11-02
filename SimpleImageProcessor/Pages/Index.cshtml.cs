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
    public class IndexModel : PageModel
    {
        private readonly ILogger _logger;
        private readonly IAppCache _cache;
        private readonly IImageProcessor _imageProcessor;

        [BindProperty]
        public IEnumerable<IFormFile> Files { get; set; }

        [BindProperty]
        public bool? HidePlates { get; set; }

        [BindProperty]
        public int? SizeLimit { get; set; }

        public IEnumerable<(Guid Id, string OriginalSize, string NewSize)> ProcessedImageIds { get; private set; }

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

            if ((Files?.Count() ?? 0) < 1 || (Files?.Count() ?? 0) > 10)
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
                    using var ms = new MemoryStream();
                    await file.CopyToAsync(ms);
                    var result = await _imageProcessor.ProcessImage(ms.GetBuffer(), file.FileName, HidePlates ?? false, (SizeLimit ?? 2) * Constants.OneMB);
                    var imageId = Guid.NewGuid();
                    var img = new ProcessingImage
                    {
                        Contents = result,
                        FileName = file.FileName,
                        MimeType = file.ContentType
                    };
                    _cache.Add(imageId.ToString(), img, TimeSpan.FromMinutes(1));
                    return (imageId, ReadableFileSize(file.Length), ReadableFileSize(result.LongLength));
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
