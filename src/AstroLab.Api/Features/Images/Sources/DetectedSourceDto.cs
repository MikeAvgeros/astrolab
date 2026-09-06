using AstroLab.Core.Sources;

namespace AstroLab.Api.Features.Images.Sources;

public sealed record DetectedSourceDto
{
    private DetectedSourceDto(
        int id, double pixelX, double pixelY, double? rightAscension, double? declination,
        int pixelCount, double peakValue, double totalFlux, double background, double signalToNoiseRatio)
    {
        Id = id;
        PixelX = pixelX;
        PixelY = pixelY;
        RightAscension = rightAscension;
        Declination = declination;
        PixelCount = pixelCount;
        PeakValue = peakValue;
        TotalFlux = totalFlux;
        Background = background;
        SignalToNoiseRatio = signalToNoiseRatio;
    }

    public int Id { get; }

    public double PixelX { get; }

    public double PixelY { get; }

    public double? RightAscension { get; }

    public double? Declination { get; }

    public int PixelCount { get; }

    public double PeakValue { get; }

    public double TotalFlux { get; }

    public double Background { get; }

    public double SignalToNoiseRatio { get; }

    public static DetectedSourceDto Create(DetectedSource source, double? rightAscension, double? declination)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(source.Id);
        
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(source.PixelCount);

        return new DetectedSourceDto(
            source.Id,
            source.PixelX,
            source.PixelY,
            rightAscension,
            declination,
            source.PixelCount,
            source.PeakValue,
            source.TotalFlux,
            source.Background,
            source.SignalToNoiseRatio);
    }
}
