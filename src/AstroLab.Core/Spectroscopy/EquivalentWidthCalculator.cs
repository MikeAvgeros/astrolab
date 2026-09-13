using AstroLab.Core.Result;

namespace AstroLab.Core.Spectroscopy;

/// <summary>
/// Computes the equivalent width of a spectral feature over a caller-supplied wavelength window: a
/// pseudo-continuum is taken as the straight line connecting the flux at the window's first and last
/// sample, and the equivalent width is the trapezoidal integral of <c>1 - flux/continuum</c> across
/// the window. By this sign convention, a positive result is an absorption feature (flux below the
/// pseudo-continuum) and a negative result is an emission feature (flux above it).
/// </summary>
public static class EquivalentWidthCalculator
{
    private const int MinimumPoints = 2;
    private const double ContinuumZeroTolerance = 1e-12;

    public static Result<double> Calculate(ReadOnlySpan<double> wavelengths, ReadOnlySpan<double> flux)
    {
        if (wavelengths.Length != flux.Length)
        {
            return Error.Validation(
                "spectroscopy.equivalent_width.length_mismatch",
                $"wavelengths length ({wavelengths.Length}) must equal flux length ({flux.Length}).");
        }

        if (wavelengths.Length < MinimumPoints)
        {
            return Error.Validation(
                "spectroscopy.equivalent_width.insufficient_points",
                $"At least {MinimumPoints} points are required to compute an equivalent width, but only {wavelengths.Length} fall within the requested window.");
        }
        
        var orderedWavelengths = wavelengths.ToArray();

        var orderedFlux = flux.ToArray();

        if (orderedWavelengths[0] > orderedWavelengths[^1])
        {
            Array.Reverse(orderedWavelengths);

            Array.Reverse(orderedFlux);
        }

        var startWavelength = orderedWavelengths[0];

        var endWavelength = orderedWavelengths[^1];

        var startFlux = orderedFlux[0];

        var endFlux = orderedFlux[^1];

        var span = endWavelength - startWavelength;

        var equivalentWidth = 0.0;

        var previousValue = ContinuumRelativeDeficit(startWavelength, startFlux, startWavelength, startFlux, endFlux, span);

        if (double.IsNaN(previousValue))
        {
            return Error.Validation(
                "spectroscopy.equivalent_width.zero_continuum", "The pseudo-continuum is at or near zero somewhere in the window; equivalent width is undefined.");
        }

        for (var i = 1; i < orderedWavelengths.Length; i++)
        {
            var currentValue = ContinuumRelativeDeficit(orderedWavelengths[i], orderedFlux[i], startWavelength, startFlux, endFlux, span);

            if (double.IsNaN(currentValue))
            {
                return Error.Validation(
                    "spectroscopy.equivalent_width.zero_continuum", "The pseudo-continuum is at or near zero somewhere in the window; equivalent width is undefined.");
            }

            var deltaWavelength = orderedWavelengths[i] - orderedWavelengths[i - 1];

            equivalentWidth += (previousValue + currentValue) / 2.0 * deltaWavelength;

            previousValue = currentValue;
        }

        return equivalentWidth;
    }

    private static double ContinuumRelativeDeficit(
        double wavelength, double flux, double startWavelength, double startFlux, double endFlux, double span)
    {
        var continuum = span == 0.0
            ? startFlux
            : startFlux + (endFlux - startFlux) * (wavelength - startWavelength) / span;

        return Math.Abs(continuum) <= ContinuumZeroTolerance ? double.NaN : 1.0 - (flux / continuum);
    }
}
