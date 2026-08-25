using System.Collections.Immutable;
using AstroLab.Core.Fits;

namespace AstroLab.Api.Features.Fits.Inspect;

public sealed record FitsHduSummaryDto(int Index, HduType Type, ImmutableList<int> NAxes);

/// <summary>Static factory accompanying <see cref="FitsHduSummaryDto"/>. Validates arguments before constructing.</summary>
public static class FitsHduSummaryDtoFactory
{
    public static FitsHduSummaryDto Create(int index, HduType type, ImmutableList<int> nAxes)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        return new FitsHduSummaryDto(index, type, nAxes);
    }

    public static FitsHduSummaryDto Create(int index, HduType type, ImmutableArray<int> nAxes) =>
        Create(index, type, nAxes.ToImmutableList());
}
