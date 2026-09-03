namespace AstroLab.Core.Astrometry;

/// <summary>
/// FITS WCS zenithal (azimuthal) projections (Calabretta &amp; Greisen 2002, "Representations of
/// celestial coordinates in FITS", Paper II) — the family used by virtually all optical/IR imaging
/// archives. Every zenithal projection shares the same celestial/native spherical-rotation math
/// (<see cref="Wcs"/>) and differs only in how a native latitude maps to a projection-plane radius
/// (<see cref="WcsProjectionExtensions"/>).
/// </summary>
public enum WcsProjection
{
    /// <summary>Gnomonic (tangent-plane) projection — the de facto standard for CCD/CMOS imaging.</summary>
    Tan,

    /// <summary>Orthographic (slant/synthesis) projection.</summary>
    Sin,

    /// <summary>Zenithal equidistant projection.</summary>
    Arc,
}
