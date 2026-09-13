using AstroLab.Api.Features.Spectroscopy.Continuum;
using AstroLab.Core.Spectroscopy;
using AstroLab.Infrastructure.Storage;

namespace AstroLab.Api.Features.Spectroscopy.ContinuumSubtract;

/// <summary>Subtracts a fitted continuum model from a one-dimensional spectrum.</summary>
public static class ContinuumSubtractEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapContinuumSubtractEndpoint()
        {
            group.MapPost("/{fileId}/continuum/subtract", SubtractContinuumAsync)
                .WithSummary("Subtracts the fitted continuum from a spectrum, preserving wavelength and uncertainty.");
        }
    }

    private static async Task<IResult> SubtractContinuumAsync(
        string fileId, ContinuumSubtractRequest request, FitsDatasetReader datasetReader, CancellationToken cancellationToken)
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
            dataset.Hdu.Header, spectrum.Length, "spectroscopy.continuum_subtract.no_wavelength_solution");

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

        if (fitResult.IsFailure)
        {
            return fitResult.Error.ToProblem();
        }

        var continuum = fitResult.Value.Continuum;

        var subtracted = (double[])spectrum.Clone();

        var subtractResult = SpectrumExtractor.SubtractBackground(subtracted, continuum);

        return subtractResult.ToApiResult(_ => Results.Ok(
            ContinuumSubtractResponse.Create(fileId, [.. wavelengths], [.. spectrum], [.. subtracted])));
    }
}
