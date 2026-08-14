namespace AstroLab.Core.Photometry;

/// <summary>The statistic used to summarise per-pixel background samples drawn from an annulus.</summary>
public enum BackgroundEstimationMethod
{
    /// <summary>Arithmetic mean of sampled pixels. Sensitive to contaminating sources in the annulus.</summary>
    Mean,

    /// <summary>Median of sampled pixels. Robust to a modest number of contaminating sources.</summary>
    Median,
}
