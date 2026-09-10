using AstroLab.Core.Sources;
using AstroLab.Infrastructure.ImageRendering;

namespace AstroLab.Tests.Infrastructure;

public class OverlayRendererTests
{
    private const byte BackgroundGray = 128;

    private static DetectedSource Source(double pixelX, double pixelY) =>
        DetectedSource.Create(1, pixelX, pixelY, pixelCount: 1, peakValue: 100.0, totalFlux: 100.0, background: 0.0, signalToNoiseRatio: 10.0);

    private static RenderedImage BlankImage(int width, int height)
    {
        var rgb = new byte[width * height * 3];

        Array.Fill(rgb, BackgroundGray);

        return RenderedImage.Create(width, height, rgb);
    }

    private static (byte R, byte G, byte B) PixelAt(RenderedImage image, int x, int y)
    {
        var offset = (y * image.Width + x) * 3;

        return (image.Rgb[offset], image.Rgb[offset + 1], image.Rgb[offset + 2]);
    }

    [Fact]
    public void DrawSourceMarkers_PaintsRingAtExpectedRadiusAroundSource()
    {
        var image = BlankImage(20, 20);

        OverlayRenderer.DrawSourceMarkers(image, [Source(10.0, 10.0)]);

        var onOuterEdge = PixelAt(image, 16, 10);

        Assert.NotEqual((BackgroundGray, BackgroundGray, BackgroundGray), onOuterEdge);

        Assert.True(onOuterEdge.R > onOuterEdge.G);
    }

    [Fact]
    public void DrawSourceMarkers_LeavesCenterAndInteriorUntouched()
    {
        var image = BlankImage(20, 20);

        OverlayRenderer.DrawSourceMarkers(image, [Source(10.0, 10.0)]);

        Assert.Equal((BackgroundGray, BackgroundGray, BackgroundGray), PixelAt(image, 10, 10));

        Assert.Equal((BackgroundGray, BackgroundGray, BackgroundGray), PixelAt(image, 14, 10));
    }

    [Fact]
    public void DrawSourceMarkers_LeavesFarAwayPixelsUntouched()
    {
        var image = BlankImage(20, 20);

        OverlayRenderer.DrawSourceMarkers(image, [Source(10.0, 10.0)]);

        Assert.Equal((BackgroundGray, BackgroundGray, BackgroundGray), PixelAt(image, 0, 0));
    }

    [Fact]
    public void DrawSourceMarkers_OnSourceNearImageEdge_ClipsWithoutThrowing()
    {
        var image = BlankImage(10, 10);

        var exception = Record.Exception(() => OverlayRenderer.DrawSourceMarkers(image, [Source(0.0, 0.0), Source(9.0, 9.0)]));

        Assert.Null(exception);
    }

    [Fact]
    public void DrawSourceMarkers_OnNoSources_LeavesImageUnchanged()
    {
        var image = BlankImage(5, 5);

        OverlayRenderer.DrawSourceMarkers(image, []);

        Assert.All(image.Rgb, value => Assert.Equal(BackgroundGray, value));
    }
}
