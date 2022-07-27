using Domain.Contracts;
using System.IO;

namespace ImageEditingServices
{
    public interface IImageResizer
    {
        Resolution GetImageSize(Stream input);
        ResizedImage? ResizeImage(Stream input, string filename, int longestSideInPixels);
        ResizedImage? ResizeImage(Stream input, string fileName, long newSizeInBytes);
    }
}