using AstroLab.Core.Astrometry;
using AstroLab.Infrastructure.Storage;

namespace AstroLab.Api.Features.Images.Separation;

/// <summary>Computes the angular separation between two pixel positions in a staged image via its WCS solution.</summary>
public static class SeparationEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapSeparationEndpoint()
        {
            group.MapGet("/{fileId}/astrometry/separation", ComputeSeparationAsync)
                .WithSummary("Computes the angular separation between two pixel positions via the image's WCS.");
        }
    }

    private static async Task<IResult> ComputeSeparationAsync(
        string fileId,
        double firstPixelX,
        double firstPixelY,
        double secondPixelX,
        double secondPixelY,
        FitsDatasetReader datasetReader,
        CancellationToken cancellationToken)
    {
        var request = AngularSeparationRequest.Create(firstPixelX, firstPixelY, secondPixelX, secondPixelY);

        var datasetResult = await datasetReader.LoadImageAsync(fileId, cancellationToken);

        if (datasetResult.IsFailure)
        {
            return datasetResult.Error.ToProblem();
        }

        using var dataset = datasetResult.Value;

        var wcsResult = Wcs.FromHeader(dataset.Hdu.Header);

        if (wcsResult.IsFailure)
        {
            return wcsResult.Error.ToProblem();
        }

        var wcs = wcsResult.Value;

        var firstWorldResult = wcs.PixelToWorld(request.FirstPixelX, request.FirstPixelY);

        if (firstWorldResult.IsFailure)
        {
            return firstWorldResult.Error.ToProblem();
        }

        var secondWorldResult = wcs.PixelToWorld(request.SecondPixelX, request.SecondPixelY);

        if (secondWorldResult.IsFailure)
        {
            return secondWorldResult.Error.ToProblem();
        }

        var (firstRightAscension, firstDeclination) = firstWorldResult.Value;

        var (secondRightAscension, secondDeclination) = secondWorldResult.Value;

        var separationResult = AngularSeparation.ComputeArcseconds(firstRightAscension, firstDeclination, secondRightAscension, secondDeclination);

        return separationResult.ToApiResult(separationArcsec => Results.Ok(AngularSeparationResponse.Create(fileId, separationArcsec)));
    }
}
