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

        Assert.Equal(flux, continuum);
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
}
