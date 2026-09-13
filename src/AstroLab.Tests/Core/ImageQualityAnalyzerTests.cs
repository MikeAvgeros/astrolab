using System.Text;
using AstroLab.Core.Fits;
using AstroLab.Core.Imaging;

namespace AstroLab.Tests.Core;

public class ImageQualityAnalyzerTests
{
    private static string PadCard(string content) => content.PadRight(FitsCardParser.CardLength);

    private static FitsHeader BuildHeader(params string[] cards)
    {
        var allCards = new string[cards.Length + 1];

        Array.Copy(cards, allCards, cards.Length);

        allCards[^1] = "END";

        var block = Encoding.ASCII.GetBytes(string.Concat(Array.ConvertAll(allCards, PadCard)));

        return FitsHeader.Parse(block).Value;
    }

    private static FitsImageDescriptor BuildByteImageDescriptor() => FitsImageDescriptor.FromHeader(BuildHeader(
        "SIMPLE  =                    T",
        "BITPIX  =                    8",
        "NAXIS   =                    2",
        "NAXIS1  =                    4",
        "NAXIS2  =                    2")).Value;

    private static FitsImageDescriptor BuildFloatImageDescriptor() => FitsImageDescriptor.FromHeader(BuildHeader(
        "SIMPLE  =                    T",
        "BITPIX  =                  -32",
        "NAXIS   =                    2",
        "NAXIS1  =                    4",
        "NAXIS2  =                    2")).Value;

    [Fact]
    public void Analyze_GradientImageWithoutSaturateKeyword_DerivesThresholdFromBitPixRange()
    {
        float[] pixels = [10, 20, 30, 40, 50, 60, 70, 80];

        var header = BuildHeader(
            "SIMPLE  =                    T", "BITPIX  =                    8", "NAXIS   =                    2",
            "NAXIS1  =                    4", "NAXIS2  =                    2");

        var result = ImageQualityAnalyzer.Analyze(pixels, header, BuildByteImageDescriptor());

        Assert.True(result.IsSuccess);

        var report = result.Value;

        Assert.Equal(0, report.NanCount);

        Assert.Equal(0, report.InfiniteCount);

        Assert.Equal(8, report.ValidPixelCount);

        Assert.Equal(8.0, report.DynamicRange!.Value, precision: 6);

        Assert.Equal(255.0, report.SaturationThreshold!.Value, precision: 6);

        Assert.False(report.SaturationThresholdFromHeader);

        Assert.Equal(0, report.SaturatedPixelCount);

        Assert.Equal(1.0, report.UsablePixelFraction, precision: 6);

        Assert.DoesNotContain("fits.quality.saturation_threshold_not_present", report.QualityFlags);
    }

    [Fact]
    public void Analyze_WithSaturateKeyword_UsesHeaderThresholdAndFlagsExceededFraction()
    {
        float[] pixels = [10, 20, 30, 40, 50, 60, 70, 80];

        var header = BuildHeader(
            "SIMPLE  =                    T", "BITPIX  =                    8", "NAXIS   =                    2",
            "NAXIS1  =                    4", "NAXIS2  =                    2", "SATURATE=                 50.0");

        var result = ImageQualityAnalyzer.Analyze(pixels, header, BuildByteImageDescriptor());

        Assert.True(result.IsSuccess);

        var report = result.Value;

        Assert.Equal(50.0, report.SaturationThreshold!.Value, precision: 6);

        Assert.True(report.SaturationThresholdFromHeader);

        Assert.Equal(4, report.SaturatedPixelCount);

        Assert.Equal(0.5, report.SaturatedPixelFraction!.Value, precision: 6);

        Assert.Contains("fits.quality.saturation_exceeds_warning_fraction", report.QualityFlags);
    }

    [Fact]
    public void Analyze_FloatingPointDataWithoutSaturateKeyword_ReportsSaturationNotPresent()
    {
        float[] pixels = [1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 6.0f, 7.0f, 8.0f];

        var header = BuildHeader(
            "SIMPLE  =                    T", "BITPIX  =                  -32", "NAXIS   =                    2",
            "NAXIS1  =                    4", "NAXIS2  =                    2");

        var result = ImageQualityAnalyzer.Analyze(pixels, header, BuildFloatImageDescriptor());

        Assert.True(result.IsSuccess);

        var report = result.Value;

        Assert.Null(report.SaturationThreshold);

        Assert.Null(report.SaturatedPixelCount);

        Assert.Contains("fits.quality.saturation_threshold_not_present", report.QualityFlags);
    }

    [Fact]
    public void Analyze_NonPositiveMinimum_ReportsDynamicRangeAsNotMeasurable()
    {
        float[] pixels = [-5.0f, 0.0f, 5.0f, 10.0f];

        var header = BuildHeader(
            "SIMPLE  =                    T", "BITPIX  =                  -32", "NAXIS   =                    2",
            "NAXIS1  =                    2", "NAXIS2  =                    2");

        var result = ImageQualityAnalyzer.Analyze(pixels, header, BuildFloatImageDescriptor());

        Assert.True(result.IsSuccess);

        Assert.Null(result.Value.DynamicRange);

        Assert.Contains("fits.quality.dynamic_range_not_measurable", result.Value.QualityFlags);
    }

    [Fact]
    public void Analyze_WithNaNAndInfiniteValues_CountsThemSeparately()
    {
        float[] pixels = [1.0f, float.NaN, float.PositiveInfinity, float.NegativeInfinity, float.NaN, 5.0f];

        var header = BuildHeader(
            "SIMPLE  =                    T", "BITPIX  =                  -32", "NAXIS   =                    2",
            "NAXIS1  =                    3", "NAXIS2  =                    2");

        var result = ImageQualityAnalyzer.Analyze(pixels, header, BuildFloatImageDescriptor());

        Assert.True(result.IsSuccess);

        var report = result.Value;

        Assert.Equal(2, report.NanCount);

        Assert.Equal(2, report.InfiniteCount);

        Assert.Equal(4, report.InvalidPixelCount);

        Assert.Equal(2, report.ValidPixelCount);

        Assert.True(report.UsablePixelFraction < 0.9);

        Assert.Contains("fits.quality.low_usable_pixel_fraction", report.QualityFlags);
    }

    [Fact]
    public void Analyze_EmptyPixelArray_ReturnsValidationFailure()
    {
        var result = ImageQualityAnalyzer.Analyze([], BuildHeader("SIMPLE  =                    T"), BuildByteImageDescriptor());

        Assert.True(result.IsFailure);

        Assert.Equal("imaging.empty_pixel_array", result.Error.Code);
    }
}
