using System.Collections.Immutable;
using AstroLab.Core.Fits;

namespace AstroLab.Api.Features.Fits.Inspect;

public sealed record FitsHeaderResponse(
    string FileId,
    FitsDatasetKind DatasetKind,
    IReadOnlyList<FitsHduSummaryDto> Hdus,
    IReadOnlyList<FitsKeywordDto> Keywords)
{
    public static FitsHeaderResponse FromInspection(string fileId, ImmutableArray<HduDescriptor> hdus)
    {
        var datasetKind = FitsDatasetClassifier.Classify(hdus);

        var hduSummaries = new List<FitsHduSummaryDto>(hdus.Length);
        foreach (var hdu in hdus)
        {
            var axes = hdu.Image?.NAxes ?? ImmutableArray<int>.Empty;
            hduSummaries.Add(new FitsHduSummaryDto(hdu.Index, hdu.Type, axes));
        }

        var primaryHeader = hdus[0].Header;
        var keywords = new List<FitsKeywordDto>(primaryHeader.Count);
        keywords.AddRange(primaryHeader.Select(keyword =>
            new FitsKeywordDto(keyword.Name, keyword.Value.ToString(), keyword.Comment)));

        return new FitsHeaderResponse(fileId, datasetKind, hduSummaries, keywords);
    }
}
