using System.IO;

namespace Domain.Contracts
{
    public class ResizedImage
    {
        public ResizedImage(Stream content, Resolution oldResolution, Resolution newResolution)
        {
            Content = content;
            OldResolution = oldResolution;
            NewResolution = newResolution;
        }

        public Stream Content { get; }
        public Resolution OldResolution { get; }
        public Resolution NewResolution { get; }
    }
}
