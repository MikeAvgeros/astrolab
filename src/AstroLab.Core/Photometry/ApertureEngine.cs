using System.Buffers;
using AstroLab.Core.Result;

namespace AstroLab.Core.Photometry;

/// <summary>
/// Pure circular-aperture photometry algorithms operating directly over a caller-owned pixel
/// span. No method in this class performs I/O, allocates large intermediate buffers, or retains
/// references to the input span beyond the call.
/// </summary>
public static class ApertureEngine
{
    /// <summary>
    /// Default subpixel oversampling factor for fractional pixel-coverage integration. Each
    /// boundary pixel is sampled on a <c>N x N</c> subgrid, matching the "subpixel" method
    /// used by common photometry packages.
    /// </summary>
    public const int DefaultSubpixelOversampling = 5;

    private const double PixelCenterOffset = 0.5;

    /// <summary>
    /// Integrates flux within a circular aperture centred at (<paramref name="centerX"/>,
    /// <paramref name="centerY"/>), using exact inclusion/exclusion for pixels fully inside or
    /// outside the aperture and subpixel-supersampled fractional coverage for boundary pixels.
    /// Non-finite (NaN/Infinity) pixels are excluded from both the flux sum and the aperture area.
    /// </summary>
    public static Result<ApertureMeasurement> MeasureCircularAperture(
        ReadOnlySpan<float> pixels,
        int width,
        int height,
        double centerX,
        double centerY,
        double radius,
        int subpixelOversampling = DefaultSubpixelOversampling)
    {
        var boundsCheck = ValidateImageBounds(pixels.Length, width, height);
        if (boundsCheck.IsFailure)
        {
            return Result<ApertureMeasurement>.Failure(boundsCheck.Error);
        }

        var radiusCheck = ValidateRadius(radius, nameof(radius));
        if (radiusCheck.IsFailure)
        {
            return Result<ApertureMeasurement>.Failure(radiusCheck.Error);
        }

        var oversamplingCheck = ValidateOversampling(subpixelOversampling);
        if (oversamplingCheck.IsFailure)
        {
            return Result<ApertureMeasurement>.Failure(oversamplingCheck.Error);
        }

        var (xMin, xMax, yMin, yMax) = BoundingBox(width, height, centerX, centerY, radius);

        double flux = 0.0;
        double area = 0.0;
        var sampledPixels = 0;

        for (var py = yMin; py <= yMax; py++)
        {
            var rowOffset = py * width;
            for (var px = xMin; px <= xMax; px++)
            {
                var coverage = PixelCoverageFraction(px, py, centerX, centerY, radius, subpixelOversampling);
                if (coverage <= 0.0)
                {
                    continue;
                }

                var value = pixels[rowOffset + px];
                if (!float.IsFinite(value))
                {
                    continue;
                }

                flux += value * coverage;
                area += coverage;
                sampledPixels++;
            }
        }

        return new ApertureMeasurement(flux, area, sampledPixels);
    }

    /// <summary>
    /// Estimates the local sky background from an annulus between <paramref name="innerRadius"/>
    /// and <paramref name="outerRadius"/>, using binary pixel-center inclusion (a pixel is a
    /// sample if its center lies within the annulus). Non-finite pixels are excluded.
    /// </summary>
    public static Result<AnnulusMeasurement> MeasureAnnulusBackground(
        ReadOnlySpan<float> pixels,
        int width,
        int height,
        double centerX,
        double centerY,
        double innerRadius,
        double outerRadius,
        BackgroundEstimationMethod method = BackgroundEstimationMethod.Median)
    {
        var boundsCheck = ValidateImageBounds(pixels.Length, width, height);
        if (boundsCheck.IsFailure)
        {
            return Result<AnnulusMeasurement>.Failure(boundsCheck.Error);
        }

        var innerRadiusCheck = ValidateRadius(innerRadius, nameof(innerRadius));
        if (innerRadiusCheck.IsFailure)
        {
            return Result<AnnulusMeasurement>.Failure(innerRadiusCheck.Error);
        }

        var outerRadiusCheck = ValidateRadius(outerRadius, nameof(outerRadius));
        if (outerRadiusCheck.IsFailure)
        {
            return Result<AnnulusMeasurement>.Failure(outerRadiusCheck.Error);
        }

        if (outerRadius <= innerRadius)
        {
            return Error.Validation("photometry.invalid_annulus", "outerRadius must be greater than innerRadius.");
        }

        var (xMin, xMax, yMin, yMax) = BoundingBox(width, height, centerX, centerY, outerRadius);
        var innerRadiusSquared = innerRadius * innerRadius;
        var outerRadiusSquared = outerRadius * outerRadius;

        return method == BackgroundEstimationMethod.Mean
            ? MeasureAnnulusMean(pixels, width, xMin, xMax, yMin, yMax, centerX, centerY, innerRadiusSquared, outerRadiusSquared)
            : MeasureAnnulusMedian(pixels, width, xMin, xMax, yMin, yMax, centerX, centerY, innerRadiusSquared, outerRadiusSquared);
    }

