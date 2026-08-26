namespace AstroLab.Infrastructure.ImageRendering;

/// <summary>An in-memory, interleaved 8-bit RGB image produced by <see cref="FitsImageRenderer"/>.</summary>
public readonly record struct RenderedImage
{
    private const int RgbChannelCount = 3;

    private RenderedImage(int width, int height, byte[] rgb)
    {
        Width = width;
        Height = height;
        Rgb = rgb;
    }

    /// <summary>Image width, in pixels.</summary>
    public int Width { get; }

    /// <summary>Image height, in pixels.</summary>
    public int Height { get; }

    /// <summary>Row-major pixel data, 3 bytes (R, G, B) per pixel; length must equal <c>Width * Height * 3</c>.</summary>
    public byte[] Rgb { get; }

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
