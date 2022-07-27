using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace Domain
{
    public static class ImageUtilities
    {
        public static Stream GetBitmapStream(Bitmap bitmap, string fileName)
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

        public static bool CanProcessFile(string fileName)
            => _imageFormats.ContainsKey(Path.GetExtension(fileName));

        public static IEnumerable<string> AllowedFormats => _imageFormats.Keys;

        static readonly Dictionary<string, ImageFormat> _imageFormats = new(StringComparer.InvariantCultureIgnoreCase)
        {
            [".bmp"] = ImageFormat.Bmp,
            [".gif"] = ImageFormat.Gif,
            [".ico"] = ImageFormat.Icon,
            [".jpg"] = ImageFormat.Jpeg,
            [".jpeg"] = ImageFormat.Jpeg,
            [".png"] = ImageFormat.Png,
        };

        static ImageFormat GetImageFormatFromFileName(string fileName)
            => _imageFormats.TryGetValue(Path.GetExtension(fileName), out var format)
            ? format
            : throw new ArgumentOutOfRangeException(nameof(fileName), fileName, "Unknown extension");
    }
}
