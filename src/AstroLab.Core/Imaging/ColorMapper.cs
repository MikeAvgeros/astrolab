using AstroLab.Core.Result;

namespace AstroLab.Core.Imaging;

/// <summary>
/// Pure mapping from normalized (post-stretch) grayscale intensities onto RGB colors. Operates
/// directly over spans with no intermediate allocations.
/// </summary>
public static class ColorMapper
{
    /// <summary>
    /// Maps each normalized grayscale byte in <paramref name="intensities"/> onto an RGB triple
    /// written contiguously into <paramref name="rgb"/> (which must be exactly three times as long).
    /// </summary>
    public static Result<Unit> Apply(ReadOnlySpan<byte> intensities, Span<byte> rgb, ColorMap colorMap)
    {
        if (rgb.Length != intensities.Length * 3)
        {
            return Error.Validation(
                "imaging.rgb_buffer_length_mismatch",
                $"RGB buffer length ({rgb.Length}) must be exactly 3x the intensity buffer length ({intensities.Length}).");
        }

        for (var i = 0; i < intensities.Length; i++)
        {
            var (r, g, b) = Map(intensities[i], colorMap);
            var offset = i * 3;
            rgb[offset] = r;
            rgb[offset + 1] = g;
            rgb[offset + 2] = b;
        }

        return Result<Unit>.Success(Unit.Value);
    }

    /// <summary>Maps a single normalized grayscale byte (0-255) onto an RGB color.</summary>
    public static (byte R, byte G, byte B) Map(byte intensity, ColorMap colorMap) => colorMap switch
    {
        ColorMap.Grayscale => (intensity, intensity, intensity),
        ColorMap.Hot => MapHot(intensity),
        ColorMap.Viridis => MapViridis(intensity),
        _ => (intensity, intensity, intensity),
    };

    private static (byte R, byte G, byte B) MapHot(byte intensity)
    {
        var t = intensity / 255.0;
        var r = Clamp01(3.0 * t);
        var g = Clamp01((3.0 * t) - 1.0);
        var b = Clamp01((3.0 * t) - 2.0);
        return (ToByte(r), ToByte(g), ToByte(b));
    }

    /// <summary>8-stop approximation of matplotlib's "viridis" colormap, linearly interpolated between stops.</summary>
    private static readonly (float Position, byte R, byte G, byte B)[] ViridisStops =
    [
        (0.000f, 68, 1, 84),
        (0.143f, 70, 51, 126),
        (0.286f, 54, 92, 141),
        (0.429f, 39, 127, 142),
        (0.571f, 31, 161, 135),
        (0.714f, 74, 193, 109),
        (0.857f, 160, 218, 57),
        (1.000f, 253, 231, 37),
    ];

    private static (byte R, byte G, byte B) MapViridis(byte intensity)
    {
        var t = intensity / 255f;
        var stops = ViridisStops;

        var upperIndex = 1;
        while (upperIndex < stops.Length - 1 && t > stops[upperIndex].Position)
        {
            upperIndex++;
        }

        var lower = stops[upperIndex - 1];
        var upper = stops[upperIndex];
        var span = upper.Position - lower.Position;
        var localT = span > 0 ? (t - lower.Position) / span : 0f;

        return (
            Lerp(lower.R, upper.R, localT),
            Lerp(lower.G, upper.G, localT),
            Lerp(lower.B, upper.B, localT));
    }

    private static byte Lerp(byte a, byte b, float t) => (byte)Math.Clamp(a + ((b - a) * t), 0f, 255f);

    private static double Clamp01(double value) => Math.Clamp(value, 0.0, 1.0);

    private static byte ToByte(double normalized) => (byte)Math.Round(normalized * 255.0);
}
