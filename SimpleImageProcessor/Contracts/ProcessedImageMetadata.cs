using System;
using System.Globalization;

namespace SimpleImageProcessor.Contracts
{
    public class ProcessedImageMetadata
    {
        public ProcessedImageMetadata(Guid id, string mimeType, int width, int height, long originalFileSize, long newFileSize)
        {
            Id = id;
            MimeType = mimeType;
            Width = width;
            Height = height;
            OriginalSize = ReadableFileSize(originalFileSize);
            NewSize = ReadableFileSize(newFileSize);
        }

        public Guid Id { get; }

        public string OriginalSize { get; }

        public string NewSize { get; }

        public string MimeType { get; }

        public int Width { get; }

        public int Height { get; }

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
