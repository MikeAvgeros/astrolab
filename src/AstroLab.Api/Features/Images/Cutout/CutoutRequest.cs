namespace AstroLab.Api.Features.Images.Cutout;

public sealed record CutoutRequest
{
    private CutoutRequest(
        int? x, int? y, int? width, int? height,
        double? rightAscension, double? declination, double? radiusArcseconds)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
        RightAscension = rightAscension;
        Declination = declination;
        RadiusArcseconds = radiusArcseconds;
    }

    public int? X { get; }

    public int? Y { get; }

    public int? Width { get; }

    public int? Height { get; }

    public double? RightAscension { get; }

    public double? Declination { get; }

    public double? RadiusArcseconds { get; }

    public static CutoutRequest Create(
        int? x, int? y, int? width, int? height,
        double? rightAscension, double? declination, double? radiusArcseconds)
    {
        var hasPixelRegion = width.HasValue || height.HasValue || x.HasValue || y.HasValue;

        var hasSkyRegion = rightAscension.HasValue || declination.HasValue || radiusArcseconds.HasValue;

        if (!hasPixelRegion && !hasSkyRegion)
        {
            throw new ArgumentException("A cutout requires either a pixel region (x, y, width, height) or a sky region (rightAscension, declination, radiusArcseconds).");
        }

        if (width is { } widthValue)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(widthValue);
        }

        if (height is { } heightValue)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(heightValue);
        }

        if (radiusArcseconds is { } radiusValue)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(radiusValue);
        }

        return new CutoutRequest(x, y, width, height, rightAscension, declination, radiusArcseconds);
    }
}
