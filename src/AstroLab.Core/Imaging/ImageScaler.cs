using AstroLab.Core.Result;

namespace AstroLab.Core.Imaging;

/// <summary>
/// Pure pixel-scaling algorithms that map raw physical pixel values onto a displayable [0, 1] or
/// [0, 255] range. This is the hot data-processing path for image visualization: <see cref="Stretch"/>
/// performs zero managed-heap allocations, operating entirely over caller-supplied spans.
/// </summary>
public static class ImageScaler
{
    /// <summary>
    /// Normalizes and stretches every pixel in <paramref name="source"/> into the corresponding
    /// byte of <paramref name="destination"/>. Non-finite pixels (NaN/Infinity) map to 0.
    /// </summary>
    public static Result<Unit> Stretch(ReadOnlySpan<float> source, Span<byte> destination, ScaleParameters parameters)
    {
        if (source.Length != destination.Length)
        {
            return Error.Validation(
                "imaging.buffer_length_mismatch",
                $"Destination buffer length ({destination.Length}) must match source length ({source.Length}).");
        }

        if (parameters.Range <= 0 || !double.IsFinite(parameters.Range))
        {
            return Error.Validation("imaging.invalid_scale_range", "WhitePoint must be strictly greater than BlackPoint.");
        }

        for (var i = 0; i < source.Length; i++)
        {
            destination[i] = ToByte(NormalizeAndStretch(source[i], parameters));
        }

        return Result<Unit>.Success(Unit.Value);
    }

    /// <summary>Normalizes and stretches a single pixel value into [0, 1]. Returns 0.0 for non-finite input.</summary>
    public static double NormalizeAndStretch(float value, ScaleParameters parameters)
    {
        if (!float.IsFinite(value))
        {
            return 0.0;
        }

        var t = (value - parameters.BlackPoint) / parameters.Range;
        t = Math.Clamp(t, 0.0, 1.0);
        return ApplyStretch(t, parameters.Mode, parameters.AsinhSoftening);
    }

    private static double ApplyStretch(double t, StretchMode mode, double softening) => mode switch
    {
        StretchMode.Linear => t,
        StretchMode.Logarithmic => LogStretch(t),
        StretchMode.SquareRoot => Math.Sqrt(t),
        StretchMode.Asinh => AsinhStretch(t, softening),
        _ => t,
    };

    private const double LogScaleFactor = 1000.0;

    private static double LogStretch(double t) => Math.Log(1.0 + (LogScaleFactor * t)) / Math.Log(1.0 + LogScaleFactor);

    private static double AsinhStretch(double t, double softening)
    {
        var safeSoftening = softening > 0 ? softening : 0.1;
        return Math.Asinh(t / safeSoftening) / Math.Asinh(1.0 / safeSoftening);
    }

    private static byte ToByte(double normalized) => (byte)Math.Clamp(Math.Round(normalized * 255.0), 0.0, 255.0);
}
