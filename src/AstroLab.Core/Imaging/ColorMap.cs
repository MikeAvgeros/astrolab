namespace AstroLab.Core.Imaging;

/// <summary>The palette used to map a normalized grayscale intensity onto an RGB color.</summary>
public enum ColorMap
{
    /// <summary>Identity mapping: R = G = B = intensity.</summary>
    Grayscale,

    /// <summary>Perceptually-uniform blue-to-yellow palette (approximates matplotlib's "viridis").</summary>
    Viridis,

    /// <summary>Black-red-yellow-white thermal palette, common for astronomical display.</summary>
    Hot,
}
