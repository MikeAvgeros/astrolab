using AstroLab.Infrastructure.Storage;

namespace AstroLab.Api.Features.Images.ApertureCorrection;

/// <summary>Applies a multiplicative aperture correction to a measured flux, propagating uncertainty where supplied.</summary>
public static class ApertureCorrectionEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapApertureCorrectionEndpoint()
        {
            group.MapPost("/{fileId}/photometry/aperture-correction", ApplyApertureCorrectionAsync)
                .WithSummary("Applies an aperture correction to a measured flux and propagates uncertainty where available.");
        }
    }

    private static async Task<IResult> ApplyApertureCorrectionAsync(
        string fileId, ApertureCorrectionRequest request, FitsDatasetReader datasetReader, CancellationToken cancellationToken)
    {
        request.Validate();

        var hduResult = await datasetReader.LoadImageMetadataAsync(fileId, cancellationToken);

        if (hduResult.IsFailure)
        {
            return hduResult.Error.ToProblem();
        }

        var (correctedFlux, correctedFluxUncertainty) = Core.Photometry.ApertureCorrection.Apply(
            request.MeasuredFlux, request.CorrectionFactor, request.MeasuredFluxUncertainty);

        return Results.Ok(ApertureCorrectionResponse.Create(fileId, correctedFlux, correctedFluxUncertainty));
    }
}