    /// <summary>
    /// Performs a full photometric measurement: source flux within a circular aperture, local
    /// background estimated from a concentric annulus, and the resulting background-subtracted
    /// net flux.
    /// </summary>
    public static Result<NetFluxMeasurement> MeasureNetFlux(
        ReadOnlySpan<float> pixels,
        int width,
        int height,
        double centerX,
        double centerY,
        double apertureRadius,
        double annulusInnerRadius,
        double annulusOuterRadius,
        BackgroundEstimationMethod backgroundMethod = BackgroundEstimationMethod.Median,
        int subpixelOversampling = DefaultSubpixelOversampling)
    {
        var apertureResult = MeasureCircularAperture(pixels, width, height, centerX, centerY, apertureRadius, subpixelOversampling);
        if (apertureResult.IsFailure)
        {
            return Result<NetFluxMeasurement>.Failure(apertureResult.Error);
        }

        var annulusResult = MeasureAnnulusBackground(pixels, width, height, centerX, centerY, annulusInnerRadius, annulusOuterRadius, backgroundMethod);
        if (annulusResult.IsFailure)
        {
            return Result<NetFluxMeasurement>.Failure(annulusResult.Error);
        }

        var aperture = apertureResult.Value;
        var annulus = annulusResult.Value;
        return new NetFluxMeasurement(
            RawFlux: aperture.Flux,
            ApertureArea: aperture.Area,
            BackgroundPerPixel: annulus.BackgroundPerPixel,
            NetFlux: aperture.Flux - (annulus.BackgroundPerPixel * aperture.Area));
    }

    private static Result<AnnulusMeasurement> MeasureAnnulusMean(
        ReadOnlySpan<float> pixels, int width, int xMin, int xMax, int yMin, int yMax,
        double centerX, double centerY, double innerRadiusSquared, double outerRadiusSquared)
    {
        double sum = 0.0;
        var count = 0;

        for (var py = yMin; py <= yMax; py++)
        {
            var rowOffset = py * width;
            var dy = py + PixelCenterOffset - centerY;
            var dySquared = dy * dy;
            for (var px = xMin; px <= xMax; px++)
            {
                var dx = px + PixelCenterOffset - centerX;
                var distanceSquared = (dx * dx) + dySquared;
                if (distanceSquared < innerRadiusSquared || distanceSquared > outerRadiusSquared)
                {
                    continue;
                }

                var value = pixels[rowOffset + px];
                if (!float.IsFinite(value))
                {
                    continue;
                }

                sum += value;
                count++;
            }
        }

        return count > 0
            ? new AnnulusMeasurement(sum / count, count)
            : Error.Validation("photometry.empty_annulus", "No valid pixels were found within the background annulus.");
    }

