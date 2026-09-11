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
        }

        return new ContoursRequest(levels, levelCount);
    }
}
