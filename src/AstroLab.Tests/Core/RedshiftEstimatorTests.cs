using AstroLab.Core.Spectroscopy;

namespace AstroLab.Tests.Core;

public class RedshiftEstimatorTests
{
    [Fact]
    public void Estimate_SingleLinePair_ComputesExactRedshiftWithZeroUncertainty()
    {
        ReadOnlySpan<double> observed = [505.0];

        ReadOnlySpan<double> rest = [500.0];

        var result = RedshiftEstimator.Estimate(observed, rest);

        Assert.True(result.IsSuccess);

        Assert.Equal(0.01, result.Value.Redshift, precision: 9);

        Assert.Equal(0.0, result.Value.Uncertainty, precision: 9);
    }

    [Fact]
    public void Estimate_MultipleLinePairs_ComputesMeanAndStandardErrorOfMean()
    {
        ReadOnlySpan<double> observed = [505.0, 1020.0];

        ReadOnlySpan<double> rest = [500.0, 1000.0];

        var result = RedshiftEstimator.Estimate(observed, rest);

        Assert.True(result.IsSuccess);

        Assert.Equal(0.015, result.Value.Redshift, precision: 9);

        Assert.Equal(0.005, result.Value.Uncertainty, precision: 9);
    }

    [Fact]
    public void Estimate_RejectsLengthMismatch()
    {
        ReadOnlySpan<double> observed = [505.0, 1020.0];

        ReadOnlySpan<double> rest = [500.0];

        var result = RedshiftEstimator.Estimate(observed, rest);

        Assert.True(result.IsFailure);

        Assert.Equal("spectroscopy.redshift.length_mismatch", result.Error.Code);
    }

    [Fact]
    public void Estimate_RejectsEmptyInput()
    {
        var result = RedshiftEstimator.Estimate([], []);

        Assert.True(result.IsFailure);

        Assert.Equal("spectroscopy.redshift.no_line_pairs", result.Error.Code);
    }

    [Fact]
    public void Estimate_RejectsNonPositiveRestWavelength()
    {
        ReadOnlySpan<double> observed = [505.0];

        ReadOnlySpan<double> rest = [0.0];

        var result = RedshiftEstimator.Estimate(observed, rest);

        Assert.True(result.IsFailure);

        Assert.Equal("spectroscopy.redshift.invalid_rest_wavelength", result.Error.Code);
    }

    [Fact]
    public void Estimate_RejectsNonFiniteWavelength()
    {
        ReadOnlySpan<double> observed = [double.NaN];

        ReadOnlySpan<double> rest = [500.0];

        var result = RedshiftEstimator.Estimate(observed, rest);

        Assert.True(result.IsFailure);

        Assert.Equal("spectroscopy.redshift.non_finite_wavelength", result.Error.Code);
    }
}
