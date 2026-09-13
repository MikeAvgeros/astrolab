using AstroLab.Core.Spectroscopy;
using AstroLab.Infrastructure.Storage;

namespace AstroLab.Api.Features.Spectroscopy.LineFit;

/// <summary>Fits a Gaussian profile to a spectral line over a supplied wavelength region.</summary>
public static class LineFitEndpoint
{
    extension(IEndpointRouteBuilder group)
    {
        public void MapLineFitEndpoint()
        {
            group.MapPost("/{fileId}/lines/fit", FitLineAsync)
                .WithSummary("Fits a Gaussian line profile (centre, amplitude, FWHM, integrated flux) over a supplied wavelength region.");
        }
    }

    private static async Task<IResult> FitLineAsync(string fileId, LineFitRequest request, FitsDatasetReader datasetReader, CancellationToken cancellationToken)
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
            dataset.Hdu.Header, spectrum.Length, "spectroscopy.line_fit.no_wavelength_solution");

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

        var fitResult = SpectralLineFitter.FitGaussian(
            [.. windowWavelengths], [.. windowFlux], request.InitialCentre, request.InitialAmplitude, request.InitialFwhm);

        return fitResult.ToApiResult(fit => Results.Ok(LineFitResponse.Create(
            fileId,
            fit.Baseline, fit.BaselineUncertainty,
            fit.Amplitude, fit.AmplitudeUncertainty,
            fit.Center, fit.CenterUncertainty,
            fit.Fwhm, fit.FwhmUncertainty,
            fit.IntegratedFlux, fit.ReducedChiSquare,
            isWavelengthCalibrated: true)));
    }
}
