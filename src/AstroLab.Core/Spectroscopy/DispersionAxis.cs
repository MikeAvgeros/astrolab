namespace AstroLab.Core.Spectroscopy;

/// <summary>The image axis along which wavelength varies (the "dispersion" direction).</summary>
public enum DispersionAxis
{
    /// <summary>Wavelength varies along image columns (x); extraction collapses rows (y).</summary>
    Horizontal,

    /// <summary>Wavelength varies along image rows (y); extraction collapses columns (x).</summary>
    Vertical,
}
