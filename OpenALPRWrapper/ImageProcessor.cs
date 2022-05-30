using Newtonsoft.Json;
using OpenALPRWrapper.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace OpenALPRWrapper
{
    public interface IImageProcessor
    {
        Task<Stream> ProcessImage(Stream input, string fileName, bool hideLicensePlates, long? sizeLimit);
        bool CanProcessFile(string fileName);
        IEnumerable<string> AllowedFormats { get; }
    }

    public class ImageProcessor : IImageProcessor
    {
        static readonly string _workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        static readonly Dictionary<string, ImageFormat> _imageFormats = new(StringComparer.InvariantCultureIgnoreCase)
        {
            [".bmp"] = ImageFormat.Bmp,
            [".gif"] = ImageFormat.Gif,
            [".ico"] = ImageFormat.Icon,
            [".jpg"] = ImageFormat.Jpeg,
            [".jpeg"] = ImageFormat.Jpeg,
            [".png"] = ImageFormat.Png,
        };

        public bool CanProcessFile(string fileName)
            => _imageFormats.ContainsKey(Path.GetExtension(fileName));

        public IEnumerable<string> AllowedFormats => _imageFormats.Keys;

        public async Task<Stream> ProcessImage(Stream input, string fileName, bool hideLicensePlates, long? sizeLimit)
        {
            var bitmap = new Bitmap(input);
            try
            {
                var licensePlatesWereHidden = false;
                var bitmapStream = GetBitmapStream(bitmap, fileName);
                double originalSize = input.Length;

                if (hideLicensePlates && CanProcessFile(fileName))
                {
                    NormalizeOrientation(bitmap);
                    input.Seek(0, SeekOrigin.Begin);
                    var processingResult = await RunAlpr(input, fileName);
                    foreach (var result in processingResult.Results)
                    {
                        using var graphics = Graphics.FromImage(bitmap);
                        var x = result.Coordinates.Min(p => p.X);
                        var y = result.Coordinates.Max(p => p.Y);
                        var height = Math.Abs(y - result.Coordinates.Min(p => p.Y));
                        var width = Math.Abs(result.Coordinates.Max(p => p.X) - x);
                        using var crop = bitmap.Clone(new Rectangle(x, y, width, height), bitmap.PixelFormat);
                        using var blurred = Blur(crop, new Rectangle(0, 0, crop.Width, crop.Height), 10);
                        using var myBrush = new TextureBrush(blurred);
                        graphics.FillPolygon(myBrush, result.Coordinates.Select(r => new Point(r.X, r.Y)).ToArray());
                        licensePlatesWereHidden = true;
                    }
                }

                if (licensePlatesWereHidden)
                {
                    bitmapStream = GetBitmapStream(bitmap, fileName);
                    originalSize = bitmapStream.Length;
                }
                else if (!sizeLimit.HasValue || originalSize <= sizeLimit.Value)
                {
                    input.Seek(0, SeekOrigin.Begin);
                    return input;
                }
                else
                {
                    originalSize = bitmapStream.Length;
                }

                for (var count = 0; sizeLimit.HasValue && originalSize > sizeLimit.Value && count < 5; count++)
                {
                    var scale = Math.Sqrt(sizeLimit.Value / originalSize) * (count == 0 ? 1d : 0.9 / count);
                    if (scale > 0 && scale < 1)
                    {
                        var scaleWidth = (int)(bitmap.Width * scale);
                        var scaleHeight = (int)(bitmap.Height * scale);
                        using var resizedBitmap = new Bitmap(scaleWidth, scaleHeight);
                        using var graph = Graphics.FromImage(resizedBitmap);
                        graph.InterpolationMode = InterpolationMode.High;
                        graph.CompositingQuality = CompositingQuality.HighQuality;
                        graph.SmoothingMode = SmoothingMode.AntiAlias;
                        graph.FillRectangle(new SolidBrush(Color.Black), new RectangleF(0, 0, scaleWidth, scaleHeight));
                        graph.DrawImage(bitmap, 0, 0, scaleWidth, scaleHeight);
                        foreach (var id in bitmap.PropertyIdList)
                        {
                            var item = bitmap.GetPropertyItem(id);
                            if (item is not null)
                            {
                                resizedBitmap.SetPropertyItem(item);
                            }
                        }
                        bitmapStream = GetBitmapStream(resizedBitmap, fileName);
                        originalSize = bitmapStream.Length;
                        bitmap = new Bitmap(resizedBitmap);
                    }
                }

                return bitmapStream;
            }
            finally
            {
                bitmap.Dispose();
            }
        }

        static Stream GetBitmapStream(Bitmap bitmap, string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            var output = new MemoryStream();
            bitmap.Save(output, GetImageFormatFromFileName(fileName));
            output.Seek(0, SeekOrigin.Begin);

            return output;
        }

        static async Task<ProcessingResult> RunAlpr(Stream file, string fileName)
        {
            var filePath = Path.Combine(_workingDirectory, $"{Guid.NewGuid():n}{Path.GetExtension(fileName)}");
            var fileInfo = new FileInfo(filePath);

            try
            {
                using (var fs = fileInfo.OpenWrite())
                {
                    await file.CopyToAsync(fs);
                }

                var processStartInfo = new ProcessStartInfo
                {
                    FileName = Path.Combine(_workingDirectory, "OpenALPR", "alpr.exe"),
                    WorkingDirectory = Path.Combine(_workingDirectory, "OpenALPR"),
                    Arguments = $"\"{filePath}\" -c \"eu\" -j",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                };
                using var process = Process.Start(processStartInfo);

                if (process is null)
                {
                    throw new NotSupportedException("No alpr.exe process has started");
                }

                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();

                process.WaitForExit(10000);

                await Task.WhenAll(outputTask, errorTask);

                var error = await errorTask;
                if (!string.IsNullOrWhiteSpace(error))
                {
                    throw new Exception(error);
                }

                return JsonConvert.DeserializeObject<ProcessingResult>(await outputTask) ?? new();
            }
            finally
            {
                fileInfo.Delete();
            }
        }

        static void NormalizeOrientation(Bitmap img)
        {
            if (Array.IndexOf(img.PropertyIdList, 274) > -1)
            {
                var orientation = (int?)img.GetPropertyItem(274)?.Value?[0];
                switch (orientation)
                {
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
                    default:
                        // Value is either 1 or null - No rotation required.
                        break;
                }
                img.RemovePropertyItem(274);
            }
        }

        unsafe static Bitmap Blur(Bitmap image, Rectangle rectangle, int blurSize)
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

                    for (var x = xx; x < xx + blurSize && x < image.Width; x++)
                    {
                        for (var y = yy; y < yy + blurSize && y < image.Height; y++)
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

        static ImageFormat GetImageFormatFromFileName(string fileName)
            => _imageFormats.TryGetValue(Path.GetExtension(fileName), out var format)
            ? format
            : throw new ArgumentOutOfRangeException(nameof(fileName), fileName, "Unknown extension");
    }
}
