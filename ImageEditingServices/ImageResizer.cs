using Domain;
using Domain.Contracts;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;

namespace ImageEditingServices
{
    public class ImageResizer : IImageResizer
    {
        public Resolution GetImageSize(Stream input)
        {
            using var bitmap = new Bitmap(input);
            input.Seek(0, SeekOrigin.Begin);
            return new Resolution(bitmap.Width, bitmap.Height);
        }

        public ResizedImage? ResizeImage(Stream input, string fileName, long newSizeInBytes)
        {
            var bitmap = new Bitmap(input);
            int oldWidth = bitmap.Width, oldHeight = bitmap.Height, newWidth = 0, newHeight = 0;
            try
            {
                double originalSize = input.Length;
                Stream? result = null;
                for (var count = 0; originalSize > newSizeInBytes && count < 5; count++)
                {
                    var scale = Math.Sqrt(newSizeInBytes / originalSize) * (count == 0 ? 1d : 0.9 / count);
                    if (scale > 0 && scale < 1)
                    {
                        using var resizedBitmap = ResizeImageCore(bitmap, (int)(bitmap.Width * scale), (int)(bitmap.Height * scale));
                        result = ImageUtilities.GetBitmapStream(resizedBitmap, fileName);
                        originalSize = result.Length;
                        bitmap = new Bitmap(resizedBitmap);
                        newWidth = resizedBitmap.Width;
                        newHeight = resizedBitmap.Height;
                    }
                }
                input.Seek(0, SeekOrigin.Begin);
                if (result is null)
                {
                    return null;
                }
                return new ResizedImage(
                    content: result, 
                    oldResolution: new Resolution(oldWidth, oldHeight), 
                    newResolution: new Resolution(newWidth, newHeight));
            }
            finally
            {
                bitmap.Dispose();
            }
        }

        public ResizedImage? ResizeImage(Stream input, string filename, int longestSideInPixels)
        {
            var bitmap = new Bitmap(input);
            try
            {
                var scale = (double)longestSideInPixels / Math.Max(bitmap.Width, bitmap.Height);
                input.Seek(0, SeekOrigin.Begin);
                if (scale > 0 && scale < 1)
                {
                    using var resizedBitmap = ResizeImageCore(bitmap, (int)(bitmap.Width * scale), (int)(bitmap.Height * scale));
                    return new ResizedImage(
                        content: ImageUtilities.GetBitmapStream(resizedBitmap, filename), 
                        oldResolution: new Resolution(bitmap.Width, bitmap.Height), 
                        newResolution: new Resolution(resizedBitmap.Width, resizedBitmap.Height));
                }
                return null;
            }
            finally
            {
                bitmap.Dispose();
            }
        }

        static Bitmap ResizeImageCore(Bitmap input, int newWidth, int newHeight)
        {
            var resizedBitmap = new Bitmap(newWidth, newHeight);
            using var graph = Graphics.FromImage(resizedBitmap);
            graph.InterpolationMode = InterpolationMode.High;
            graph.CompositingQuality = CompositingQuality.HighQuality;
            graph.SmoothingMode = SmoothingMode.AntiAlias;
            graph.FillRectangle(new SolidBrush(Color.Black), new RectangleF(0, 0, newWidth, newHeight));
            graph.DrawImage(input, 0, 0, newWidth, newHeight);
            foreach (var id in input.PropertyIdList)
            {
                var item = input.GetPropertyItem(id);
                if (item is not null)
                {
                    resizedBitmap.SetPropertyItem(item);
                }
            }
            return resizedBitmap;
        }
    }
}
