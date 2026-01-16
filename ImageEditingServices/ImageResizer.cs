using Domain.Contracts;

using System;

using SixLabors.ImageSharp;

using SixLabors.ImageSharp.Processing;

using System.IO;

using System.Threading.Tasks;

namespace ImageEditingServices
{
    class ImageResizer : IImageResizer
    {
        public Resolution GetImageSize(Stream input)
        {
            using var img = Image.Load(input);
            return new Resolution(img.Width, img.Height);
        }

        public async Task<ResizedImage?> ResizeImageByFileSize(Stream input, string fileName, double newSizeInBytes)
        {
            using var oldImage = await Image.LoadAsync(input);
            var oldResolution = new Resolution(oldImage.Width, oldImage.Height);
            double originalSize = input.Length;
            Stream? result = null;
            for (var count = 0; originalSize > newSizeInBytes && count < 5; count++)
            {
                var scale = Math.Sqrt(newSizeInBytes / originalSize) * (count == 0 ? 1d : 0.9 / count);
                if (scale > 0 && scale < 1)
                {
                    result = new MemoryStream();
                    await ResizeImageCore(scale, fileName, oldImage, result);
                    originalSize = result.Length;
                }
            }
            using var finalImage = result != null ? await Image.LoadAsync(result) : null;
            result?.Seek(0, SeekOrigin.Begin);
            return finalImage != null
                ? new ResizedImage(result!, oldResolution, new Resolution(finalImage.Width, finalImage.Height))
                : null;
        }

        public async Task<ResizedImage?> ResizeImageByResolution(Stream input, string fileName, int longestSideInPixels)
        {
            using var inputImage = await Image.LoadAsync(input);
            var scale = (double)longestSideInPixels / Math.Max(inputImage.Width, inputImage.Height);
            if (scale > 0 && scale < 1)
            {
                var oldResolution = new Resolution(inputImage.Width, inputImage.Height);
                var result = new MemoryStream();
                await ResizeImageCore(scale, fileName, inputImage, result);
                using var finalImage = await Image.LoadAsync(result);
                result.Seek(0, SeekOrigin.Begin);
                return new ResizedImage(result!, oldResolution, new Resolution(finalImage.Width, finalImage.Height));
            }
            return null;
        }

        static async Task ResizeImageCore(double scale, string fileName, Image image, Stream result)
        {
            image.Mutate(ctx => ctx.Resize((int)(image.Width * scale), (int)(image.Height * scale)));
            await image.SaveAsync(result, image.Metadata.DecodedImageFormat ?? throw new InvalidOperationException($"Unknown image format in file '{fileName}'."));
            result.Seek(0, SeekOrigin.Begin);
        }
    }
}
