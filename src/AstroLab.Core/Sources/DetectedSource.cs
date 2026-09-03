namespace AstroLab.Core.Sources;

/// <summary>
/// A candidate astronomical source detected by <see cref="SourceDetector"/>: a connected group of
/// pixels significantly above the estimated image background, with basic aperture-free photometry.
/// </summary>
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

    /// <summary>1-based rank, assigned after sorting by <see cref="TotalFlux"/> descending.</summary>
    public int Id { get; }

    /// <summary>Flux-weighted centroid X, in this API's 0-indexed pixel-center coordinate convention (see <c>AstroLab.Core.Astrometry.Wcs</c>).</summary>
    public double PixelX { get; }

    /// <summary>Flux-weighted centroid Y, in this API's 0-indexed pixel-center coordinate convention.</summary>
    public double PixelY { get; }

    /// <summary>The number of 8-connected pixels comprising this source.</summary>
    public int PixelCount { get; }

    /// <summary>The single brightest raw pixel value within the source.</summary>
    public double PeakValue { get; }

    /// <summary>Background-subtracted flux, summed over every pixel in the source.</summary>
    public double TotalFlux { get; }

    /// <summary>The image-wide background level subtracted to compute <see cref="TotalFlux"/>.</summary>
    public double Background { get; }

    /// <summary>Background-noise-limited significance: <c>TotalFlux / (sigma * sqrt(PixelCount))</c>.</summary>
    public double SignalToNoiseRatio { get; }

    public static DetectedSource Create(int id, double pixelX, double pixelY, int pixelCount, double peakValue, double totalFlux, double background, double signalToNoiseRatio)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pixelCount);

        return new DetectedSource(id, pixelX, pixelY, pixelCount, peakValue, totalFlux, background, signalToNoiseRatio);
    }
}
