using System.Collections.Immutable;
using AstroLab.Core.Fits;

namespace AstroLab.Api.Features.Fits.Inspect;

public sealed record FitsHduSummaryDto
{
    private FitsHduSummaryDto(int index, HduType type, string? extensionName, BitPixType? dataType, int numberOfAxes, ImmutableList<int> axisDimensions, ImmutableList<FitsKeywordDto> header)
    {
        Index = index;
        Type = type;
        ExtensionName = extensionName;
        DataType = dataType;
        NumberOfAxes = numberOfAxes;
        AxisDimensions = axisDimensions;
        Header = header;
    }

    public int Index { get; }

    public HduType Type { get; }

    public string? ExtensionName { get; }

    public BitPixType? DataType { get; }

    public int NumberOfAxes { get; }

    public ImmutableList<int> AxisDimensions { get; }

    public ImmutableList<FitsKeywordDto> Header { get; }

    public static FitsHduSummaryDto Create(int index, HduType type, string? extensionName, BitPixType? dataType, ImmutableList<int> axisDimensions, ImmutableList<FitsKeywordDto> header)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        return new FitsHduSummaryDto(index, type, extensionName, dataType, axisDimensions.Count, axisDimensions, header);
    }
}
