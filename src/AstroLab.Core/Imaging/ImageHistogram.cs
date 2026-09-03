using System.Collections.Immutable;

namespace AstroLab.Core.Imaging;

/// <summary>
/// A fixed-width histogram of pixel values, suitable for rendering by an API client without
/// requiring it to receive the (potentially gigapixel) source pixel array.
/// </summary>
public readonly record struct ImageHistogram
{
    private ImageHistogram(ImmutableArray<double> binEdges, ImmutableArray<long> counts, long validPixelCount)
    {
        BinEdges = binEdges;
        Counts = counts;
        ValidPixelCount = validPixelCount;
    }

    /// <summary>Bin boundaries, length <see cref="BinCount"/> + 1: edges[i] and edges[i + 1] bound bin i.</summary>
    public ImmutableArray<double> BinEdges { get; }

    /// <summary>The number of valid (finite) pixels falling into each bin.</summary>
    public ImmutableArray<long> Counts { get; }

    public int BinCount => Counts.Length;

    /// <summary>The total number of finite pixels represented across all bins.</summary>
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
