namespace AstroLab.Core.Astrometry;

/// <summary>How a single WCS axis's <c>CTYPE</c> classifies against the celestial-sphere roles a projection needs.</summary>
internal enum WcsAxisKind
{
    Longitude,
    Latitude,
    Other,
}
