using AstroLab.Core.Result;

namespace AstroLab.Core.Imaging;

/// <summary>
/// Reduces a pixel array's resolution via integer-factor box averaging, so very large images can
/// be rendered without paying the full per-pixel stretch/color-map cost. Never modifies the source
/// pixel data.
/// </summary>
public static class ImageDownsampler
{
    /// <summary>
    /// The integer block-averaging factor required to bring the longer of <paramref name="width"/>/
    /// <paramref name="height"/> at or below <paramref name="maxDimension"/>. Returns 1 (no-op) when
    /// the image is already within bounds.
    /// </summary>
    public static Result<int> ComputeFactor(int width, int height, int maxDimension)
    {
        if (width <= 0 || height <= 0)
        {
            return Error.Validation(
                "imaging.invalid_downsample_dimensions", $"width and height must be positive, was {width}x{height}.");
        }

        if (maxDimension <= 0)
        {
            return Error.Validation("imaging.invalid_downsample_max_dimension", "maxDimension must be positive.");
        }

        var longestSide = Math.Max(width, height);

        return longestSide <= maxDimension ? 1 : (int)Math.Ceiling(longestSide / (double)maxDimension);
    }

    /// <summary>The output dimensions produced by downsampling a width x height image by an integer factor.</summary>
    public static (int Width, int Height) ComputeDownsampledDimensions(int width, int height, int factor) =>
        ((width + factor - 1) / factor, (height + factor - 1) / factor);

    /// <summary>
    /// Box-averages <paramref name="source"/> (row-major, <paramref name="width"/> x <paramref name="height"/>)
    /// into <paramref name="destination"/> at 1/<paramref name="factor"/> resolution. A destination pixel is
    /// the mean of the finite source pixels in its block; a block with no finite source pixels produces
    /// <see cref="float.NaN"/>, matching how the renderer already treats invalid pixels.
    /// </summary>
    public static Result<Unit> Downsample(ReadOnlySpan<float> source, int width, int height, int factor, Span<float> destination)
    {
        if (width <= 0 || height <= 0 || source.Length != width * height)
        {
            return Error.Validation(
                "imaging.invalid_downsample_source",
                $"Source span length ({source.Length}) does not match width x height ({width}x{height}).");
        }

        if (factor <= 0)
        {
            return Error.Validation("imaging.invalid_downsample_factor", "factor must be positive.");
        }

        var (destWidth, destHeight) = ComputeDownsampledDimensions(width, height, factor);

        if (destination.Length != destWidth * destHeight)
        {
            return Error.Validation(
                "imaging.downsample_buffer_length_mismatch",
                $"Destination buffer length ({destination.Length}) must match the downsampled dimensions ({destWidth}x{destHeight}).");
        }

        for (var destY = 0; destY < destHeight; destY++)
        {
            var sourceRowStart = destY * factor;

            var sourceRowEnd = Math.Min(sourceRowStart + factor, height);

            for (var destX = 0; destX < destWidth; destX++)
            {
                var sourceColStart = destX * factor;

                var sourceColEnd = Math.Min(sourceColStart + factor, width);

                destination[destY * destWidth + destX] = AverageBlock(source, width, sourceRowStart, sourceRowEnd, sourceColStart, sourceColEnd);
            }
        }

        return Result<Unit>.Success(Unit.Value);
    }

    private static float AverageBlock(ReadOnlySpan<float> source, int width, int rowStart, int rowEnd, int colStart, int colEnd)
    {
        double sum = 0.0;

        var count = 0;

        for (var sourceY = rowStart; sourceY < rowEnd; sourceY++)
        {
            var rowOffset = sourceY * width;

            for (var sourceX = colStart; sourceX < colEnd; sourceX++)
            {
                var value = source[rowOffset + sourceX];

                if (float.IsFinite(value))
                {
                    sum += value;

                    count++;
                }
            }
        }

        return count > 0 ? (float)(sum / count) : float.NaN;
    }
}
