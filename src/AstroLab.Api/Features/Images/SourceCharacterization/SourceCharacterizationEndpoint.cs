using System.Collections.Immutable;
using AstroLab.Core.Sources;
using AstroLab.Infrastructure.Storage;

namespace AstroLab.Api.Features.Images.SourceCharacterization;

/// <summary>Measures the size, shape, and ellipticity of every source detected in a staged image via weighted second-moment analysis.</summary>
public static class SourceCharacterizationEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapSourceCharacterizationEndpoint()
        {
            group.MapGet("/{fileId}/sources/characterization", CharacterizeSourcesAsync)
                .WithSummary("Measures the size, shape, and ellipticity of every detected source in a staged image.");
        }
    }

    private static async Task<IResult> CharacterizeSourcesAsync(
        string fileId,
        FitsDatasetReader datasetReader,
        CancellationToken cancellationToken,
        double thresholdSigma = SourceDetector.DefaultThresholdSigma,
        int minimumArea = SourceDetector.DefaultMinimumArea,
        int maxSources = SourceDetector.DefaultMaxSources)
    {
        var request = SourceCharacterizationRequest.Create(thresholdSigma, minimumArea, maxSources);

        var datasetResult = await datasetReader.LoadImageAsync(fileId, cancellationToken);

        if (datasetResult.IsFailure)
        {
            return datasetResult.Error.ToProblem();
        }

        using var dataset = datasetResult.Value;

        var (width, height) = dataset.Image.Resolve2DDimensions();

        var shapesResult = SourceShapeAnalyzer.Analyze(
            dataset.Pixels, width, height, request.ThresholdSigma, request.MinimumArea, request.MaxSources);

        return shapesResult.ToApiResult(shapes => Results.Ok(SourceCharacterizationResponse.Create(
            fileId,
            shapes
                .Select(shape => SourceShapeDto.Create(
                    shape.Id, shape.SemiMajorAxisPixels, shape.SemiMinorAxisPixels, shape.Ellipticity, shape.PositionAngleDegrees))
                .ToImmutableList())));
    }
}
