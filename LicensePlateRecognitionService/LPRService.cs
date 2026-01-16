using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CSnakes.Runtime;
using Domain.Contracts;

namespace LicensePlateRecognitionService;

public class LPRService(IPythonEnvironment pythonEnvironment) : ILPRService
{
    public async Task<List<LicensePlateDetectionResult>> RecognizeLicensePlates(Stream image)
    {
        using var lpr = pythonEnvironment.Lpr();
        var filePath = Path.GetTempFileName();
        using var stream = File.Create(filePath);
        await image.CopyToAsync(stream);
        return JsonSerializer.Deserialize<List<LicensePlateDetectionResult>>(await lpr.Run([filePath])) ?? [];
    }
}