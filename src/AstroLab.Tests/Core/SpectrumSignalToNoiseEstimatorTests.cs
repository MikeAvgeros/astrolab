using AstroLab.Core.Spectroscopy;

namespace AstroLab.Tests.Core;

public class SpectrumSignalToNoiseEstimatorTests
{
    [Fact]
    public void Estimate_VariedSpectrum_ComputesMedianIqrBasedSnr()
    {
        double[] spectrum = [1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0, 8.0, 9.0];

        var result = SpectrumSignalToNoiseEstimator.Estimate(spectrum);

        Assert.True(result.IsSuccess);

        var (noiseSigma, overallSnr, perSampleSnr) = result.Value;

        var expectedSigma = 4.0 / 1.349;

        Assert.Equal(expectedSigma, noiseSigma, precision: 6);

        Assert.Equal(5.0 / expectedSigma, overallSnr, precision: 6);

        Assert.Equal(1.0 / expectedSigma, perSampleSnr[0], precision: 6);

        Assert.Equal(9.0 / expectedSigma, perSampleSnr[^1], precision: 6);
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
    public void Estimate_RejectsEmptySpectrum()
    {
        var result = SpectrumSignalToNoiseEstimator.Estimate([]);

        Assert.True(result.IsFailure);

        Assert.Equal("spectroscopy.snr.empty_spectrum", result.Error.Code);
    }
}
