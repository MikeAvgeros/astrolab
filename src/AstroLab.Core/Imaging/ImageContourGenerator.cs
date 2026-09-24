using System.Collections.Immutable;
using AstroLab.Core.Result;

namespace AstroLab.Core.Imaging;

/// <summary>
/// Pure marching-squares contour tracing over a 2D pixel buffer: for a given level, walks every
/// 2x2 cell of the grid, classifies which corners lie at or above the level, and emits a linearly
/// interpolated line segment (or, for the two ambiguous "saddle" cases, two segments) crossing that
/// cell's edges. Segments are returned individually rather than stitched into long polylines — for
/// visualisation geometry this is a reasonable simplification that keeps the algorithm's
/// correctness easy to verify, at the cost of not merging adjacent cells' segments into one path.
/// </summary>
public static class ImageContourGenerator
{
    public const int MaxLevelCount = 256;
    private const double DefaultLowerPercentile = 1.0;
    private const double DefaultUpperPercentile = 99.0;

    public static Result<double[]> SuggestLevels(ReadOnlySpan<float> pixels, int levelCount)
    {
        if (levelCount is <= 0 or > MaxLevelCount)
        {
            return Error.Validation(
                "imaging.contours.invalid_level_count", $"levelCount must be between 1 and {MaxLevelCount}.");
        }

        var boundsResult = ImageStatistics.ComputePercentileBounds(pixels, DefaultLowerPercentile, DefaultUpperPercentile);

        if (boundsResult.IsFailure)
        {
            return Result<double[]>.Failure(boundsResult.Error);
        }

        var (lower, upper) = boundsResult.Value;

        var levels = new double[levelCount];

        if (levelCount == 1)
        {
            levels[0] = (lower + upper) / 2.0;

            return levels;
        }

        var step = (upper - lower) / (levelCount - 1);

        for (var i = 0; i < levelCount; i++)
        {
            levels[i] = lower + i * step;
        }

        return levels;
    }

    public static Result<ImmutableArray<ImmutableArray<(double X, double Y)>>> Trace(
        ReadOnlySpan<float> pixels, int width, int height, double level)
    {
        if (width <= 0 || height <= 0 || pixels.Length != width * height)
        {
            return Error.Validation(
                "imaging.contours.invalid_image_bounds",
                $"Pixel span length ({pixels.Length}) does not match width x height ({width}x{height}).");
        }

        var builder = ImmutableArray.CreateBuilder<ImmutableArray<(double X, double Y)>>();

        if (width < 2 || height < 2)
        {
            return builder.ToImmutable();
        }

        for (var y = 0; y < height - 1; y++)
        {
            for (var x = 0; x < width - 1; x++)
            {
                var topLeft = pixels[y * width + x];

                var topRight = pixels[y * width + x + 1];

                var bottomLeft = pixels[(y + 1) * width + x];

                var bottomRight = pixels[(y + 1) * width + x + 1];

                if (!float.IsFinite(topLeft) || !float.IsFinite(topRight) || !float.IsFinite(bottomLeft) || !float.IsFinite(bottomRight))
                {
                    continue;
                }

                TraceCell(builder, x, y, topLeft, topRight, bottomRight, bottomLeft, level);
            }
        }

        return builder.ToImmutable();
    }

    private static void TraceCell(
        ImmutableArray<ImmutableArray<(double X, double Y)>>.Builder builder,
        int x, int y, float topLeft, float topRight, float bottomRight, float bottomLeft, double level)
    {
        var a = topLeft >= level;

        var b = topRight >= level;

        var c = bottomRight >= level;

        var d = bottomLeft >= level;

        var caseIndex = (a ? 1 : 0) | (b ? 2 : 0) | (c ? 4 : 0) | (d ? 8 : 0);

        if (caseIndex is 0 or 15)
        {
            return;
        }

        var top = Interpolate(x, y, x + 1, y, topLeft, topRight, level);

        var right = Interpolate(x + 1, y, x + 1, y + 1, topRight, bottomRight, level);

        var bottom = Interpolate(x, y + 1, x + 1, y + 1, bottomLeft, bottomRight, level);

        var left = Interpolate(x, y, x, y + 1, topLeft, bottomLeft, level);

        switch (caseIndex)
        {
            case 1 or 14:
                AddSegment(builder, left, top);
                break;
            case 2 or 13:
                AddSegment(builder, top, right);
                break;
            case 3 or 12:
                AddSegment(builder, left, right);
                break;
            case 4 or 11:
                AddSegment(builder, right, bottom);
                break;
            case 6 or 9:
                AddSegment(builder, top, bottom);
                break;
            case 7 or 8:
                AddSegment(builder, left, bottom);
                break;
            case 5:
                var centerAverage5 = (topLeft + topRight + bottomRight + bottomLeft) / 4.0;
                if (centerAverage5 >= level)
                {
                    AddSegment(builder, top, right);
                    AddSegment(builder, left, bottom);
                }
                else
                {
                    AddSegment(builder, left, top);
                    AddSegment(builder, right, bottom);
                }
                break;
            case 10:
                var centerAverage10 = (topLeft + topRight + bottomRight + bottomLeft) / 4.0;
                if (centerAverage10 >= level)
                {
                    AddSegment(builder, left, top);
                    AddSegment(builder, right, bottom);
                }
                else
                {
                    AddSegment(builder, top, right);
                    AddSegment(builder, left, bottom);
                }
                break;
        }
    }

    private static void AddSegment(
        ImmutableArray<ImmutableArray<(double X, double Y)>>.Builder builder, (double X, double Y) first, (double X, double Y) second) =>
        builder.Add([first, second]);

    private static (double X, double Y) Interpolate(double x1, double y1, double x2, double y2, double v1, double v2, double level)
    {
        if (v1 == v2)
        {
            return ((x1 + x2) / 2.0, (y1 + y2) / 2.0);
        }

        var t = (level - v1) / (v2 - v1);

        t = Math.Clamp(t, 0.0, 1.0);

        return (x1 + t * (x2 - x1), y1 + t * (y2 - y1));
    }
}
