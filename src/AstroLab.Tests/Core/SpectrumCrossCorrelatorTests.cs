using AstroLab.Core.Spectroscopy;

namespace AstroLab.Tests.Core;

public class SpectrumCrossCorrelatorTests
{
    [Fact]
    public void CorrelatePixelLag_ShiftedGaussianBump_RecoversKnownLag()
    {
        var fluxA = new double[60];

        var fluxB = new double[60];

        for (var i = 0; i < 60; i++)
        {
            fluxA[i] = Math.Exp(-Math.Pow(i - 20, 2) / (2.0 * 3.0 * 3.0));

            fluxB[i] = Math.Exp(-Math.Pow(i - 23, 2) / (2.0 * 3.0 * 3.0));
        }

        var result = SpectrumCrossCorrelator.CorrelatePixelLag(fluxA, fluxB);

        Assert.True(result.IsSuccess);

        Assert.True(Math.Abs(result.Value.LagBins - 3.0) < 0.5, $"Expected lag near 3, got {result.Value.LagBins}.");

        Assert.True(result.Value.PeakCorrelation > 0.9);
    }

    [Fact]
    public void CorrelatePixelLag_RejectsLengthMismatch()
    {
        double[] fluxA = [1.0, 2.0, 3.0, 4.0, 5.0];

        double[] fluxB = [1.0, 2.0, 3.0];

        var result = SpectrumCrossCorrelator.CorrelatePixelLag(fluxA, fluxB);

        Assert.True(result.IsFailure);

        Assert.Equal("spectroscopy.compare.length_mismatch", result.Error.Code);
    }

    [Fact]
    public void CorrelateAgainstTemplate_RedshiftedAbsorptionProfile_RecoversKnownRedshift()
    {
        const double trueRedshift = 0.01;

        var templateWavelengths = new double[201];

        var templateFlux = new double[201];

        for (var i = 0; i < templateWavelengths.Length; i++)
        {
            templateWavelengths[i] = 4900.0 + i;

            templateFlux[i] = RestFrameProfile(templateWavelengths[i]);
        }

        var observedWavelengths = new double[201];

        var observedFlux = new double[201];

        for (var i = 0; i < observedWavelengths.Length; i++)
        {
            observedWavelengths[i] = 4900.0 + i;

            observedFlux[i] = RestFrameProfile(observedWavelengths[i] / (1.0 + trueRedshift));
        }

        var result = SpectrumCrossCorrelator.CorrelateAgainstTemplate(
            observedWavelengths, observedFlux, templateWavelengths, templateFlux, minRedshift: 0.0, maxRedshift: 0.05, gridSize: 500);

        Assert.True(result.IsSuccess);

        Assert.True(
            Math.Abs(result.Value.Redshift - trueRedshift) < 0.001, $"Expected redshift near {trueRedshift}, got {result.Value.Redshift}.");

        Assert.True(result.Value.PeakCorrelation > 0.9);
    }

    [Fact]
    public void CorrelateAgainstTemplate_RejectsNonIncreasingWavelengths()
    {
        double[] observedWavelengths = [5000.0, 4999.0, 5001.0, 5002.0, 5003.0];

        double[] observedFlux = [1.0, 1.0, 1.0, 1.0, 1.0];

        double[] templateWavelengths = [5000.0, 5001.0, 5002.0, 5003.0, 5004.0];

        double[] templateFlux = [1.0, 1.0, 1.0, 1.0, 1.0];

        var result = SpectrumCrossCorrelator.CorrelateAgainstTemplate(
            observedWavelengths, observedFlux, templateWavelengths, templateFlux, minRedshift: 0.0, maxRedshift: 0.01);

        Assert.True(result.IsFailure);

        Assert.Equal("spectroscopy.redshift.wavelengths_not_increasing", result.Error.Code);
    }

    private static double RestFrameProfile(double wavelength) =>
        1.0 - (0.5 * Math.Exp(-Math.Pow(wavelength - 5000.0, 2) / 50.0));
}
