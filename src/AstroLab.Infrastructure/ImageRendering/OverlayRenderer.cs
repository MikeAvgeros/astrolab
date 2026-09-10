using AstroLab.Core.Sources;

namespace AstroLab.Infrastructure.ImageRendering;

/// <summary>
/// Draws source-position markers directly into an already-rendered image's RGB buffer,
/// compositing Core detection results onto a browser-displayable overlay. Pixel byte manipulation
/// for a concrete visual representation is a rendering concern, not a scientific one, which is why
/// it lives alongside <see cref="FitsImageRenderer"/> in Infrastructure rather than Core.
/// </summary>
public static class OverlayRenderer
{
    private const int MarkerRadiusPixels = 6;
    private const int MarkerRingThicknessPixels = 1;
    private const byte MarkerRed = 255;
    private const byte MarkerGreen = 40;
    private const byte MarkerBlue = 40;
    private const int RgbChannelCount = 3;

    public static RenderedImage DrawSourceMarkers(RenderedImage image, IReadOnlyList<DetectedSource> sources)
    {
        foreach (var source in sources)
        {
            DrawMarkerRing(image, (int)Math.Round(source.PixelX), (int)Math.Round(source.PixelY));
        }

        return image;
    }

    private static void DrawMarkerRing(RenderedImage image, int centerX, int centerY)
    {
        var outerRadiusSquared = MarkerRadiusPixels * MarkerRadiusPixels;

        var innerRadius = MarkerRadiusPixels - MarkerRingThicknessPixels;

        var innerRadiusSquared = innerRadius * innerRadius;

        for (var y = centerY - MarkerRadiusPixels; y <= centerY + MarkerRadiusPixels; y++)
        {
            if (y < 0 || y >= image.Height)
            {
                continue;
            }

            for (var x = centerX - MarkerRadiusPixels; x <= centerX + MarkerRadiusPixels; x++)
            {
                if (x < 0 || x >= image.Width)
                {
                    continue;
                }

                var dx = x - centerX;

                var dy = y - centerY;

                var distanceSquared = dx * dx + dy * dy;

                if (distanceSquared > outerRadiusSquared || distanceSquared < innerRadiusSquared)
                {
                    continue;
                }

                var pixelOffset = (y * image.Width + x) * RgbChannelCount;

                image.Rgb[pixelOffset] = MarkerRed;

                image.Rgb[pixelOffset + 1] = MarkerGreen;

                image.Rgb[pixelOffset + 2] = MarkerBlue;
            }
        }
    }
}
