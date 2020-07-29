using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Distributed;
using openalprnet;
using Serilog;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleImageProcessor.Pages
{
    public class IndexModel : PageModel
    {
        const double _2MB = 2 * 1024 * 1024;

        private readonly ILogger _logger;
        private readonly IDistributedCache _cache;

        [BindProperty]
        public IEnumerable<IFormFile> Files { get; set; }

        public Guid SessionId { get; set; }

        public int Count { get; set; }

        public IndexModel(ILogger logger, IDistributedCache cache)
        {
            _logger = logger;
            _cache = cache;
            Count = 0;
        }

        public void OnGet()
        {
            
        }

        public async Task OnPost()
        {
            if ((Files?.Count() ?? 0) > 10)
            {
                ModelState.AddModelError(nameof(Files), "Maxim 10 fișiere!");
                return;
            }

            var badFiles = Files.Where(f => !f.ContentType.StartsWith("image"));
            if (badFiles.Any())
            {
                ModelState.AddModelError(nameof(Files), $"Următoarele fișiere nu sunt valide: {string.Join(',', badFiles.Select(f => f.FileName))}");
                return;
            }

            try
            {
                SessionId = Guid.NewGuid();
                var processedImages = new List<ProcessedImage>();
                foreach (var file in Files)
                {
                    using var alpr = new AlprNet("eu", "openalpr.conf", "RuntimeData");
                    if (!alpr.IsLoaded())
                    {
                        ModelState.AddModelError(nameof(Files), "A intervenit o eroare, te rugăm să încerci mai târziu.");
                        return;
                    }

                    using var input = new MemoryStream();
                    file.OpenReadStream().CopyTo(input);
                    using var bitmap = new Bitmap(input);
                    var results = alpr.Recognize(input.ToArray());
                    foreach (var result in results.Plates)
                    {
                        using var graphics = Graphics.FromImage(bitmap);
                        using var myBrush = new SolidBrush(Color.WhiteSmoke);
                        graphics.FillPolygon(myBrush, result.PlatePoints.ToArray());
                    }

                    if (file.Length > _2MB)
                    {
                        var scale = Math.Sqrt(_2MB / file.Length);
                        var scaleWidth = (int)(bitmap.Width * scale);
                        var scaleHeight = (int)(bitmap.Height * scale);
                        using var bmp = new Bitmap(scaleWidth, scaleHeight);
                        using var graph = Graphics.FromImage(bmp);
                        graph.InterpolationMode = InterpolationMode.High;
                        graph.CompositingQuality = CompositingQuality.HighQuality;
                        graph.SmoothingMode = SmoothingMode.AntiAlias;
                        graph.FillRectangle(new SolidBrush(Color.Black), new RectangleF(0, 0, scaleWidth, scaleHeight));
                        graph.DrawImage(bitmap, 0, 0, scaleWidth, scaleHeight);
                        await SaveBitmap(bmp, file.FileName, file.ContentType);
                    }
                    else
                    {
                        await SaveBitmap(bitmap, file.FileName, file.ContentType);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "error");
                ModelState.AddModelError(nameof(Files), $"A intervenit o eroare, te rugăm să încerci mai târziu: {ex.Message}");
            }
        }

        private async Task SaveBitmap(Bitmap bitmap, string fileName, string contentType)
        {
            var extension = Path.GetExtension(fileName);
            var exception = new ArgumentException($"Unable to determine file extension for fileName: {fileName}", nameof(fileName));
            ImageFormat imageFormat;

            if (string.IsNullOrEmpty(extension))
            {
                throw exception;
            }

            switch (extension.ToLower())
            {
                case @".bmp":
                    imageFormat = ImageFormat.Bmp;
                    break;
                case @".gif":
                    imageFormat = ImageFormat.Gif;
                    break;
                case @".ico":
                    imageFormat = ImageFormat.Icon;
                    break;
                case @".jpg":
                case @".jpeg":
                    imageFormat = ImageFormat.Jpeg;
                    break;
                case @".png":
                    imageFormat = ImageFormat.Png;
                    break;
                case @".tif":
                case @".tiff":
                    imageFormat = ImageFormat.Tiff;
                    break;
                case @".wmf":
                    imageFormat = ImageFormat.Wmf;
                    break;
                default:
                    throw exception;
            }

            using var output = new MemoryStream();
            bitmap.Save(output, imageFormat);
            var options = new DistributedCacheEntryOptions { SlidingExpiration = TimeSpan.FromHours(3) };
            await _cache.SetAsync($"{SessionId}_file_{Count}", output.GetBuffer(), options);
            await _cache.SetStringAsync($"{SessionId}_contentType_{Count}", contentType, options);
            await _cache.SetStringAsync($"{SessionId}_fileName_{Count++}", fileName, options);
        }
    }
}
