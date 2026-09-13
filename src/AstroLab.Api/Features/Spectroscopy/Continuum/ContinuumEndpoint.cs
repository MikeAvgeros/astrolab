using System.Collections.Immutable;
using AstroLab.Core.Spectroscopy;
using AstroLab.Infrastructure.Storage;

namespace AstroLab.Api.Features.Spectroscopy.Continuum;

/// <summary>Fits a continuum model (polynomial, with optional exclusion ranges and sigma-clipping) to a one-dimensional spectrum.</summary>
public static class ContinuumEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapContinuumEndpoint()
        {
            group.MapPost("/{fileId}/continuum", FitContinuumAsync)
                .WithSummary("Fits a polynomial continuum model to a one-dimensional spectrum without mutating the original spectrum.");
        }
    }

    private static async Task<IResult> FitContinuumAsync(string fileId, ContinuumRequest request, FitsDatasetReader datasetReader, CancellationToken cancellationToken)
    {
        request.Validate();

        var datasetResult = await datasetReader.LoadSpectrumImageAsync(fileId, cancellationToken);

        if (datasetResult.IsFailure)
        {
            return datasetResult.Error.ToProblem();
        }

        using var dataset = datasetResult.Value;

        var spectrumResult = SpectrumFrameExtraction.ExtractFullFrame(dataset);

        if (spectrumResult.IsFailure)
        {
            return spectrumResult.Error.ToProblem();
        }

        var spectrum = spectrumResult.Value;

        var wavelengthsResult = SpectrumFrameExtraction.ResolveWavelengths(
            dataset.Hdu.Header, spectrum.Length, "spectroscopy.continuum_fit.no_wavelength_solution");

        if (wavelengthsResult.IsFailure)
        {
            return wavelengthsResult.Error.ToProblem();
        }

        var wavelengths = wavelengthsResult.Value;

        var excludedRanges = request.ExcludedRanges is { Length: > 0 } ranges
            ? ranges.Select(r => (r.MinWavelength, r.MaxWavelength)).ToArray()
            : [];

        var fitResult = ContinuumFitter.Fit(
            wavelengths, spectrum, request.PolynomialDegree, excludedRanges, request.SigmaClipThreshold, request.SigmaClipIterations);

        return fitResult.ToApiResult(fit => Results.Ok(ContinuumResponse.Create(
            fileId, [.. wavelengths], [.. spectrum], [.. fit.Continuum], [.. fit.Coefficients])));
    }
}
