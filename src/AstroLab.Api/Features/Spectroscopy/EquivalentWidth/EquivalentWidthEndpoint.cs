using AstroLab.Core.Spectroscopy;
using AstroLab.Infrastructure.Storage;

namespace AstroLab.Api.Features.Spectroscopy.EquivalentWidth;

/// <summary>Calculates the equivalent width of a spectral feature over a supplied wavelength interval.</summary>
public static class EquivalentWidthEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapEquivalentWidthEndpoint()
        {
            group.MapPost("/{fileId}/equivalent-width", CalculateEquivalentWidthAsync)
                .WithSummary("Calculates the equivalent width (absorption or emission, per the documented sign convention) over a wavelength interval.");
        }
    }

    private static async Task<IResult> CalculateEquivalentWidthAsync(
        string fileId, EquivalentWidthRequest request, FitsDatasetReader datasetReader, CancellationToken cancellationToken)
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
            dataset.Hdu.Header, spectrum.Length, "spectroscopy.equivalent_width.no_wavelength_solution");

        if (wavelengthsResult.IsFailure)
        {
            return wavelengthsResult.Error.ToProblem();
        }

        var wavelengths = wavelengthsResult.Value;

        var windowWavelengths = new List<double>();

        var windowFlux = new List<double>();

        for (var i = 0; i < wavelengths.Length; i++)
        {
            if (wavelengths[i] >= request.MinWavelength && wavelengths[i] <= request.MaxWavelength)
            {
                windowWavelengths.Add(wavelengths[i]);

                windowFlux.Add(spectrum[i]);
            }
        }

        var equivalentWidthResult = EquivalentWidthCalculator.Calculate([.. windowWavelengths], [.. windowFlux]);

        return equivalentWidthResult.ToApiResult(equivalentWidth => Results.Ok(EquivalentWidthResponse.Create(
            fileId, equivalentWidth, request.MinWavelength, request.MaxWavelength, isWavelengthCalibrated: true)));
    }
}
