using AstroLab.Core.Fits;
using AstroLab.Core.Result;
using AstroLab.Core.Spectroscopy;
using AstroLab.Infrastructure.Storage;

namespace AstroLab.Api.Features.Spectroscopy.Compare;

/// <summary>
/// Compares two staged spectra (extracted from their full spatial extent, as in <c>Lines</c>): their
/// flux ratio and RMS flux difference, and their cross-correlation peak and velocity shift (derived
/// from the primary file's CRVAL1/CDELT1 dispersion WCS, if present).
/// </summary>
public static class CompareEndpoint
{
    private const double SpeedOfLightKmPerSec = 299792.458;

    extension(IEndpointRouteBuilder group)
    {
        public void MapCompareEndpoint()
        {
            group.MapPost("/{fileId}/compare", CompareSpectraAsync)
                .WithSummary("Compares two staged spectra via cross-correlation, flux ratio, and flux difference.");
        }
    }

    private static async Task<IResult> CompareSpectraAsync(
        string fileId, SpectrumCompareRequest request, FitsDatasetReader datasetReader, CancellationToken cancellationToken)
    {
        request.Validate();

        var primaryResult = await datasetReader.LoadSpectrumImageAsync(fileId, cancellationToken);

        if (primaryResult.IsFailure)
        {
            return primaryResult.Error.ToProblem();
        }

        using var primaryDataset = primaryResult.Value;

        var primarySpectrumResult = SpectrumFrameExtraction.ExtractFullFrame(primaryDataset);

        if (primarySpectrumResult.IsFailure)
        {
            return primarySpectrumResult.Error.ToProblem();
        }

        var comparisonResult = await datasetReader.LoadSpectrumImageAsync(request.ComparisonFileId, cancellationToken);

        if (comparisonResult.IsFailure)
        {
            return comparisonResult.Error.ToProblem();
        }

        using var comparisonDataset = comparisonResult.Value;

        var comparisonSpectrumResult = SpectrumFrameExtraction.ExtractFullFrame(comparisonDataset);

        if (comparisonSpectrumResult.IsFailure)
        {
            return comparisonSpectrumResult.Error.ToProblem();
        }

        var primarySpectrum = primarySpectrumResult.Value;

        var comparisonSpectrum = comparisonSpectrumResult.Value;

        var compareResult = SpectrumComparer.Compare(primarySpectrum, comparisonSpectrum);

        if (compareResult.IsFailure)
        {
            return compareResult.Error.ToProblem();
        }

        var correlateResult = SpectrumCrossCorrelator.CorrelatePixelLag(primarySpectrum, comparisonSpectrum);

        if (correlateResult.IsFailure)
        {
            return correlateResult.Error.ToProblem();
        }

        var velocityResult = ComputeVelocityShift(primaryDataset.Hdu.Header, correlateResult.Value.LagBins);

        if (velocityResult.IsFailure)
        {
            return velocityResult.Error.ToProblem();
        }

        var compare = compareResult.Value;

        return Results.Ok(SpectrumCompareResponse.Create(
            fileId,
            request.ComparisonFileId,
            correlateResult.Value.PeakCorrelation,
            velocityResult.Value,
            compare.MeanFluxRatio,
            compare.RmsFluxDifference));
    }

    private static Result<double> ComputeVelocityShift(FitsHeader header, double lagBins)
    {
        var referenceWavelengthResult = header.GetReal("CRVAL1");

        if (referenceWavelengthResult.IsFailure)
        {
            return Error.Validation(
                "spectroscopy.compare.no_wavelength_solution",
                "The primary file's header carries no CRVAL1 dispersion reference wavelength; a velocity shift cannot be computed.");
        }

        var dispersionPerPixelResult = header.GetReal("CDELT1");

        if (dispersionPerPixelResult.IsFailure)
        {
            return Error.Validation(
                "spectroscopy.compare.no_wavelength_solution",
                "The primary file's header carries no CDELT1 dispersion scale; a velocity shift cannot be computed.");
        }

        var referenceWavelength = referenceWavelengthResult.Value;

        if (referenceWavelength <= 0.0)
        {
            return Error.Validation(
                "spectroscopy.compare.invalid_wavelength_solution", "CRVAL1 must be a positive reference wavelength.");
        }

        var wavelengthShift = lagBins * dispersionPerPixelResult.Value;

        return SpeedOfLightKmPerSec * wavelengthShift / referenceWavelength;
    }
}
