using AstroLab.Core.Sources;

namespace AstroLab.Api.Features.Images.SourceCharacterization;

/// <summary>
/// Roadmap slice: measuring the size, shape, and ellipticity of every source detected in a
/// staged image. Request/response contract is final; the algorithm itself is not yet implemented
/// (see spec.md §6.5), so this route always returns HTTP 501.
/// </summary>
public static class SourceCharacterizationEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapSourceCharacterizationEndpoint()
        {
            group.MapGet("/{fileId}/sources/characterization", CharacterizeSources)
                .WithSummary("Measures the size, shape, and ellipticity of every detected source in a staged image. Not yet implemented.");
        }
    }

    private static IResult CharacterizeSources(
        string fileId,
        double thresholdSigma = SourceDetector.DefaultThresholdSigma,
        int minimumArea = SourceDetector.DefaultMinimumArea,
        int maxSources = SourceDetector.DefaultMaxSources)
    {
        _ = SourceCharacterizationRequest.Create(thresholdSigma, minimumArea, maxSources);

        return NotImplementedResult.Value("images.sourcecharacterization.not_implemented", "Source shape characterization is not yet implemented.");
    }
}
