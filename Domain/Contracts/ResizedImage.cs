using System;
using System.IO;

namespace Domain.Contracts
{
    public class ResizedImage : IDisposable
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

        public void Dispose()
        {
            Content?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
