using AstroLab.Core.Imaging;
using AstroLab.Core.Sources;

namespace AstroLab.Tests.Core;

public class SourceDetectorTests
{
    private const int Width = 12;
    private const int Height = 12;

    /// <summary>
    /// A 12x12 background with a repeating 5..15 cycle (guarantees a nonzero, hand-independent-
    /// computable sigma) and one 3x3, constant-value 1000 block at columns/rows 4-6 — orders of
    /// magnitude above any plausible threshold.
    /// </summary>
    private static float[] BuildImageWithOneBlock(double blockValue = 1000.0)
    {
        var pixels = new float[Width * Height];

        for (var i = 0; i < pixels.Length; i++)
        {
            pixels[i] = 5 + (i % 11);
        }

        for (var y = 4; y <= 6; y++)
        {
            for (var x = 4; x <= 6; x++)
            {
                pixels[(y * Width) + x] = (float)blockValue;
            }
        }

        return pixels;
    }

    private static (double Background, double Sigma) ExpectedBackgroundAndSigma(ReadOnlySpan<float> pixels)
    {
        var stats = ImageStatistics.Compute(pixels).Value;

        Span<double> median = stackalloc double[1];

        ImageStatistics.ComputePercentiles(pixels, stats, [50.0], median);

        var sigma = ImageStatistics.ComputeSkyBackground(pixels, stats).SkySigma;

        return (median[0], sigma);
    }

    [Fact]
    public void Detect_FindsSingleBlock_WithExactCentroidPeakAndFlux()
    {
        var pixels = BuildImageWithOneBlock();

        var (expectedBackground, expectedSigma) = ExpectedBackgroundAndSigma(pixels);

        var result = SourceDetector.Detect(pixels, Width, Height);

        Assert.True(result.IsSuccess);

        var source = Assert.Single(result.Value);

        Assert.Equal(1, source.Id);

        Assert.Equal(9, source.PixelCount);

        Assert.Equal(1000.0, source.PeakValue, precision: 6);

        Assert.Equal(5.5, source.PixelX, precision: 6);

        Assert.Equal(5.5, source.PixelY, precision: 6);

        Assert.Equal(expectedBackground, source.Background, precision: 9);

        var expectedFlux = 9 * (1000.0 - expectedBackground);

        Assert.Equal(expectedFlux, source.TotalFlux, precision: 6);

        var expectedSnr = expectedFlux / (expectedSigma * Math.Sqrt(9));

        Assert.Equal(expectedSnr, source.SignalToNoiseRatio, precision: 6);
    }

    [Fact]
    public void Detect_WithMinimumAreaAboveBlockSize_ExcludesTheBlock()
    {
        var pixels = BuildImageWithOneBlock();

        var result = SourceDetector.Detect(pixels, Width, Height, minimumArea: 10);

        Assert.True(result.IsSuccess);

        Assert.Empty(result.Value);
    }

    [Fact]
    public void Detect_TwoDiagonallyTouchingPixels_MergeIntoOneRegion()
    {
        var pixels = new float[Width * Height];

        for (var i = 0; i < pixels.Length; i++)
        {
            pixels[i] = 5 + (i % 11);
        }

        pixels[(5 * Width) + 5] = 1000f;

        pixels[(6 * Width) + 6] = 1000f;

        var result = SourceDetector.Detect(pixels, Width, Height, minimumArea: 2);

        Assert.True(result.IsSuccess);

        var source = Assert.Single(result.Value);

        Assert.Equal(2, source.PixelCount);
    }

    [Fact]
    public void Detect_TwoSeparatedBlocks_ReturnsTwoRegionsRankedByFlux()
    {
        var pixels = BuildImageWithOneBlock();

        pixels[(0 * Width) + 0] = 500f;

        var result = SourceDetector.Detect(pixels, Width, Height, minimumArea: 1);

        Assert.True(result.IsSuccess);

        Assert.Equal(2, result.Value.Length);

        Assert.Equal(9, result.Value[0].PixelCount);

        Assert.Equal(1, result.Value[1].PixelCount);

        Assert.Equal(1, result.Value[0].Id);

        Assert.Equal(2, result.Value[1].Id);
    }

    [Fact]
    public void Detect_WithMaxSourcesOne_ReturnsOnlyTheBrightestRegion()
    {
        var pixels = BuildImageWithOneBlock();

        pixels[(0 * Width) + 0] = 500f;

        var result = SourceDetector.Detect(pixels, Width, Height, minimumArea: 1, maxSources: 1);

        Assert.True(result.IsSuccess);

        var source = Assert.Single(result.Value);

        Assert.Equal(9, source.PixelCount);
    }

    [Fact]
    public void Detect_NaNPixelAdjacentToBlock_IsNeverAbsorbedIntoTheRegion()
    {
        var pixels = BuildImageWithOneBlock();

        pixels[(3 * Width) + 5] = float.NaN;

        var result = SourceDetector.Detect(pixels, Width, Height);

        Assert.True(result.IsSuccess);

        var source = Assert.Single(result.Value);

        Assert.Equal(9, source.PixelCount);
    }

    [Fact]
    public void Detect_OnUniformImage_ReturnsNoSources()
    {
        var pixels = new float[Width * Height];

        Array.Fill(pixels, 5.0f);

        var result = SourceDetector.Detect(pixels, Width, Height);

        Assert.True(result.IsSuccess);

        Assert.Empty(result.Value);
    }

    [Fact]
    public void Detect_IsDeterministicAcrossRepeatedCalls()
    {
        var pixels = BuildImageWithOneBlock();

        var first = SourceDetector.Detect(pixels, Width, Height).Value;

        var second = SourceDetector.Detect(pixels, Width, Height).Value;

        Assert.Equal(first.Length, second.Length);

        for (var i = 0; i < first.Length; i++)
        {
            Assert.Equal(first[i], second[i]);
        }
    }

    [Fact]
    public void Detect_RejectsMismatchedImageBounds()
    {
        var result = SourceDetector.Detect([1f, 2f, 3f], width: 2, height: 2);

        Assert.True(result.IsFailure);

        Assert.Equal("sources.invalid_image_bounds", result.Error.Code);
    }

    [Fact]
    public void Detect_RejectsNonPositiveThreshold()
    {
        var pixels = BuildImageWithOneBlock();

        var result = SourceDetector.Detect(pixels, Width, Height, thresholdSigma: 0.0);

        Assert.True(result.IsFailure);

        Assert.Equal("sources.invalid_threshold", result.Error.Code);
    }

    [Fact]
    public void Detect_RejectsNonPositiveMinimumArea()
    {
        var pixels = BuildImageWithOneBlock();

        var result = SourceDetector.Detect(pixels, Width, Height, minimumArea: 0);

        Assert.True(result.IsFailure);

        Assert.Equal("sources.invalid_minimum_area", result.Error.Code);
    }

    [Fact]
    public void Detect_RejectsNonPositiveMaxSources()
    {
        var pixels = BuildImageWithOneBlock();

        var result = SourceDetector.Detect(pixels, Width, Height, maxSources: 0);

        Assert.True(result.IsFailure);

        Assert.Equal("sources.invalid_max_sources", result.Error.Code);
    }
}