    private static Result<AnnulusMeasurement> MeasureAnnulusMedian(
        ReadOnlySpan<float> pixels, int width, int xMin, int xMax, int yMin, int yMax,
        double centerX, double centerY, double innerRadiusSquared, double outerRadiusSquared)
    {
        var boundingArea = (xMax - xMin + 1) * (yMax - yMin + 1);
        var buffer = ArrayPool<float>.Shared.Rent(Math.Max(boundingArea, 1));
        try
        {
            var count = 0;
            for (var py = yMin; py <= yMax; py++)
            {
                var rowOffset = py * width;
                var dy = py + PixelCenterOffset - centerY;
                var dySquared = dy * dy;
                for (var px = xMin; px <= xMax; px++)
                {
                    var dx = px + PixelCenterOffset - centerX;
                    var distanceSquared = (dx * dx) + dySquared;
                    if (distanceSquared < innerRadiusSquared || distanceSquared > outerRadiusSquared)
                    {
                        continue;
                    }

                    var value = pixels[rowOffset + px];
                    if (!float.IsFinite(value))
                    {
                        continue;
                    }

                    buffer[count++] = value;
                }
            }

            if (count == 0)
            {
                return Error.Validation("photometry.empty_annulus", "No valid pixels were found within the background annulus.");
            }

            var samples = buffer.AsSpan(0, count);
            samples.Sort();
            var median = count % 2 == 1
                ? samples[count / 2]
                : (samples[(count / 2) - 1] + samples[count / 2]) / 2.0f;

            return new AnnulusMeasurement(median, count);
        }
        finally
        {
            ArrayPool<float>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// The fraction (0..1) of pixel (<paramref name="px"/>, <paramref name="py"/>) that lies
    /// within <paramref name="radius"/> of the aperture center. Pixels fully inside or fully
    /// outside are resolved in O(1) via corner-distance checks; only boundary pixels pay the
    /// cost of subpixel supersampling.
    /// </summary>
    private static double PixelCoverageFraction(int px, int py, double centerX, double centerY, double radius, int oversampling)
    {
        var nearCornerDistance = NearestCornerDistance(px, py, centerX, centerY);
        if (nearCornerDistance >= radius)
        {
            return 0.0;
        }

        var farCornerDistance = FarthestCornerDistance(px, py, centerX, centerY);
        if (farCornerDistance <= radius)
        {
            return 1.0;
        }

        var radiusSquared = radius * radius;
        var hits = 0;
        for (var sy = 0; sy < oversampling; sy++)
        {
            var subY = py + ((sy + PixelCenterOffset) / oversampling);
            var dy = subY - centerY;
            var dySquared = dy * dy;
            for (var sx = 0; sx < oversampling; sx++)
            {
                var subX = px + ((sx + PixelCenterOffset) / oversampling);
                var dx = subX - centerX;
                if ((dx * dx) + dySquared <= radiusSquared)
                {
                    hits++;
                }
            }
        }

        return (double)hits / (oversampling * oversampling);
    }

    private static double NearestCornerDistance(int px, int py, double centerX, double centerY)
    {
        var dx = Math.Max(0.0, Math.Max(px - centerX, centerX - (px + 1)));
        var dy = Math.Max(0.0, Math.Max(py - centerY, centerY - (py + 1)));
        return Math.Sqrt((dx * dx) + (dy * dy));
    }

    private static double FarthestCornerDistance(int px, int py, double centerX, double centerY)
    {
        var dx = Math.Max(Math.Abs(px - centerX), Math.Abs(px + 1 - centerX));
        var dy = Math.Max(Math.Abs(py - centerY), Math.Abs(py + 1 - centerY));
        return Math.Sqrt((dx * dx) + (dy * dy));
    }

    private static (int XMin, int XMax, int YMin, int YMax) BoundingBox(int width, int height, double centerX, double centerY, double radius)
    {
        var xMin = Math.Max(0, (int)Math.Floor(centerX - radius));
        var xMax = Math.Min(width - 1, (int)Math.Ceiling(centerX + radius));
        var yMin = Math.Max(0, (int)Math.Floor(centerY - radius));
        var yMax = Math.Min(height - 1, (int)Math.Ceiling(centerY + radius));
        return (xMin, xMax, yMin, yMax);
    }

    private static Result<Unit> ValidateImageBounds(int pixelLength, int width, int height) =>
        width > 0 && height > 0 && pixelLength == width * height
            ? Result<Unit>.Success(Unit.Value)
            : Error.Validation("photometry.invalid_image_bounds", $"Pixel span length ({pixelLength}) does not match width x height ({width}x{height}).");

    private static Result<Unit> ValidateRadius(double radius, string paramName) =>
        radius > 0 && double.IsFinite(radius)
            ? Result<Unit>.Success(Unit.Value)
            : Error.Validation("photometry.invalid_radius", $"{paramName} must be a finite, positive value.");

    private static Result<Unit> ValidateOversampling(int oversampling) =>
        oversampling >= 1
            ? Result<Unit>.Success(Unit.Value)
            : Error.Validation("photometry.invalid_oversampling", "subpixelOversampling must be at least 1.");
}
