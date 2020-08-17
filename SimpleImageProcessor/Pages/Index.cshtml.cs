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

        [BindProperty]
        public bool? HidePlates { get; set; }

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
            var hasErrors = false;

            if ((Files?.Count() ?? 0) == 0 || (Files?.Count() ?? 0) > 10)
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

            if (HidePlates == null)
            {
                ModelState.AddModelError(nameof(HidePlates), "Alege un tip de procesare");
                hasErrors = true;
            }

            if (hasErrors)
            {
                return;
            }

            SessionId = Guid.NewGuid();
            var tasks = Files.Select(async file =>
            {
                using var input = new MemoryStream();
                file.OpenReadStream().CopyTo(input);
                using var bitmap = new Bitmap(input);
                
                if (HidePlates ?? false)
                {
                    using var alpr = new AlprNet("eu", "openalpr.conf", "RuntimeData");
                    if (!alpr.IsLoaded())
                    {
                        ModelState.AddModelError(nameof(Files), "A intervenit o eroare, te rugăm să încerci mai târziu.");
                        return;
                    }

                    NormalizeOrientation(bitmap);
                    using var ms = new MemoryStream();
                    bitmap.Save(ms, ImageFormat.Jpeg);

                    input.Seek(0, SeekOrigin.Begin);
                    var results = alpr.Recognize(input.ToArray());
                    foreach (var result in results.Plates)
                    {
                        using var graphics = Graphics.FromImage(bitmap);
                        using var myBrush = new SolidBrush(Color.WhiteSmoke);
                        graphics.FillPolygon(myBrush, result.PlatePoints.ToArray());
                    }
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
                    foreach (var id in bitmap.PropertyIdList)
                    {
                        bmp.SetPropertyItem(bitmap.GetPropertyItem(id));
                    }
                    await SaveBitmap(bmp, file.FileName, file.ContentType);
                }
                else
                {
                    await SaveBitmap(bitmap, file.FileName, file.ContentType);
                }
            }); 
            
            try
            {
                await Task.WhenAll(tasks);
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

        void NormalizeOrientation(Bitmap img)
        {
            if (Array.IndexOf(img.PropertyIdList, 274) > -1)
            {
                var orientation = (int)img.GetPropertyItem(274).Value[0];
                switch (orientation)
                {
                    case 1:
                        // No rotation required.
                        break;
                    case 2:
                        img.RotateFlip(RotateFlipType.RotateNoneFlipX);
                        break;
                    case 3:
                        img.RotateFlip(RotateFlipType.Rotate180FlipNone);
                        break;
                    case 4:
                        img.RotateFlip(RotateFlipType.Rotate180FlipX);
                        break;
                    case 5:
                        img.RotateFlip(RotateFlipType.Rotate90FlipX);
                        break;
                    case 6:
                        img.RotateFlip(RotateFlipType.Rotate90FlipNone);
                        break;
                    case 7:
                        img.RotateFlip(RotateFlipType.Rotate270FlipX);
                        break;
                    case 8:
                        img.RotateFlip(RotateFlipType.Rotate270FlipNone);
                        break;
                }
                img.RemovePropertyItem(274);
            }
        }
    }
}
