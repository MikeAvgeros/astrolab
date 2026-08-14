namespace AstroLab.Core.Imaging;

/// <summary>The non-linear intensity transform applied to a black/white-point-normalized pixel value.</summary>
public enum StretchMode
{
    /// <summary>No transform: output is directly proportional to input.</summary>
    Linear,

    /// <summary>Compresses bright values, expanding faint detail — suited to high-dynamic-range images.</summary>
    Logarithmic,

    /// <summary>A gentler expansion of faint detail than <see cref="Logarithmic"/>.</summary>
    SquareRoot,

    /// <summary>
    /// Inverse hyperbolic sine stretch: behaves linearly near zero and logarithmically at the
    /// extremes, controlled by a softening parameter. Common for astronomical display because it
    /// handles both faint and saturated regions gracefully.
    /// </summary>
    Asinh,
}
