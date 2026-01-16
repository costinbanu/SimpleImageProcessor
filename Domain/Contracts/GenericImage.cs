using System;
using System.IO;

namespace Domain.Contracts;

public class GenericImage : IDisposable
{
    public GenericImage(Stream content, string fileName, string mimeType)
    {
        Content = content;
        FileName = fileName;
        MimeType = mimeType;
    }

    public Stream Content { get; }
    public string FileName { get; }
    public string MimeType { get; }

    public void Dispose()
    {
        Content?.Dispose();
    }
}
