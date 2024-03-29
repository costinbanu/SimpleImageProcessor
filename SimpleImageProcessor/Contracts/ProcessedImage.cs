﻿namespace SimpleImageProcessor.Contracts
{
    public class ProcessedImage
    {
        public byte[] Contents { get; set; } = new byte[0];

        public string FileName { get; set; } = string.Empty;

        public string MimeType { get; set; } = string.Empty;
    }
}
