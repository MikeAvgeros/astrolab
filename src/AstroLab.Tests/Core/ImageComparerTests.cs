using AstroLab.Core.Imaging;

namespace AstroLab.Tests.Core;

public class ImageComparerTests
{
    [Fact]
    public void Compare_OnKnownArrays_ProducesExpectedDifferenceStatistics()
    {
        ReadOnlySpan<float> pixels = [1f, 2f, 3f, 4f];

        ReadOnlySpan<float> comparisonPixels = [2f, 2f, 3f, 8f];

        var result = ImageComparer.Compare(pixels, comparisonPixels, width: 4, height: 1);

        Assert.True(result.IsSuccess);

        var stats = result.Value;

        // differences: 1, 0, 0, 4 -> mean 1.25, max abs 4
        Assert.Equal(1.25, stats.MeanDifference, precision: 6);

        Assert.Equal(4.0, stats.MaxAbsoluteDifference, precision: 6);

        var expectedVariance = ((1 - 1.25) * (1 - 1.25) + (0 - 1.25) * (0 - 1.25) + (0 - 1.25) * (0 - 1.25) + (4 - 1.25) * (4 - 1.25)) / 4.0;

        Assert.Equal(Math.Sqrt(expectedVariance), stats.StandardDeviationDifference, precision: 6);
    }

    [Fact]
    public void Compare_IgnoresNonFinitePixelPairs()
    {
        ReadOnlySpan<float> pixels = [1f, float.NaN, 3f];

        ReadOnlySpan<float> comparisonPixels = [2f, 5f, 3f];

        var result = ImageComparer.Compare(pixels, comparisonPixels, width: 3, height: 1);

        Assert.True(result.IsSuccess);

        Assert.Equal(0.5, result.Value.MeanDifference, precision: 6);
    }

    [Fact]
    public void Compare_OnMismatchedDimensions_ReturnsValidationError()
    {
        ReadOnlySpan<float> pixels = [1f, 2f];

        ReadOnlySpan<float> comparisonPixels = [1f, 2f, 3f];

        var result = ImageComparer.Compare(pixels, comparisonPixels, width: 2, height: 1);

        Assert.True(result.IsFailure);

        Assert.Equal("imaging.compare.invalid_image_bounds", result.Error.Code);
    }

    [Fact]
    public void Compare_WhenAllDifferencesNonFinite_ReturnsValidationError()
    {
        ReadOnlySpan<float> pixels = [float.NaN, float.PositiveInfinity];

        ReadOnlySpan<float> comparisonPixels = [1f, 2f];

        var result = ImageComparer.Compare(pixels, comparisonPixels, width: 2, height: 1);

        Assert.True(result.IsFailure);

        Assert.Equal("imaging.compare.no_valid_pixels", result.Error.Code);
    }
}
