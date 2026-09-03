using System.Collections.Immutable;
using AstroLab.Core.Fits;

namespace AstroLab.Api.Features.Fits.Inspect;

public sealed record FitsHeaderResponse
{
    private FitsHeaderResponse(string fileId, FitsDatasetKind datasetKind, ImmutableList<FitsHduSummaryDto> hdus, ImmutableList<FitsKeywordDto> keywords, FitsCommonMetadataDto commonMetadata)
    {
        FileId = fileId;
        DatasetKind = datasetKind;
        Hdus = hdus;
        Keywords = keywords;
        CommonMetadata = commonMetadata;
    }

    public string FileId { get; }

    public FitsDatasetKind DatasetKind { get; }

    public ImmutableList<FitsHduSummaryDto> Hdus { get; }

    /// <summary>The primary HDU's header cards. Kept for backward compatibility — see each entry in <see cref="Hdus"/> for its own header.</summary>
    public ImmutableList<FitsKeywordDto> Keywords { get; }

    /// <summary>Commonly useful astronomical keywords extracted from the primary HDU's header, where present.</summary>
    public FitsCommonMetadataDto CommonMetadata { get; }

    public static FitsHeaderResponse Create(string fileId, FitsDatasetKind datasetKind, ImmutableList<FitsHduSummaryDto> hdus, ImmutableList<FitsKeywordDto> keywords, FitsCommonMetadataDto commonMetadata)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        return new FitsHeaderResponse(fileId, datasetKind, hdus, keywords, commonMetadata);
    }

    public static FitsHeaderResponse Create(string fileId, ImmutableArray<HduDescriptor> hdus)
    {
        var datasetKind = FitsDatasetClassifier.Classify(hdus);

        var hduSummaries = hdus
            .Select(hdu => FitsHduSummaryDto.Create(
                hdu.Index,
                hdu.Type,
                ExtensionName(hdu.Header),
                hdu.Image?.BitPix,
                (hdu.Image?.NAxes ?? ImmutableArray<int>.Empty).ToImmutableList(),
                ToKeywordDtos(hdu.Header)))
            .ToImmutableList();

        var primaryHeader = hdus[0].Header;

        var keywords = ToKeywordDtos(primaryHeader);

        var commonMetadata = FitsCommonMetadataDto.FromHeader(primaryHeader);

        return Create(fileId, datasetKind, hduSummaries, keywords, commonMetadata);
    }

    private static string? ExtensionName(FitsHeader header)
    {
        var result = header.GetString("EXTNAME");

        return result.IsSuccess ? result.Value : null;
    }

    private static ImmutableList<FitsKeywordDto> ToKeywordDtos(FitsHeader header) =>
        header
            .Select(keyword => FitsKeywordDto.Create(keyword.Name, keyword.Value.ToString(), keyword.Comment))
            .ToImmutableList();
}
