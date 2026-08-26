using System.Collections.Immutable;
using AstroLab.Core.Fits;

namespace AstroLab.Api.Features.Fits.Inspect;

public sealed record FitsHduSummaryDto
{
    private FitsHduSummaryDto(int index, HduType type, ImmutableList<int> nAxes)
    {
        Index = index;
        Type = type;
        NAxes = nAxes;
    }

    public int Index { get; }

    public HduType Type { get; }

    public ImmutableList<int> NAxes { get; }

    public static FitsHduSummaryDto Create(int index, HduType type, ImmutableList<int> nAxes)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        return new FitsHduSummaryDto(index, type, nAxes);
    }

    public static FitsHduSummaryDto Create(int index, HduType type, ImmutableArray<int> nAxes) =>
        Create(index, type, nAxes.ToImmutableList());
}
