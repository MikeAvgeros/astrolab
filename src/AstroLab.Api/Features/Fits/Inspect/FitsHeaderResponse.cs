using System.Collections.Immutable;
using AstroLab.Core.Fits;

namespace AstroLab.Api.Features.Fits.Inspect;

public sealed record FitsHeaderResponse(string FileId, FitsDatasetKind DatasetKind, ImmutableList<FitsHduSummaryDto> Hdus, ImmutableList<FitsKeywordDto> Keywords);

/// <summary>Static factory accompanying <see cref="FitsHeaderResponse"/>. Validates arguments before constructing.</summary>
public static class FitsHeaderResponseFactory
{
    public static FitsHeaderResponse Create(string fileId, FitsDatasetKind datasetKind, ImmutableList<FitsHduSummaryDto> hdus, ImmutableList<FitsKeywordDto> keywords)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        return new FitsHeaderResponse(fileId, datasetKind, hdus, keywords);
    }

    public static FitsHeaderResponse Create(string fileId, ImmutableArray<HduDescriptor> hdus)
    {
        var datasetKind = FitsDatasetClassifier.Classify(hdus);

        var hduSummaries = hdus
            .Select(hdu => FitsHduSummaryDtoFactory.Create(hdu.Index, hdu.Type, hdu.Image?.NAxes ?? ImmutableArray<int>.Empty))
            .ToImmutableList();

        var primaryHeader = hdus[0].Header;

        var keywords = primaryHeader
            .Select(keyword => FitsKeywordDtoFactory.Create(keyword.Name, keyword.Value.ToString(), keyword.Comment))
            .ToImmutableList();

        return Create(fileId, datasetKind, hduSummaries, keywords);
    }
}
