namespace AstroLab.Infrastructure.ImageRendering;

/// <summary>An in-memory, interleaved 8-bit RGB image produced by <see cref="FitsImageRenderer"/>.</summary>
/// <param name="Width">Image width, in pixels.</param>
/// <param name="Height">Image height, in pixels.</param>
/// <param name="Rgb">Row-major pixel data, 3 bytes (R, G, B) per pixel; length must equal <c>Width * Height * 3</c>.</param>
public readonly record struct RenderedImage(int Width, int Height, byte[] Rgb);

/// <summary>Static factory accompanying <see cref="RenderedImage"/>. Validates arguments before constructing.</summary>
public static class RenderedImageFactory
{
    private const int RgbChannelCount = 3;

    public static RenderedImage Create(int width, int height, byte[] rgb)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

        ArgumentNullException.ThrowIfNull(rgb);

        if (rgb.Length != width * height * RgbChannelCount)
        {
            throw new ArgumentException(
                $"Rgb buffer length ({rgb.Length}) does not match width * height * {RgbChannelCount} ({width * height * RgbChannelCount}).",
                nameof(rgb));
        }

        return new RenderedImage(width, height, rgb);
    }
}
