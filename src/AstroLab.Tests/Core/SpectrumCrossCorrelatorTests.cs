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

    [Fact]
    public void CorrelateAgainstTemplate_DifferentContinuumSlopes_RecoversRedshiftFromLineFeatures()
    {
        // Rest-frame template: rising continuum with three absorption lines. The observed spectrum is the
        // same lines at z = 0.3 on a falling continuum, so only the line pattern (not the continuum shape)
        // can identify the redshift; a raw Pearson search is dominated by the slopes.
        const double trueRedshift = 0.3;

        double[] restLineCenters = [4340.0, 4861.0, 5890.0];

        var templateWavelengths = Enumerable.Range(0, 2001).Select(i => 4000.0 + i).ToArray();

        var templateFlux = templateWavelengths.Select(w => 1.0 + 0.0002 * (w - 4000.0) - Absorption(w, restLineCenters, 1.0)).ToArray();

        var observedWavelengths = Enumerable.Range(0, 1501).Select(i => 5000.0 + 2.0 * i).ToArray();

        var observedLineCenters = restLineCenters.Select(center => center * (1.0 + trueRedshift)).ToArray();

        var random = new Random(99);

        var observedFlux = observedWavelengths
            .Select(w => 3.0 - 0.0003 * (w - 5000.0) - Absorption(w, observedLineCenters, 1.3) + 0.01 * (random.NextDouble() - 0.5))
            .ToArray();

        var result = SpectrumCrossCorrelator.CorrelateAgainstTemplate(
            observedWavelengths, observedFlux, templateWavelengths, templateFlux, minRedshift: 0.0, maxRedshift: 1.0, gridSize: 1001);

        Assert.True(result.IsSuccess);

        Assert.Equal(trueRedshift, result.Value.Redshift, precision: 3);
    }

    private static double Absorption(double wavelength, double[] centers, double sigma)
    {
        var depth = 0.0;

        foreach (var center in centers)
        {
            var offset = (wavelength - center) / (sigma * 4.0);

            depth += 0.4 * Math.Exp(-0.5 * offset * offset);
        }

        return depth;
    }
}
