using AstroLab.Core.Spectroscopy;

namespace AstroLab.Tests.Core;

public class SpectralLineDetectorTests
{
    [Fact]
    public void Detect_SingleEmissionSpikeAboveFlatContinuum_ReportsExactPositionFluxAndFwhm()
    {
        ReadOnlySpan<double> spectrum = [30.0, 30.0, 30.0, 30.0, 300.0, 30.0, 30.0, 30.0, 30.0];

        var result = SpectralLineDetector.Detect(spectrum, significanceSigma: 3.0);

        Assert.True(result.IsSuccess);

        var line = Assert.Single(result.Value);

        Assert.Equal(4.0, line.Position, precision: 9);

        Assert.Equal(270.0, line.Flux, precision: 9);

        Assert.Equal(1.0, line.Fwhm, precision: 9);
    }

    [Fact]
    public void Detect_FlatContinuumWithNoDeviation_FindsNoLines()
    {
        ReadOnlySpan<double> spectrum = [10.0, 10.0, 10.0, 10.0, 10.0];

        var result = SpectralLineDetector.Detect(spectrum);

        Assert.True(result.IsSuccess);

        Assert.Empty(result.Value);
    }

    [Fact]
    public void Detect_WeakBumpBelowThreshold_IsNotReportedAsALine()
    {
        ReadOnlySpan<double> spectrum = [60.0, 80.0, 100.0, 120.0];

        var result = SpectralLineDetector.Detect(spectrum, significanceSigma: 5.0);

        Assert.True(result.IsSuccess);

        Assert.Empty(result.Value);
    }

    [Fact]
    public void Detect_NoisySlopedContinuum_FindsExactlyTheInjectedLines()
    {
        // A continuum rising from 100 to 500 across 2000 bins with sigma = 1 noise: a global median and
        // spread would treat the slope as signal (sigma ~ 150), hiding the 20-sigma lines entirely.
        var random = new Random(2024);

        var spectrum = new double[2000];

        for (var i = 0; i < spectrum.Length; i++)
        {
            spectrum[i] = 100.0 + 0.2 * i + NextGaussian(random);
        }

        AddGaussianLine(spectrum, center: 400, amplitude: 20.0, sigma: 2.0);

        AddGaussianLine(spectrum, center: 1500, amplitude: -20.0, sigma: 2.0);

        var result = SpectralLineDetector.Detect(spectrum);

        Assert.True(result.IsSuccess);

        Assert.Equal(2, result.Value.Length);

        Assert.Contains(result.Value, line => Math.Abs(line.Position - 400) <= 1 && line.Flux > 15.0);

        Assert.Contains(result.Value, line => Math.Abs(line.Position - 1500) <= 1 && line.Flux < -15.0);

        Assert.All(result.Value, line => Assert.InRange(line.Fwhm, 3.0, 6.5));
    }

    [Fact]
    public void Detect_NoiselessLinearContinuum_FindsNoLinesAtEitherEnd()
    {
        var spectrum = Enumerable.Range(0, 500).Select(i => 1000.0 + 3.0 * i).ToArray();

        var result = SpectralLineDetector.Detect(spectrum, significanceSigma: 3.0);

        Assert.True(result.IsSuccess);

        Assert.Empty(result.Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void Detect_RejectsContinuumWindowBelowMinimum(int continuumWindowBins)
    {
        ReadOnlySpan<double> spectrum = [10.0, 20.0, 10.0];

        var result = SpectralLineDetector.Detect(spectrum, continuumWindowBins: continuumWindowBins);

        Assert.True(result.IsFailure);

        Assert.Equal("spectroscopy.lines.invalid_continuum_window", result.Error.Code);
    }

    private static void AddGaussianLine(double[] spectrum, int center, double amplitude, double sigma)
    {
        for (var i = 0; i < spectrum.Length; i++)
        {
            var offset = i - center;

            spectrum[i] += amplitude * Math.Exp(-(offset * offset) / (2.0 * sigma * sigma));
        }
    }

    private static double NextGaussian(Random random) =>
        Math.Sqrt(-2.0 * Math.Log(1.0 - random.NextDouble())) * Math.Cos(2.0 * Math.PI * random.NextDouble());

    [Fact]
    public void Detect_RejectsEmptySpectrum()
    {
        var result = SpectralLineDetector.Detect([]);

        Assert.True(result.IsFailure);

        Assert.Equal("spectroscopy.lines.empty_spectrum", result.Error.Code);
    }

    [Fact]
    public void Detect_RejectsNonPositiveSignificanceSigma()
    {
        ReadOnlySpan<double> spectrum = [10.0, 20.0, 10.0];

        var result = SpectralLineDetector.Detect(spectrum, significanceSigma: 0.0);

        Assert.True(result.IsFailure);

        Assert.Equal("spectroscopy.lines.invalid_threshold", result.Error.Code);
    }
}
