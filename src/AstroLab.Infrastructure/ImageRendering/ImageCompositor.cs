using AstroLab.Core.Result;

namespace AstroLab.Infrastructure.ImageRendering;

/// <summary>
/// Combines three independently-scaled grayscale channel buffers into one RGB
/// <see cref="RenderedImage"/> by interleaving them directly into the R, G, and B byte slots. The
/// per-channel scientific scaling (auto black/white point, stretch) is Core's job
/// (<see cref="AstroLab.Core.Imaging.ImageScaler"/>); this class only performs the concrete visual
/// byte layout, which is why it lives alongside <see cref="FitsImageRenderer"/> in Infrastructure.
/// </summary>
public static class ImageCompositor
{
    private const int RgbChannelCount = 3;

    public static Result<RenderedImage> Compose(ReadOnlySpan<byte> red, ReadOnlySpan<byte> green, ReadOnlySpan<byte> blue, int width, int height)
    {
        var expectedLength = width * height;

        if (width <= 0 || height <= 0 || red.Length != expectedLength || green.Length != expectedLength || blue.Length != expectedLength)
        {
            return Error.Validation(
                "rendering.composite.channel_length_mismatch",
                $"Each channel buffer must have length width x height ({expectedLength}); got red={red.Length}, green={green.Length}, blue={blue.Length}.");
        }

        var rgb = new byte[expectedLength * RgbChannelCount];

        for (var i = 0; i < expectedLength; i++)
        {
            var offset = i * RgbChannelCount;

            rgb[offset] = red[i];

            rgb[offset + 1] = green[i];

            rgb[offset + 2] = blue[i];
        }

        return RenderedImage.Create(width, height, rgb);
    }
}
