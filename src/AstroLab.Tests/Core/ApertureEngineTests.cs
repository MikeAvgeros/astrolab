using AstroLab.Core.Photometry;

namespace AstroLab.Tests.Core;

public class ApertureEngineTests
{
    private static float[] CreateUniformImage(int width, int height, float value)
    {
        var pixels = new float[width * height];
        Array.Fill(pixels, value);
        return pixels;
    }

    [Fact]
    public void MeasureCircularAperture_OnUniformImage_MatchesAnalyticCircleArea()
    {
        const int size = 121;
        const double radius = 10.0;
        const float value = 5.0f;
        var pixels = CreateUniformImage(size, size, value);

        var result = ApertureEngine.MeasureCircularAperture(pixels, size, size, 60.0, 60.0, radius);

        Assert.True(result.IsSuccess);
        var expectedArea = Math.PI * radius * radius;
        Assert.True(Math.Abs(result.Value.Area - expectedArea) / expectedArea < 0.01);
        Assert.True(Math.Abs(result.Value.Flux - (value * expectedArea)) / (value * expectedArea) < 0.01);
        Assert.Equal(value, result.Value.MeanValue, precision: 3);
    }

    [Fact]
    public void MeasureCircularAperture_ExcludesNonFinitePixels_FromFluxAndArea()
    {
        const int size = 121;
        const double radius = 10.0;
        const float value = 5.0f;
        var pixels = CreateUniformImage(size, size, value);

        var baseline = ApertureEngine.MeasureCircularAperture(pixels, size, size, 60.0, 60.0, radius).Value;

        pixels[(60 * size) + 60] = float.NaN;
        var withNaN = ApertureEngine.MeasureCircularAperture(pixels, size, size, 60.0, 60.0, radius).Value;

        Assert.Equal(baseline.Area - 1.0, withNaN.Area, precision: 6);
        Assert.Equal(baseline.Flux - value, withNaN.Flux, precision: 3);
    }

    [Fact]
    public void MeasureCircularAperture_RejectsMismatchedDimensions()
    {
        var pixels = new float[10];

        var result = ApertureEngine.MeasureCircularAperture(pixels, 5, 5, 2, 2, 1);

        Assert.True(result.IsFailure);
        Assert.Equal("photometry.invalid_image_bounds", result.Error.Code);
    }

    [Fact]
    public void MeasureAnnulusBackground_Median_IsRobustToOutliers()
    {
        const int size = 121;
        var pixels = CreateUniformImage(size, size, 10.0f);

        for (var i = 0; i < 5; i++)
        {
            pixels[(60 * size) + 85 + i] = 5000.0f;
        }

        var median = ApertureEngine.MeasureAnnulusBackground(
            pixels, size, size, 60.0, 60.0, 20.0, 30.0, BackgroundEstimationMethod.Median);
        var mean = ApertureEngine.MeasureAnnulusBackground(
            pixels, size, size, 60.0, 60.0, 20.0, 30.0, BackgroundEstimationMethod.Mean);

        Assert.True(median.IsSuccess);
        Assert.True(mean.IsSuccess);
        Assert.Equal(10.0, median.Value.BackgroundPerPixel, precision: 3);
        Assert.True(mean.Value.BackgroundPerPixel > 10.5);
    }

    [Fact]
    public void MeasureAnnulusBackground_RejectsInnerRadiusGreaterThanOuter()
    {
        var pixels = CreateUniformImage(21, 21, 1.0f);

        var result = ApertureEngine.MeasureAnnulusBackground(pixels, 21, 21, 10, 10, 8.0, 4.0);

        Assert.True(result.IsFailure);
        Assert.Equal("photometry.invalid_annulus", result.Error.Code);
    }

    [Fact]
    public void MeasureNetFlux_OnFlatField_IsApproximatelyZero()
    {
        const int size = 121;
        var pixels = CreateUniformImage(size, size, 42.0f);

        var result = ApertureEngine.MeasureNetFlux(
            pixels, size, size, 60.0, 60.0, apertureRadius: 8.0, annulusInnerRadius: 12.0, annulusOuterRadius: 18.0);

        Assert.True(result.IsSuccess);
        Assert.True(Math.Abs(result.Value.NetFlux) < 0.5);
    }
}
