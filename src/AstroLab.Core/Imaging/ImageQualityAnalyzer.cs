using System.Collections.Immutable;
using AstroLab.Core.Fits;
using AstroLab.Core.Result;

namespace AstroLab.Core.Imaging;

/// <summary>
/// Cross-cutting data-quality analysis for a loaded image: non-finite pixel accounting, basic
/// statistics, an estimated sky background/noise floor, dynamic range, saturation (from a
/// <c>SATURATE</c> header keyword when present, otherwise a heuristic derived from the HDU's
/// <c>BITPIX</c> representable range for integer data), and the fraction of pixels usable for
/// analysis. Values that cannot be determined for a given dataset (for example, saturation for a
/// floating-point HDU with no <c>SATURATE</c> keyword) are reported as absent rather than
/// invented, per the not-present / not-measurable / measured-as-zero distinction this analysis
/// exists to preserve.
/// </summary>
public static class ImageQualityAnalyzer
{
    private const string SaturateKeyword = "SATURATE";

    private const double SaturationWarningFraction = 0.01;
    private const double LowUsablePixelFractionThreshold = 0.9;

    public static Result<ImageQualityReport> Analyze(ReadOnlySpan<float> pixels, FitsHeader header, FitsImageDescriptor descriptor)
    {
        var statsResult = ImageStatistics.Compute(pixels);

        if (statsResult.IsFailure)
        {
            return Result<ImageQualityReport>.Failure(statsResult.Error);
        }

        var stats = statsResult.Value;

        var (nanCount, infiniteCount) = CountNonFinite(pixels);

        var skyBackground = ImageStatistics.ComputeSkyBackground(pixels, stats);

        var estimatedBackground = (skyBackground.Q1 + skyBackground.Q3) / 2.0;

        var dynamicRange = stats.Min > 0.0 ? stats.Max / stats.Min : (double?)null;

        var saturationThresholdResult = ResolveSaturationThreshold(header, descriptor);

        if (saturationThresholdResult.IsFailure)
        {
            return Result<ImageQualityReport>.Failure(saturationThresholdResult.Error);
        }

        var (saturationThreshold, saturationThresholdFromHeader) = saturationThresholdResult.Value;

        var (saturatedPixelCount, saturatedPixelFraction) = CountSaturated(pixels, saturationThreshold, stats.ValidPixelCount);

        var usablePixelFraction = stats.TotalPixelCount > 0 ? stats.ValidPixelCount / (double)stats.TotalPixelCount : 0.0;

        var flags = BuildQualityFlags(dynamicRange, saturationThreshold, saturatedPixelFraction, usablePixelFraction);

        return ImageQualityReport.Create(
            nanCount, infiniteCount, stats.InvalidPixelCount, stats.ValidPixelCount, stats.TotalPixelCount,
            stats.Min, stats.Max, stats.Mean, stats.StdDev,
            estimatedBackground, skyBackground.SkySigma, dynamicRange,
            saturationThreshold, saturationThresholdFromHeader, saturatedPixelCount, saturatedPixelFraction,
            usablePixelFraction, flags);
    }

    private static (long NanCount, long InfiniteCount) CountNonFinite(ReadOnlySpan<float> pixels)
    {
        long nanCount = 0;

        long infiniteCount = 0;

        foreach (var value in pixels)
        {
            if (float.IsNaN(value))
            {
                nanCount++;
            }
            else if (float.IsInfinity(value))
            {
                infiniteCount++;
            }
        }

        return (nanCount, infiniteCount);
    }

    private static Result<(double? Threshold, bool FromHeader)> ResolveSaturationThreshold(FitsHeader header, FitsImageDescriptor descriptor)
    {
        var saturateResult = header.GetReal(SaturateKeyword);

        if (saturateResult.IsSuccess)
        {
            return (saturateResult.Value, true);
        }
        
        if (saturateResult.Error.Category != ErrorCategory.NotFound)
        {
            return Result<(double?, bool)>.Failure(saturateResult.Error);
        }

        if (descriptor.BitPix.IsFloatingPoint())
        {
            return (null, false);
        }

        return (descriptor.ToPhysical(MaxRawValue(descriptor.BitPix)), false);
    }

    private static double MaxRawValue(BitPixType bitPix) => bitPix switch
    {
        BitPixType.Byte => byte.MaxValue,
        BitPixType.Int16 => short.MaxValue,
        BitPixType.Int32 => int.MaxValue,
        BitPixType.Int64 => long.MaxValue,
        _ => throw new ArgumentOutOfRangeException(nameof(bitPix), bitPix, "Unsupported integer BITPIX value.")
    };

    private static (long? Count, double? Fraction) CountSaturated(ReadOnlySpan<float> pixels, double? threshold, long validPixelCount)
    {
        if (threshold is not { } saturationThreshold)
        {
            return (null, null);
        }

        long count = 0;

        foreach (var value in pixels)
        {
            if (float.IsFinite(value) && value >= saturationThreshold)
            {
                count++;
            }
        }

        var fraction = validPixelCount > 0 ? count / (double)validPixelCount : 0.0;

        return (count, fraction);
    }

    private static ImmutableArray<string> BuildQualityFlags(
        double? dynamicRange, double? saturationThreshold, double? saturatedPixelFraction, double usablePixelFraction)
    {
        var flags = ImmutableArray.CreateBuilder<string>();

        if (dynamicRange is null)
        {
            flags.Add("fits.quality.dynamic_range_not_measurable");
        }

        if (saturationThreshold is null)
        {
            flags.Add("fits.quality.saturation_threshold_not_present");
        }
        else if (saturatedPixelFraction > SaturationWarningFraction)
        {
            flags.Add("fits.quality.saturation_exceeds_warning_fraction");
        }

        if (usablePixelFraction < LowUsablePixelFractionThreshold)
        {
            flags.Add("fits.quality.low_usable_pixel_fraction");
        }

        return flags.ToImmutable();
    }
}
