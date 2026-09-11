using AstroLab.Core.Result;

namespace AstroLab.Core.Spectroscopy;

/// <summary>
/// Pure line-of-sight radial velocity estimation from a single observed/rest-frame spectral line
/// wavelength pair: the classical (non-relativistic) Doppler approximation v = c * z, valid for
/// the sub-relativistic velocities typical of stellar radial-velocity measurements. Reuses
/// <see cref="RedshiftEstimator"/> for the underlying fractional wavelength shift.
/// </summary>
public static class RadialVelocityEstimator
{
    public const double SpeedOfLightKmPerSecond = 299792.458;

    public static Result<double> EstimateKilometersPerSecond(double observedWavelengthNm, double restWavelengthNm)
    {
        ReadOnlySpan<double> observed = [observedWavelengthNm];

        ReadOnlySpan<double> rest = [restWavelengthNm];

        var redshiftResult = RedshiftEstimator.Estimate(observed, rest);

        return redshiftResult.Map(estimate => estimate.Redshift * SpeedOfLightKmPerSecond);
    }
}
