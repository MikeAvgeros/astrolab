using AstroLab.Core.Spectroscopy;

namespace AstroLab.Tests.Core;

public class SpectralLineFitterTests
{
    private const double TrueBaseline = 100.0;
    private const double TrueAmplitude = 50.0;
    private const double TrueCenter = 5000.0;
    private const double TrueSigma = 2.0;

    private static (double[] X, double[] Flux) BuildSyntheticGaussian()
    {
        var x = new double[21];

        var flux = new double[21];

        for (var i = 0; i < 21; i++)
        {
            var wavelength = 4990.0 + i;

            x[i] = wavelength;

            var offset = wavelength - TrueCenter;

            flux[i] = TrueBaseline + (TrueAmplitude * Math.Exp(-(offset * offset) / (2.0 * TrueSigma * TrueSigma)));
        }

        return (x, flux);
    }

    [Fact]
    public void FitGaussian_NoiselessEmissionLine_RecoversTrueParametersWithoutInitialGuesses()
    {
        var (x, flux) = BuildSyntheticGaussian();

        var result = SpectralLineFitter.FitGaussian(x, flux, initialCenter: null, initialAmplitude: null, initialFwhm: null);

        Assert.True(result.IsSuccess);

        var fit = result.Value;

        Assert.Equal(TrueBaseline, fit.Baseline, precision: 3);

        Assert.Equal(TrueAmplitude, fit.Amplitude, precision: 3);

        Assert.Equal(TrueCenter, fit.Center, precision: 3);

        var expectedFwhm = TrueSigma * 2.0 * Math.Sqrt(2.0 * Math.Log(2.0));

        Assert.Equal(expectedFwhm, fit.Fwhm, precision: 3);

        var expectedIntegratedFlux = TrueAmplitude * TrueSigma * Math.Sqrt(2.0 * Math.PI);

        Assert.Equal(expectedIntegratedFlux, fit.IntegratedFlux, precision: 2);

        Assert.True(fit.ReducedChiSquare < 1e-6);
    }

    [Fact]
    public void FitGaussian_WithCloseInitialGuesses_Converges()
    {
        var (x, flux) = BuildSyntheticGaussian();

        var result = SpectralLineFitter.FitGaussian(x, flux, initialCenter: 4999.0, initialAmplitude: 40.0, initialFwhm: 5.0);

        Assert.True(result.IsSuccess);

        var fit = result.Value;

        Assert.Equal(TrueCenter, fit.Center, precision: 3);

        Assert.Equal(TrueAmplitude, fit.Amplitude, precision: 3);
    }

    [Fact]
    public void FitGaussian_RejectsTooFewPoints()
    {
        double[] x = [1.0, 2.0, 3.0, 4.0];

        double[] flux = [1.0, 2.0, 3.0, 4.0];

        var result = SpectralLineFitter.FitGaussian(x, flux, null, null, null);

        Assert.True(result.IsFailure);

        Assert.Equal("spectroscopy.line_fit.insufficient_points", result.Error.Code);
    }

    [Fact]
    public void FitGaussian_RejectsLengthMismatch()
    {
        double[] x = [1.0, 2.0, 3.0, 4.0, 5.0];

        double[] flux = [1.0, 2.0, 3.0];

        var result = SpectralLineFitter.FitGaussian(x, flux, null, null, null);

        Assert.True(result.IsFailure);

        Assert.Equal("spectroscopy.line_fit.length_mismatch", result.Error.Code);
    }
}
