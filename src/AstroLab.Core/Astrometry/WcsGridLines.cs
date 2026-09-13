using System.Collections.Immutable;

namespace AstroLab.Core.Astrometry;

/// <summary>Pixel-space polylines for a WCS coordinate grid: lines of constant right ascension, and lines of constant declination.</summary>
public readonly record struct WcsGridLines
{
    private WcsGridLines(
        ImmutableArray<ImmutableArray<(double X, double Y)>> rightAscensionLines,
        ImmutableArray<ImmutableArray<(double X, double Y)>> declinationLines)
    {
        RightAscensionLines = rightAscensionLines;
        DeclinationLines = declinationLines;
    }
    
    public ImmutableArray<ImmutableArray<(double X, double Y)>> RightAscensionLines { get; }
    
    public ImmutableArray<ImmutableArray<(double X, double Y)>> DeclinationLines { get; }

    public static WcsGridLines Create(
        ImmutableArray<ImmutableArray<(double X, double Y)>> rightAscensionLines,
        ImmutableArray<ImmutableArray<(double X, double Y)>> declinationLines) =>
        new(rightAscensionLines, declinationLines);
}
