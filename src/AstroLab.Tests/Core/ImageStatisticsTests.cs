using AstroLab.Core.Imaging;

namespace AstroLab.Tests.Core;

public class ImageStatisticsTests
{
    [Fact]
    public void Compute_OnKnownArray_ProducesExpectedMoments()
    {
        ReadOnlySpan<float> pixels = [1f, 2f, 3f, 4f, 5f];

        var result = ImageStatistics.Compute(pixels);

        Assert.True(result.IsSuccess);

        var stats = result.Value;

        Assert.Equal(1.0, stats.Min, precision: 6);

        Assert.Equal(5.0, stats.Max, precision: 6);

        Assert.Equal(3.0, stats.Mean, precision: 6);

        Assert.Equal(Math.Sqrt(2), stats.StdDev, precision: 6);

        Assert.Equal(5, stats.ValidPixelCount);

        Assert.Equal(5, stats.TotalPixelCount);
    }

    [Fact]
    public void Compute_IgnoresNonFinitePixels()
    {
        ReadOnlySpan<float> withNaN = [1f, 2f, float.NaN, 4f, 5f];

        ReadOnlySpan<float> withoutNaN = [1f, 2f, 4f, 5f];

        var withNaNResult = ImageStatistics.Compute(withNaN).Value;

        var withoutNaNResult = ImageStatistics.Compute(withoutNaN).Value;

        Assert.Equal(withoutNaNResult.Mean, withNaNResult.Mean, precision: 6);

        Assert.Equal(withoutNaNResult.StdDev, withNaNResult.StdDev, precision: 6);

        Assert.Equal(4, withNaNResult.ValidPixelCount);

        Assert.Equal(5, withNaNResult.TotalPixelCount);

        Assert.Equal(1, withNaNResult.InvalidPixelCount);
    }

    [Fact]
    public void Compute_OnEmptyArray_Fails()
    {
        var result = ImageStatistics.Compute(ReadOnlySpan<float>.Empty);

        Assert.True(result.IsFailure);

        Assert.Equal("imaging.empty_pixel_array", result.Error.Code);
    }

    [Fact]
    public void Compute_OnAllNonFiniteArray_Fails()
    {
        ReadOnlySpan<float> pixels = [float.NaN, float.PositiveInfinity];

        var result = ImageStatistics.Compute(pixels);

        Assert.True(result.IsFailure);

        Assert.Equal("imaging.no_valid_pixels", result.Error.Code);
    }

    [Fact]
    public void ComputePercentileBounds_OnUniformDistribution_ApproximatesExpectedQuantiles()
    {
        var pixels = new float[1000];

        for (var i = 0; i < pixels.Length; i++)
        {
            pixels[i] = i;
        }

        var result = ImageStatistics.ComputePercentileBounds(pixels, lowerPercentile: 10, upperPercentile: 90);

        Assert.True(result.IsSuccess);

        Assert.True(Math.Abs(result.Value.Lower - 100) < 15);

        Assert.True(Math.Abs(result.Value.Upper - 900) < 15);
    }

    [Fact]
    public void ComputePercentileBounds_RejectsInvalidRange()
    {
        var result = ImageStatistics.ComputePercentileBounds([1f, 2f, 3f], lowerPercentile: 90, upperPercentile: 10);

        Assert.True(result.IsFailure);

        Assert.Equal("imaging.invalid_percentile_range", result.Error.Code);
    }

    [Fact]
    public void ComputeSkyBackground_OnUniformDistribution_ApproximatesExpectedSigma()
    {
        var pixels = new float[1000];

        for (var i = 0; i < pixels.Length; i++)
        {
            pixels[i] = i;
        }

        var stats = ImageStatistics.Compute(pixels).Value;

        var skyBackground = ImageStatistics.ComputeSkyBackground(pixels, stats);

        Assert.True(Math.Abs(skyBackground.Q1 - 250) < 15);

        Assert.True(Math.Abs(skyBackground.Q3 - 750) < 15);

        Assert.True(Math.Abs(skyBackground.SkySigma - (500.0 / 1.349)) < 15);
    }

    [Fact]
    public void ComputeSkyBackground_OnUniformPixelArray_ProducesZeroSigma()
    {
        ReadOnlySpan<float> pixels = [5f, 5f, 5f, 5f];

        var stats = ImageStatistics.Compute(pixels).Value;

        var skyBackground = ImageStatistics.ComputeSkyBackground(pixels, stats);

        Assert.Equal(0.0, skyBackground.SkySigma, precision: 6);
    }
}
