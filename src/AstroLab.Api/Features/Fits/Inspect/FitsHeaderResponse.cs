using System.Collections.Immutable;
using AstroLab.Core.Fits;

namespace AstroLab.Api.Features.Fits.Inspect;

public sealed record FitsHeaderResponse
{
    private FitsHeaderResponse(string fileId, FitsDatasetKind datasetKind, ImmutableList<FitsHduSummaryDto> hdus, ImmutableList<FitsKeywordDto> keywords)
    {
        FileId = fileId;
        DatasetKind = datasetKind;
        Hdus = hdus;
        Keywords = keywords;
    }

    public string FileId { get; }

    public FitsDatasetKind DatasetKind { get; }

    public ImmutableList<FitsHduSummaryDto> Hdus { get; }

    public ImmutableList<FitsKeywordDto> Keywords { get; }

    public static FitsHeaderResponse Create(string fileId, FitsDatasetKind datasetKind, ImmutableList<FitsHduSummaryDto> hdus, ImmutableList<FitsKeywordDto> keywords)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        return new FitsHeaderResponse(fileId, datasetKind, hdus, keywords);
    }

    public static FitsHeaderResponse Create(string fileId, ImmutableArray<HduDescriptor> hdus)
    {
        var datasetKind = FitsDatasetClassifier.Classify(hdus);

        var hduSummaries = hdus
            .Select(hdu => FitsHduSummaryDto.Create(hdu.Index, hdu.Type, hdu.Image?.NAxes ?? ImmutableArray<int>.Empty))
            .ToImmutableList();

        var primaryHeader = hdus[0].Header;

        var keywords = primaryHeader
            .Select(keyword => FitsKeywordDto.Create(keyword.Name, keyword.Value.ToString(), keyword.Comment))
            .ToImmutableList();

        return Create(fileId, datasetKind, hduSummaries, keywords);
    }
}
