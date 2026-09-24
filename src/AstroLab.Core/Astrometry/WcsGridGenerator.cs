using System.Collections.Immutable;
using AstroLab.Core.Result;

namespace AstroLab.Core.Astrometry;

/// <summary>
/// Pure generation of WCS coordinate-grid geometry: samples the image's sky footprint from its
/// corners (and center), lays out evenly-spaced right-ascension and declination gridlines spanning
/// that footprint, and traces each line's pixel-space path by walking the other coordinate and
/// converting through the WCS. Points that fall outside the image, or where the WCS projection has
/// no solution (e.g. near its singular point), simply end the current polyline rather than failing
/// the whole request — a coordinate grid is inherently a best-effort visual aid.
/// </summary>
public static class WcsGridGenerator
{
    public const int DefaultLinesPerAxis = 6;
    public const int MaxLinesPerAxis = 100;

    private const int SamplesPerLine = 50;
    private const double MinDeclinationDegrees = -90.0;
    private const double MaxDeclinationDegrees = 90.0;
    private const double HalfCircleDegrees = 180.0;
    private const double FullCircleDegrees = 360.0;
    private const double BoundsTolerancePixels = 1e-6;

    public static Result<WcsGridLines> GenerateGridLines(Wcs wcs, int width, int height, int linesPerAxis = DefaultLinesPerAxis)
    {
        if (width <= 0 || height <= 0)
        {
            return Error.Validation("astrometry.wcsgrid.invalid_image_bounds", "width and height must both be positive.");
        }

        if (linesPerAxis is <= 0 or > MaxLinesPerAxis)
        {
            return Error.Validation(
                "astrometry.wcsgrid.invalid_line_count", $"linesPerAxis must be between 1 and {MaxLinesPerAxis}.");
        }

        var footprintResult = SampleFootprint(wcs, width, height);

        if (footprintResult.IsFailure)
        {
            return Result<WcsGridLines>.Failure(footprintResult.Error);
        }

        var (raMin, raMax, decMin, decMax) = footprintResult.Value;

        var raValues = LinSpace(raMin, raMax, linesPerAxis);

        var decValues = LinSpace(Math.Max(decMin, MinDeclinationDegrees), Math.Min(decMax, MaxDeclinationDegrees), linesPerAxis);

        var raLines = ImmutableArray.CreateBuilder<ImmutableArray<(double X, double Y)>>();

        foreach (var ra in raValues)
        {
            AppendPolylines(raLines, wcs, width, height, fixedValue: ra, varyingMin: decMin, varyingMax: decMax, varyIsDeclination: true);
        }

        var decLines = ImmutableArray.CreateBuilder<ImmutableArray<(double X, double Y)>>();

        foreach (var dec in decValues)
        {
            AppendPolylines(decLines, wcs, width, height, fixedValue: dec, varyingMin: raMin, varyingMax: raMax, varyIsDeclination: false);
        }

        return WcsGridLines.Create(raLines.ToImmutable(), decLines.ToImmutable());
    }

    private static void AppendPolylines(
        ImmutableArray<ImmutableArray<(double X, double Y)>>.Builder lines,
        Wcs wcs, int width, int height,
        double fixedValue, double varyingMin, double varyingMax, bool varyIsDeclination)
    {
        var current = new List<(double X, double Y)>();

        for (var step = 0; step <= SamplesPerLine; step++)
        {
            var t = (double)step / SamplesPerLine;

            var varyingValue = varyingMin + (t * (varyingMax - varyingMin));

            var rightAscension = varyIsDeclination ? fixedValue : varyingValue;

            var declination = varyIsDeclination ? varyingValue : fixedValue;

            var isPointVisible = declination is >= MinDeclinationDegrees and <= MaxDeclinationDegrees;

            var pixelResult = isPointVisible ? wcs.WorldToPixel(rightAscension, declination) : default;

            if (!isPointVisible || pixelResult.IsFailure)
            {
                FlushPolyline(lines, ref current);

                continue;
            }

            var (pixelX, pixelY) = pixelResult.Value;

            if (pixelX < -BoundsTolerancePixels || pixelX > width + BoundsTolerancePixels ||
                pixelY < -BoundsTolerancePixels || pixelY > height + BoundsTolerancePixels)
            {
                FlushPolyline(lines, ref current);

                continue;
            }

            current.Add((pixelX, pixelY));
        }

        FlushPolyline(lines, ref current);
    }

    private static void FlushPolyline(ImmutableArray<ImmutableArray<(double X, double Y)>>.Builder lines, ref List<(double X, double Y)> current)
    {
        if (current.Count >= 2)
        {
            lines.Add([.. current]);
        }

        current = [];
    }

    private static Result<(double RaMin, double RaMax, double DecMin, double DecMax)> SampleFootprint(Wcs wcs, int width, int height)
    {
        ReadOnlySpan<(double X, double Y)> corners =
        [
            (0, 0),
            (width, 0),
            (width, height),
            (0, height),
            (width / 2.0, height / 2.0),
        ];

        var rightAscensions = new List<double>(corners.Length);

        var declinations = new List<double>(corners.Length);

        foreach (var (x, y) in corners)
        {
            var worldResult = wcs.PixelToWorld(x, y);

            if (worldResult.IsFailure)
            {
                continue;
            }

            rightAscensions.Add(worldResult.Value.RightAscension);

            declinations.Add(worldResult.Value.Declination);
        }

        if (rightAscensions.Count == 0)
        {
            return Error.Validation(
                "astrometry.wcsgrid.no_visible_sky_region", "No corner or center pixel of the image resolved to a valid sky position.");
        }

        var unwrappedRightAscensions = UnwrapDegrees(rightAscensions);

        return (unwrappedRightAscensions.Min(), unwrappedRightAscensions.Max(), declinations.Min(), declinations.Max());
    }
    
    private static double[] UnwrapDegrees(IReadOnlyList<double> values)
    {
        var unwrapped = new double[values.Count];

        unwrapped[0] = values[0];

        for (var i = 1; i < values.Count; i++)
        {
            var delta = values[i] - values[i - 1];

            while (delta > HalfCircleDegrees)
            {
                delta -= FullCircleDegrees;
            }

            while (delta <= -HalfCircleDegrees)
            {
                delta += FullCircleDegrees;
            }

            unwrapped[i] = unwrapped[i - 1] + delta;
        }

        return unwrapped;
    }

    private static double[] LinSpace(double start, double end, int count)
    {
        var values = new double[count];

        if (count == 1)
        {
            values[0] = (start + end) / 2.0;

            return values;
        }

        var step = (end - start) / (count - 1);

        for (var i = 0; i < count; i++)
        {
            values[i] = start + i * step;
        }

        return values;
    }
}
