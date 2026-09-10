using AstroLab.Core.Result;

namespace AstroLab.Core.Imaging;

/// <summary>
/// Computes summary difference statistics between two equally-sized, pre-aligned pixel arrays —
/// the mean, spread, and peak of the per-pixel difference — used to flag transient or variable
/// sources between two frames of the same field.
/// </summary>
public static class ImageComparer
{
    public static Result<ImageDifferenceStatistics> Compare(ReadOnlySpan<float> pixels, ReadOnlySpan<float> comparisonPixels, int width, int height)
    {
        if (width <= 0 || height <= 0 || pixels.Length != width * height || comparisonPixels.Length != width * height)
        {
            return Error.Validation(
                "imaging.compare.invalid_image_bounds",
                $"Pixel span lengths ({pixels.Length}, {comparisonPixels.Length}) do not both match width x height ({width}x{height}).");
        }

        var sum = 0.0;

        var maxAbsoluteDifference = 0.0;

        long validCount = 0;

        for (var i = 0; i < pixels.Length; i++)
        {
            var difference = (double)comparisonPixels[i] - pixels[i];

            if (!double.IsFinite(difference))
            {
                continue;
            }

            sum += difference;

            var absoluteDifference = Math.Abs(difference);

            if (absoluteDifference > maxAbsoluteDifference)
            {
                maxAbsoluteDifference = absoluteDifference;
            }

            validCount++;
        }

        if (validCount == 0)
        {
            return Error.Validation("imaging.compare.no_valid_pixels", "No finite pixel differences were found between the two images.");
        }

        var mean = sum / validCount;

        var sumSquaredDeviation = 0.0;

        for (var i = 0; i < pixels.Length; i++)
        {
            var difference = (double)comparisonPixels[i] - pixels[i];

            if (!double.IsFinite(difference))
            {
                continue;
            }

            var deviation = difference - mean;

            sumSquaredDeviation += deviation * deviation;
        }

        var standardDeviation = Math.Sqrt(sumSquaredDeviation / validCount);

        return ImageDifferenceStatistics.Create(mean, standardDeviation, maxAbsoluteDifference);
    }
}
