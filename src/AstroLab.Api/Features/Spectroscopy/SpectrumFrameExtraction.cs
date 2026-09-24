using AstroLab.Core.Fits;
using AstroLab.Core.Result;
using AstroLab.Core.Spectroscopy;
using AstroLab.Infrastructure.Storage;

namespace AstroLab.Api.Features.Spectroscopy;

/// <summary>
/// Extracts the full-spatial-extent, boxcar-summed 1D spectrum from a staged spectroscopic frame (no
/// trace/aperture is requested), shared by the endpoints that operate on a whole collapsed spectrum
/// rather than a caller-supplied trace (<c>Lines</c>, <c>Calibrate</c>, <c>Compare</c>).
/// </summary>
internal static class SpectrumFrameExtraction
{
    public static Result<double[]> ExtractFullFrame(FitsDataset dataset)
    {
        var (width, height) = dataset.Image.Resolve2DDimensions();

        var axis = SpectrumExtractor.ResolveDispersionAxis(dataset.Hdu.Header);

        var dispersionBins = axis == DispersionAxis.Horizontal ? width : height;

        var spatialExtent = axis == DispersionAxis.Horizontal ? height : width;

        var traceCenters = new double[dispersionBins];

        Array.Fill(traceCenters, spatialExtent / 2.0);

        var spectrum = new double[dispersionBins];

        var extractResult = SpectrumExtractor.ExtractBoxcar(
            dataset.Pixels, width, height, axis, traceCenters, spatialExtent / 2.0, spectrum);

        return extractResult.IsFailure ? Result<double[]>.Failure(extractResult.Error) : spectrum;
    }
    
    public static Result<double[]> ResolveWavelengths(FitsHeader header, int dispersionBins, string errorCode)
    {
        var solutionResult = SpectrumExtractor.ResolveLinearDispersionSolution(header);

        if (solutionResult.IsFailure)
        {
            return Error.Validation(errorCode, solutionResult.Error.Message);
        }

        var (referenceWavelength, wavelengthPerPixel, referencePixel) = solutionResult.Value;

        var wavelengths = new double[dispersionBins];

        for (var i = 0; i < dispersionBins; i++)
        {
            wavelengths[i] = referenceWavelength + (i - (referencePixel - 1.0)) * wavelengthPerPixel;
        }

        return wavelengths;
    }
}
