using AstroLab.Core.Imaging;
using AstroLab.Infrastructure.Storage;

namespace AstroLab.Api.Features.Fits.Quality;

/// <summary>Reports cross-cutting data-quality statistics (invalid pixel counts, saturation, dynamic range, usable-pixel fraction) for a staged FITS dataset's image data.</summary>
public static class QualityEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapQualityEndpoint()
        {
            group.MapGet("/{fileId}/quality", GetQualityAsync)
                .WithSummary("Reports data-quality statistics for a staged FITS dataset's image data, distinguishing not-present, not-measurable, and measured-as-zero.");
        }
    }

    private static async Task<IResult> GetQualityAsync(string fileId, FitsDatasetReader datasetReader, CancellationToken cancellationToken)
    {
        var datasetResult = await datasetReader.LoadImageAsync(fileId, cancellationToken);

        if (datasetResult.IsFailure)
        {
            return datasetResult.Error.ToProblem();
        }

        using var dataset = datasetResult.Value;

        var reportResult = ImageQualityAnalyzer.Analyze(dataset.Pixels, dataset.Hdu.Header, dataset.Image);

        return reportResult.ToApiResult(report => Results.Ok(FitsQualityResponse.Create(fileId, report)));
    }
}
