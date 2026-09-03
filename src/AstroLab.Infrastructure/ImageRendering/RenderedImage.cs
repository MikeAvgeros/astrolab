namespace AstroLab.Infrastructure.ImageRendering;

public readonly record struct RenderedImage
{
    private const int RgbChannelCount = 3;

    private RenderedImage(int width, int height, byte[] rgb)
    {
        Width = width;
        Height = height;
        Rgb = rgb;
    }

    public int Width { get; }

    public int Height { get; }

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
