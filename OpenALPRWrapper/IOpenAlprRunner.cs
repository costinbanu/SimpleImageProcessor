using System.IO;
using System.Threading.Tasks;

namespace OpenALPRWrapper
{
    public interface IOpenAlprRunner
    {
        Task<Stream> ProcessImage(Stream input, string fileName);
    }
}