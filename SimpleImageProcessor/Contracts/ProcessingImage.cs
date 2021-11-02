namespace SimpleImageProcessor.Contracts
{
    public class ProcessingImage
    {
        public byte[] Contents { get; set; }

        public string FileName { get; set; }

        public string MimeType { get; set; }
    }
}
