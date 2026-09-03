namespace AstroLab.Core.Sources;

public readonly record struct DetectedSource
{
    private DetectedSource(int id, double pixelX, double pixelY, int pixelCount, double peakValue, double totalFlux, double background, double signalToNoiseRatio)
    {
        Id = id;
        PixelX = pixelX;
        PixelY = pixelY;
        PixelCount = pixelCount;
        PeakValue = peakValue;
        TotalFlux = totalFlux;
        Background = background;
        SignalToNoiseRatio = signalToNoiseRatio;
    }

    public int Id { get; }

    public double PixelX { get; }

    public double PixelY { get; }

    public int PixelCount { get; }

    public double PeakValue { get; }

    public double TotalFlux { get; }

    public double Background { get; }

    public double SignalToNoiseRatio { get; }

    public static DetectedSource Create(int id, double pixelX, double pixelY, int pixelCount, double peakValue, double totalFlux, double background, double signalToNoiseRatio)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pixelCount);

        return new DetectedSource(id, pixelX, pixelY, pixelCount, peakValue, totalFlux, background, signalToNoiseRatio);
    }
}
