using AstroLab.Core.Imaging;

namespace AstroLab.Api.Features.Images.Contours;

public sealed record ContoursRequest
{
    private ContoursRequest(double[]? levels, int? levelCount)
    {
        Levels = levels;
        LevelCount = levelCount;
    }

    public double[]? Levels { get; }

    public int? LevelCount { get; }

    public static ContoursRequest Create(double[]? levels = null, int? levelCount = null)
    {
        if (levelCount is { } levelCountValue)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(levelCountValue);

            ArgumentOutOfRangeException.ThrowIfGreaterThan(levelCountValue, ImageContourGenerator.MaxLevelCount);
        }

        if (levels is not null)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(levels.Length, ImageContourGenerator.MaxLevelCount, nameof(levels));

            if (!Array.TrueForAll(levels, double.IsFinite))
            {
                throw new ArgumentException("Every contour level must be finite.", nameof(levels));
            }
        }

        return new ContoursRequest(levels, levelCount);
    }
}
