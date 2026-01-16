using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Domain.Contracts;

namespace LicensePlateRecognitionService;

public interface ILPRService
{
    Task<List<LicensePlateDetectionResult>> RecognizeLicensePlates(Stream image);
}
