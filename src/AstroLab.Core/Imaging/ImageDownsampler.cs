using AstroLab.Core.Result;

namespace AstroLab.Core.Imaging;

/// <summary>
/// Reduces a pixel array's resolution via integer-factor box averaging, so very large images can
/// be rendered without paying the full per-pixel stretch/color-map cost. Never modifies the source
/// pixel data.
/// </summary>
public static class ImageDownsampler
{
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

    public static (int Width, int Height) ComputeDownsampledDimensions(int width, int height, int factor) =>
        ((width + factor - 1) / factor, (height + factor - 1) / factor);

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
