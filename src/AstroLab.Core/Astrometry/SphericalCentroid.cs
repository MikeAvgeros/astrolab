using AstroLab.Core.Result;

namespace AstroLab.Core.Astrometry;

/// <summary>
/// Pure spherical mean position: averages a set of RA/Dec coordinates on the unit sphere, by
/// averaging each point's Cartesian unit vector and converting the result back to RA/Dec, rather
/// than naively averaging the RA/Dec numbers themselves (which is wrong across the RA 0/360 degree
/// wraparound).
/// </summary>
public static class SphericalCentroid
{
    private const double DegreesToRadians = Math.PI / 180.0;
    private const double RadiansToDegrees = 180.0 / Math.PI;
    private const double FullCircleDegrees = 360.0;

    public static Result<(double RightAscensionDegrees, double DeclinationDegrees)> Compute(
        IReadOnlyList<(double RightAscensionDegrees, double DeclinationDegrees)> positions)
    {
        if (positions.Count == 0)
        {
            return Error.Validation("astrometry.centroid.no_positions", "At least one position is required.");
        }

        var sumX = 0.0;

        var sumY = 0.0;

        var sumZ = 0.0;

        foreach (var (rightAscensionDegrees, declinationDegrees) in positions)
        {
            var rightAscensionRadians = rightAscensionDegrees * DegreesToRadians;

            var declinationRadians = declinationDegrees * DegreesToRadians;

            var cosDeclination = Math.Cos(declinationRadians);

            sumX += cosDeclination * Math.Cos(rightAscensionRadians);

            sumY += cosDeclination * Math.Sin(rightAscensionRadians);

            sumZ += Math.Sin(declinationRadians);
        }

        var norm = Math.Sqrt(sumX * sumX + sumY * sumY + sumZ * sumZ);

        if (norm < double.Epsilon)
        {
            return Error.Validation(
                "astrometry.centroid.indeterminate",
                "The supplied positions average to a zero vector (e.g. antipodal points), so no single centroid exists.");
        }

        var declinationOut = Math.Asin(Math.Clamp(sumZ / norm, -1.0, 1.0)) * RadiansToDegrees;

        var rightAscensionOut = Math.Atan2(sumY, sumX) * RadiansToDegrees;

        if (rightAscensionOut < 0.0)
        {
            rightAscensionOut += FullCircleDegrees;
        }

        return (rightAscensionOut, declinationOut);
    }
}
