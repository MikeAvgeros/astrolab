using AstroLab.Core.Imaging;
using AstroLab.Infrastructure.ImageRendering;

namespace AstroLab.Tests.Infrastructure;

public class FitsImageRendererTests
{
    [Fact]
    public void Render_WithExplicitBlackWhitePoints_ProducesExpectedGrayscale()
    {
        float[] pixels = [0f, 5f, 10f];
        
        var options = RenderOptions.Create(StretchMode.Linear, colorMap: ColorMap.Grayscale, blackPoint: 0, whitePoint: 10);

        var result = FitsImageRenderer.Render(pixels, width: 3, height: 1, options);

        Assert.True(result.IsSuccess);
        
        var rgb = result.Value.Rgb;
        
        Assert.Equal(0, rgb[0]);
        
        Assert.Equal(255, rgb[6]);
    }

    [Fact]
    public void Render_WithAutoScale_DerivesBlackWhitePointsFromPercentiles()
    {
        var pixels = new float[1000];
        
        for (var i = 0; i < pixels.Length; i++)
        {
            pixels[i] = i;
        }

        var options = RenderOptions.Create(StretchMode.Linear, colorMap: ColorMap.Grayscale, autoLowerPercentile: 0, autoUpperPercentile: 100);

        var result = FitsImageRenderer.Render(pixels, width: 1000, height: 1, options);

        Assert.True(result.IsSuccess);
        
        var rgb = result.Value.Rgb;

        Assert.True(rgb[0] < 5);
        
        Assert.True(rgb[^1] > 250);
    }

    [Fact]
    public void Render_RejectsMismatchedDimensions()
    {
        float[] pixels = [1f, 2f, 3f];

        var result = FitsImageRenderer.Render(pixels, width: 2, height: 2, RenderOptions.Create());

        Assert.True(result.IsFailure);
        
        Assert.Equal("rendering.invalid_image_bounds", result.Error.Code);
    }

    [Fact]
    public void RenderToPng_ProducesValidPngBytes()
    {
        float[] pixels = [0f, 1f, 2f, 3f];

        var options = RenderOptions.Create(StretchMode.Linear, blackPoint: 0, whitePoint: 3);

        var result = FitsImageRenderer.RenderToPng(pixels, width: 2, height: 2, options);

        Assert.True(result.IsSuccess);

        Assert.Equal([137, 80, 78, 71, 13, 10, 26, 10], result.Value[..8]);
    }

    [Fact]
    public void Render_OversizedImage_DownsamplesBeforeStretching()
    {
        const int width = 4000;
        const int height = 100;

        var pixels = new float[width * height];

        Array.Fill(pixels, 5f);

        var options = RenderOptions.Create(StretchMode.Linear, blackPoint: 0, whitePoint: 10, maxDimension: 2000);

        var result = FitsImageRenderer.Render(pixels, width, height, options);

        Assert.True(result.IsSuccess);

        Assert.Equal(2000, result.Value.Width);

        Assert.Equal(50, result.Value.Height);
    }

    [Fact]
    public void Render_WithinMaxDimension_DoesNotDownsample()
    {
        float[] pixels = [0f, 5f, 10f];

        var options = RenderOptions.Create(StretchMode.Linear, blackPoint: 0, whitePoint: 10, maxDimension: 2000);

        var result = FitsImageRenderer.Render(pixels, width: 3, height: 1, options);

        Assert.True(result.IsSuccess);

        Assert.Equal(3, result.Value.Width);

        Assert.Equal(1, result.Value.Height);
    }

    [Fact]
    public void Render_WithNullMaxDimension_NeverDownsamples()
    {
        const int width = 4000;
        const int height = 100;

        var pixels = new float[width * height];

        Array.Fill(pixels, 5f);

        var options = RenderOptions.Create(StretchMode.Linear, blackPoint: 0, whitePoint: 10, maxDimension: null);

        var result = FitsImageRenderer.Render(pixels, width, height, options);

        Assert.True(result.IsSuccess);

        Assert.Equal(width, result.Value.Width);

        Assert.Equal(height, result.Value.Height);
    }
}
