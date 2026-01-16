using System;
using System.IO;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace ImageEditingServices;

public class ImageBlurer
{
    public async Task BlurImage(Stream input)
    {
        var rectangle = new Rectangle(0, 0, 100, 100);
        using var image = await Image.LoadAsync(input);
        image.Mutate(ctx => ctx.GaussianBlur(10f, rectangle));
    }
}
