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

    /// <summary>The <c>EXTNAME</c> keyword value, or <see langword="null"/> when absent (e.g. the primary HDU).</summary>
    public string? ExtensionName { get; }

    /// <summary>The <c>BITPIX</c> pixel representation, or <see langword="null"/> for HDUs with no pixel data.</summary>
    public BitPixType? DataType { get; }

    public int NumberOfAxes { get; }

    public ImmutableList<int> AxisDimensions { get; }

    /// <summary>This HDU's own header cards — not the primary HDU's.</summary>
    public ImmutableList<FitsKeywordDto> Header { get; }

    public static FitsHduSummaryDto Create(int index, HduType type, string? extensionName, BitPixType? dataType, ImmutableList<int> axisDimensions, ImmutableList<FitsKeywordDto> header)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        return new FitsHduSummaryDto(index, type, extensionName, dataType, axisDimensions.Count, axisDimensions, header);
    }
}
