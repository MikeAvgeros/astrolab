using AstroLab.Core.Spectroscopy;

namespace AstroLab.Tests.Core;

public class ContinuumFitterTests
{
    [Fact]
    public void Fit_LinearContinuum_RecoversExactCoefficients()
    {
        double[] x = [0.0, 1.0, 2.0, 3.0, 4.0];

        double[] flux = [2.0, 5.0, 8.0, 11.0, 14.0];

        var result = ContinuumFitter.Fit(x, flux, polynomialDegree: 1, [], sigmaClipThreshold: null, sigmaClipIterations: null);

        Assert.True(result.IsSuccess);

        var (continuum, coefficients) = result.Value;

        Assert.Equal(2.0, coefficients[0], precision: 6);

        Assert.Equal(3.0, coefficients[1], precision: 6);

        Assert.Equal(flux, continuum, (expected, actual) => Math.Abs(expected - actual) < 1e-9);
    }

    [Fact]
    public void Fit_CubicContinuumOnOpticalWavelengthsInFluxCalibratedUnits_Succeeds()
    {
        var x = Enumerable.Range(0, 301).Select(i => 4000.0 + 10.0 * i).ToArray();

        var flux = x.Select(wavelength => 1e-16 * (1.0 + 0.2 * ((wavelength - 5500.0) / 1500.0))).ToArray();

        var result = ContinuumFitter.Fit(x, flux, polynomialDegree: 3, [], sigmaClipThreshold: null, sigmaClipIterations: null);

        Assert.True(result.IsSuccess);

        for (var i = 0; i < x.Length; i++)
        {
            Assert.Equal(1.0, result.Value.Continuum[i] / flux[i], precision: 6);
        }
    }

    [Fact]
    public void Fit_ExcludedRange_IsIgnoredByTheFit()
    {
        double[] x = [0.0, 1.0, 2.0, 3.0, 4.0];

        double[] flux = [10.0, 10.0, 1000.0, 10.0, 10.0];

        var result = ContinuumFitter.Fit(
            x, flux, polynomialDegree: 0, [(1.5, 2.5)], sigmaClipThreshold: null, sigmaClipIterations: null);

        Assert.True(result.IsSuccess);

        var (continuum, coefficients) = result.Value;

        Assert.Equal(10.0, coefficients[0], precision: 6);

        Assert.All(continuum, value => Assert.Equal(10.0, value, precision: 6));
    }

    [Fact]
    public void Fit_SigmaClipping_RemovesInjectedOutlier()
    {
        double[] x = [0.0, 1.0, 2.0, 3.0, 4.0, 5.0, 6.0];

        double[] flux = [10.0, 10.0, 10.0, 1000.0, 10.0, 10.0, 10.0];

        var result = ContinuumFitter.Fit(
            x, flux, polynomialDegree: 0, [], sigmaClipThreshold: 2.0, sigmaClipIterations: 3);

        Assert.True(result.IsSuccess);

        var (_, coefficients) = result.Value;

        Assert.Equal(10.0, coefficients[0], precision: 6);
    }

    [Fact]
    public void Fit_RejectsLengthMismatch()
    {
        double[] x = [0.0, 1.0];

        double[] flux = [1.0];

        var result = ContinuumFitter.Fit(x, flux, polynomialDegree: 0, [], null, null);

        Assert.True(result.IsFailure);

        Assert.Equal("spectroscopy.continuum.length_mismatch", result.Error.Code);
    }

    [Fact]
    public void Fit_RejectsEmptySpectrum()
    {
        var result = ContinuumFitter.Fit([], [], polynomialDegree: 0, [], null, null);

        Assert.True(result.IsFailure);

        Assert.Equal("spectroscopy.continuum.empty_spectrum", result.Error.Code);
    }

    [Fact]
    public void Fit_RejectsTooFewPointsForRequestedDegree()
    {
        double[] x = [0.0, 1.0];

        double[] flux = [1.0, 2.0];

        var result = ContinuumFitter.Fit(x, flux, polynomialDegree: 2, [], null, null);

        Assert.True(result.IsFailure);

        Assert.Equal("spectroscopy.continuum.insufficient_points", result.Error.Code);
    }

    [Fact]
    public void Fit_HighDegreePolynomialOnOpticalWavelengths_RecoversItExactly()
    {
        // A degree-7 continuum over 4000-7000 A: in raw powers of x the normal equations are hopelessly
        // ill-conditioned; in the normalized variable the fit is exact to rounding.
        var x = Enumerable.Range(0, 601).Select(i => 4000.0 + 5.0 * i).ToArray();

        static double Truth(double wavelength)
        {
            var t = (wavelength - 5500.0) / 1500.0;

            return 1.0 + 0.3 * t - 0.2 * t * t + 0.05 * Math.Pow(t, 5) - 0.04 * Math.Pow(t, 7);
        }

        var flux = x.Select(Truth).ToArray();

        var result = ContinuumFitter.Fit(x, flux, polynomialDegree: 7, [], sigmaClipThreshold: null, sigmaClipIterations: null);

        Assert.True(result.IsSuccess);

        var (continuum, coefficients) = result.Value;

        for (var i = 0; i < x.Length; i++)
        {
            Assert.Equal(flux[i], continuum[i], precision: 9);
        }

        // The raw power-basis coefficients describe the same polynomial.
        Assert.Equal(flux[300], SpectrumExtractor.EvaluateWavelength(x[300], coefficients), precision: 4);
    }

    [Fact]
    public void Fit_DegreeAboveMaximum_ReturnsValidationError()
    {
        var x = Enumerable.Range(0, 100).Select(i => (double)i).ToArray();

        var result = ContinuumFitter.Fit(x, x, polynomialDegree: ContinuumFitter.MaxPolynomialDegree + 1, [], null, null);

        Assert.True(result.IsFailure);

        Assert.Equal("spectroscopy.continuum.invalid_degree", result.Error.Code);
    }
}
