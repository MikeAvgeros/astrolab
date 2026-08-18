namespace AstroLab.Core.Fits;

/// <summary>The scientific data type a staged FITS file was classified as, derived from its HDU/header metadata.</summary>
public enum FitsDatasetKind
{
    /// <summary>A 2D (or higher-dimensional) pixel array with no spectral or temporal markers.</summary>
    Image,

    /// <summary>A 1D flux array, or 2D data carrying a dispersion axis / spectral WCS marker.</summary>
    Spectrum,

    /// <summary>A table HDU carrying a <c>TIME</c> column.</summary>
    TimeSeries,

    /// <summary>A table HDU that is neither a time series nor a spectral table.</summary>
    Table,

    /// <summary>No HDU carried enough metadata to classify the file.</summary>
    Unknown,
}
