using AstroLab.Core.Photometry;
using AstroLab.Infrastructure.Storage;

namespace AstroLab.Api.Features.Photometry;

/// <summary>Aperture photometry endpoints: background-subtracted flux measurement at a pixel position.</summary>
public static class PhotometryEndpoints
{
    extension(IEndpointRouteBuilder app)
    {
        public RouteGroupBuilder MapPhotometryEndpoints()
        {
            var group = app.MapGroup("/api/photometry").WithTags("Photometry");

            group.MapPost("/{fileId}/aperture", MeasureApertureAsync)
                .WithSummary("Measures background-subtracted aperture flux at a pixel position in the primary image HDU.");

            return group;
        }
    }

    private static async Task<IResult> MeasureApertureAsync(
        string fileId,
        AperturePhotometryRequest request,
        FitsDatasetReader datasetReader,
        CancellationToken cancellationToken)
    {
        var datasetResult = await datasetReader.LoadPrimaryImageAsync(fileId, cancellationToken);
        if (datasetResult.IsFailure)
        {
            return datasetResult.Error.ToProblem();
        }

        var dataset = datasetResult.Value;
        var (width, height) = dataset.Image.Resolve2DDimensions();

        var measurementResult = ApertureEngine.MeasureNetFlux(
            dataset.Pixels,
            width,
            height,
            request.CenterX,
            request.CenterY,
            request.ApertureRadius,
            request.AnnulusInnerRadius,
            request.AnnulusOuterRadius,
            request.BackgroundMethod);

        return measurementResult.ToApiResult(measurement => Results.Ok(new AperturePhotometryResponse(
            fileId,
            measurement.RawFlux,
            measurement.ApertureArea,
            measurement.BackgroundPerPixel,
            measurement.NetFlux)));
    }
}
