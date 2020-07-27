using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using openalprnet;
using Serilog;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace SimpleImageProcessor.Pages
{
    public class IndexModel : PageModel
    {
        const int TWO_MB = 2 * 1024 * 1024;

        private readonly ILogger _logger;

        [BindProperty]
        public IEnumerable<IFormFile> Files { get; set; }

        public List<(string mimeType, string contents)> ProcessedFiles { get; set; }

        public IndexModel(ILogger logger)
        {
            _logger = logger;
            ProcessedFiles = new List<(string, string)>();
        }

        public void OnGet()
        {

        }

        public void OnPost()
        {
            if ((Files?.Count() ?? 0) > 10)
            {
                ModelState.AddModelError(nameof(Files), "Maxim 10 fișiere!");
                return;
            }

            try
            {
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

                    if (file.Length > TWO_MB)
                    {
                        var scale = (float)TWO_MB / file.Length;
                        var scaleWidth = (int)(bitmap.Width * scale);
                        var scaleHeight = (int)(bitmap.Height * scale);
                        using var bmp = new Bitmap(scaleWidth, scaleHeight);
                        using var graph = Graphics.FromImage(bmp);
                        graph.InterpolationMode = InterpolationMode.High;
                        graph.CompositingQuality = CompositingQuality.HighQuality;
                        graph.SmoothingMode = SmoothingMode.AntiAlias;
                        graph.FillRectangle(new SolidBrush(Color.Black), new RectangleF(0, 0, scaleWidth, scaleHeight));
                        graph.DrawImage(bitmap, 0, 0, scaleWidth, scaleHeight);
                        SaveBitmap(bmp, file.FileName, file.ContentType);
                    }
                    else
                    {
                        SaveBitmap(bitmap, file.FileName, file.ContentType);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "error");
                ModelState.AddModelError(nameof(Files), $"A intervenit o eroare, te rugăm să încerci mai târziu: {ex.Message}");
            }
        }

        private void SaveBitmap(Bitmap bitmap, string fileName, string contentType)
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
            ProcessedFiles.Add((contentType, Convert.ToBase64String(output.ToArray())));
        }
    }
}
