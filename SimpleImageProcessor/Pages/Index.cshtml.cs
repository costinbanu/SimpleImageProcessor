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
                        int x = result.PlatePoints.Min(p => p.X), y = result.PlatePoints.Max(p => p.Y), 
                            height = Math.Abs(y - result.PlatePoints.Min(p => p.Y)), width = Math.Abs(result.PlatePoints.Max(p => p.X) - x);
                        using var crop = bitmap.Clone(new Rectangle(x, y, width, height), bitmap.PixelFormat);
                        using var blurred = Blur(crop, new Rectangle(0, 0, crop.Width, crop.Height), 10);
                        using var myBrush = new TextureBrush(blurred);
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

            switch (extension.ToLowerInvariant())
            {
                case ".bmp":
                    imageFormat = ImageFormat.Bmp;
                    break;
                case ".gif":
                    imageFormat = ImageFormat.Gif;
                    break;
                case ".ico":
                    imageFormat = ImageFormat.Icon;
                    break;
                case ".jpg":
                case ".jpeg":
                    imageFormat = ImageFormat.Jpeg;
                    break;
                case ".png":
                    imageFormat = ImageFormat.Png;
                    break;
                case ".tif":
                case ".tiff":
                    imageFormat = ImageFormat.Tiff;
                    break;
                case ".wmf":
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

        private void NormalizeOrientation(Bitmap img)
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

        private unsafe static Bitmap Blur(Bitmap image, Rectangle rectangle, Int32 blurSize)
        {
            var blurred = new Bitmap(image.Width, image.Height);

            using var graphics = Graphics.FromImage(blurred);
            graphics.DrawImage(image, new Rectangle(0, 0, image.Width, image.Height), new Rectangle(0, 0, image.Width, image.Height), GraphicsUnit.Pixel);

            var blurredData = blurred.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadWrite, blurred.PixelFormat);

            int bitsPerPixel = Image.GetPixelFormatSize(blurred.PixelFormat);

            var scan0 = (byte*)blurredData.Scan0.ToPointer();

            for (var xx = rectangle.X; xx < rectangle.X + rectangle.Width; xx++)
            {
                for (var yy = rectangle.Y; yy < rectangle.Y + rectangle.Height; yy++)
                {
                    int avgR = 0, avgG = 0, avgB = 0;
                    var blurPixelCount = 0;

                    for (var x = xx; (x < xx + blurSize && x < image.Width); x++)
                    {
                        for (var y = yy; (y < yy + blurSize && y < image.Height); y++)
                        {
                            var data = scan0 + y * blurredData.Stride + x * bitsPerPixel / 8;

                            avgB += data[0]; 
                            avgG += data[1]; 
                            avgR += data[2]; 

                            blurPixelCount++;
                        }
                    }

                    avgR /= blurPixelCount;
                    avgG /= blurPixelCount;
                    avgB /= blurPixelCount;

                    for (var x = xx; x < xx + blurSize && x < image.Width && x < rectangle.Width; x++)
                    {
                        for (var y = yy; y < yy + blurSize && y < image.Height && y < rectangle.Height; y++)
                        {
                            var data = scan0 + y * blurredData.Stride + x * bitsPerPixel / 8;

                            data[0] = (byte)avgB;
                            data[1] = (byte)avgG;
                            data[2] = (byte)avgR;
                        }
                    }
                }
            }

            blurred.UnlockBits(blurredData);
            return blurred;
        }
    }
}
