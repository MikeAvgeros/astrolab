using AstroLab.Core.Spectroscopy;

namespace AstroLab.Tests.Core;

public class SpectrumSignalToNoiseEstimatorTests
{
    [Fact]
    public void Estimate_AlternatingScatter_MatchesDerSnrDefinition()
    {
        // |2f[i] - f[i-2] - f[i+2]| is 0 everywhere except where the +/-1 pattern breaks symmetry; build a
        // pattern whose second differences are all exactly 4 so the expected noise is 1.482602/sqrt(6) * 4.
        double[] spectrum = [11.0, 11.0, 9.0, 9.0, 11.0, 11.0, 9.0, 9.0, 11.0, 11.0];

        var result = SpectrumSignalToNoiseEstimator.Estimate(spectrum);

        Assert.True(result.IsSuccess);

        var (noiseSigma, overallSnr, perSampleSnr) = result.Value;

        var expectedSigma = 1.482602 / Math.Sqrt(6.0) * 4.0;

        Assert.Equal(expectedSigma, noiseSigma, precision: 9);

        // Six 11s and four 9s: the median flux is 11.
        Assert.Equal(11.0 / expectedSigma, overallSnr, precision: 9);

        Assert.Equal(11.0 / expectedSigma, perSampleSnr[0], precision: 9);
    }

    [Fact]
    public void Estimate_GaussianNoiseOnSlopedContinuum_RecoversNoiseSigmaIndependentOfSlope()
    {
        const double trueSigma = 2.0;

        var random = new Random(12345);

        var noise = Enumerable.Range(0, 4000).Select(_ => trueSigma * NextGaussian(random)).ToArray();

        var flat = noise.Select(value => 100.0 + value).ToArray();

        var sloped = noise.Select((value, i) => 100.0 + 0.05 * i + value).ToArray();

        var flatResult = SpectrumSignalToNoiseEstimator.Estimate(flat);

        var slopedResult = SpectrumSignalToNoiseEstimator.Estimate(sloped);

        Assert.True(flatResult.IsSuccess);

        Assert.True(slopedResult.IsSuccess);

        Assert.InRange(flatResult.Value.NoiseSigma, 0.95 * trueSigma, 1.05 * trueSigma);

        Assert.Equal(flatResult.Value.NoiseSigma, slopedResult.Value.NoiseSigma, precision: 9);
    }

    [Fact]
    public void Estimate_NoiselessRamp_ReturnsIndeterminateNoiseError()
    {
        double[] spectrum = [1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0, 8.0, 9.0];

        var result = SpectrumSignalToNoiseEstimator.Estimate(spectrum);

        Assert.True(result.IsFailure);

        Assert.Equal("spectroscopy.snr.indeterminate_noise", result.Error.Code);
    }

    [Fact]
    public void Estimate_ConstantSpectrum_ReturnsIndeterminateNoiseError()
    {
        double[] spectrum = [5.0, 5.0, 5.0, 5.0, 5.0];

        var result = SpectrumSignalToNoiseEstimator.Estimate(spectrum);

        Assert.True(result.IsFailure);

        Assert.Equal("spectroscopy.snr.indeterminate_noise", result.Error.Code);
    }

    [Fact]
    public void Estimate_FewerBinsThanDerSnrStencil_ReturnsValidationError()
    {
        double[] spectrum = [60.0, 80.0, 100.0, 120.0];

        var result = SpectrumSignalToNoiseEstimator.Estimate(spectrum);

        Assert.True(result.IsFailure);

        Assert.Equal("spectroscopy.snr.insufficient_bins", result.Error.Code);
    }

    [Fact]
    public void Estimate_RejectsEmptySpectrum()
    {
        var result = SpectrumSignalToNoiseEstimator.Estimate([]);

        Assert.True(result.IsFailure);

        Assert.Equal("spectroscopy.snr.empty_spectrum", result.Error.Code);
    }

    private static double NextGaussian(Random random) =>
        Math.Sqrt(-2.0 * Math.Log(1.0 - random.NextDouble())) * Math.Cos(2.0 * Math.PI * random.NextDouble());
}
