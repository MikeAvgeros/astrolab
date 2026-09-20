using System.Collections.Immutable;
using AstroLab.Core.Astrometry;
using AstroLab.Core.Sources;

namespace AstroLab.Infrastructure.ImageRendering;

/// <summary>
/// Draws source-position markers and WCS coordinate grids directly into an already-rendered
/// image's RGB buffer, compositing Core detection/geometry results onto a browser-displayable
/// overlay. Pixel byte manipulation for a concrete visual representation is a rendering concern,
/// not a scientific one, which is why it lives alongside <see cref="FitsImageRenderer"/> in
/// Infrastructure rather than Core.
/// </summary>
public static class OverlayRenderer
{
    private const int MarkerRadiusPixels = 6;
    private const int MarkerRingThicknessPixels = 1;
    private const byte MarkerRed = 255;
    private const byte MarkerGreen = 40;
    private const byte MarkerBlue = 40;
    private const int RgbChannelCount = 3;
    private const byte GridLineRed = 60;
    private const byte GridLineGreen = 220;
    private const byte GridLineBlue = 220;
    
    public static RenderedImage DrawSourceMarkers(
        RenderedImage image, IReadOnlyList<DetectedSource> sources, int sourceWidth, int sourceHeight)
    {
        var (scaleX, scaleY) = ComputeScale(image, sourceWidth, sourceHeight);

        foreach (var source in sources)
        {
            DrawMarkerRing(image, (int)Math.Round(source.PixelX * scaleX), (int)Math.Round(source.PixelY * scaleY));
        }

        return image;
    }
    
    public static RenderedImage DrawGridLines(RenderedImage image, WcsGridLines grid, int sourceWidth, int sourceHeight)
    {
        var (scaleX, scaleY) = ComputeScale(image, sourceWidth, sourceHeight);

        foreach (var line in grid.RightAscensionLines)
        {
            DrawPolyline(image, line, scaleX, scaleY);
        }

        foreach (var line in grid.DeclinationLines)
        {
            DrawPolyline(image, line, scaleX, scaleY);
        }

        return image;
    }

    private static (double ScaleX, double ScaleY) ComputeScale(RenderedImage image, int sourceWidth, int sourceHeight) =>
        (sourceWidth > 0 ? image.Width / (double)sourceWidth : 1.0,
            sourceHeight > 0 ? image.Height / (double)sourceHeight : 1.0);

    private static void DrawPolyline(RenderedImage image, ImmutableArray<(double X, double Y)> points, double scaleX, double scaleY)
    {
        for (var i = 1; i < points.Length; i++)
        {
            DrawLine(
                image,
                (points[i - 1].X * scaleX, points[i - 1].Y * scaleY),
                (points[i].X * scaleX, points[i].Y * scaleY));
        }
    }

    private static void DrawLine(RenderedImage image, (double X, double Y) start, (double X, double Y) end)
    {
        var deltaX = end.X - start.X;

        var deltaY = end.Y - start.Y;

        var stepCount = (int)Math.Ceiling(Math.Max(Math.Abs(deltaX), Math.Abs(deltaY)));

        if (stepCount <= 0)
        {
            PlotPixel(image, (int)Math.Round(start.X), (int)Math.Round(start.Y));

            return;
        }

        for (var step = 0; step <= stepCount; step++)
        {
            var t = (double)step / stepCount;

            var x = start.X + (t * deltaX);

            var y = start.Y + (t * deltaY);

            PlotPixel(image, (int)Math.Round(x), (int)Math.Round(y));
        }
    }

    private static void PlotPixel(RenderedImage image, int x, int y)
    {
        if (x < 0 || x >= image.Width || y < 0 || y >= image.Height)
        {
            return;
        }

        var pixelOffset = ((y * image.Width) + x) * RgbChannelCount;

        image.Rgb[pixelOffset] = GridLineRed;

        image.Rgb[pixelOffset + 1] = GridLineGreen;

        image.Rgb[pixelOffset + 2] = GridLineBlue;
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
