using System.Collections.Immutable;

namespace AstroLab.Core.Imaging;

public readonly record struct ImageHistogram
{
    private ImageHistogram(ImmutableArray<double> binEdges, ImmutableArray<long> counts, long validPixelCount)
    {
        BinEdges = binEdges;
        Counts = counts;
        ValidPixelCount = validPixelCount;
    }

    public ImmutableArray<double> BinEdges { get; }

    public ImmutableArray<long> Counts { get; }

    public int BinCount => Counts.Length;

    public long ValidPixelCount { get; }

    public static ImageHistogram Create(ImmutableArray<double> binEdges, ImmutableArray<long> counts, long validPixelCount)
    {
        if (binEdges.Length != counts.Length + 1)
        {
            throw new ArgumentException($"binEdges length ({binEdges.Length}) must be counts length ({counts.Length}) + 1.");
        }

        ArgumentOutOfRangeException.ThrowIfNegative(validPixelCount);

        return new ImageHistogram(binEdges, counts, validPixelCount);
    }
}
