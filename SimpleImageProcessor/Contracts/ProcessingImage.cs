namespace SimpleImageProcessor.Contracts
{
    public class ProcessingImage
    {
        public byte[] Contents { get; set; } = new byte[0];

        public string FileName { get; set; } = string.Empty;

        public string MimeType { get; set; } = string.Empty;
    }
}
