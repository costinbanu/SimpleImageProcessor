using Domain.Contracts;
using SixLabors.ImageSharp;
using System.IO;
using System.Threading.Tasks;

namespace ImageEditingServices
{
    public interface IImageResizer
    {
        Resolution GetImageSize(Stream input);
		Task<ResizedImage?> ResizeImageByFileSize(Stream input, string fileName, double newSizeInBytes);
		Task<ResizedImage?> ResizeImageByResolution(Stream input, string fileName, int longestSideInPixels);
    }
}