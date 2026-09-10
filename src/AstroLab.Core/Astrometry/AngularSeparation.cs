using AstroLab.Core.Result;

namespace AstroLab.Core.Astrometry;

/// <summary>
/// Computes the great-circle angular separation between two points on the celestial sphere using
/// the Vincenty formula, which stays numerically stable at both very small and near-antipodal
/// separations (unlike the plain spherical law of cosines).
/// </summary>
public static class AngularSeparation
{
    private const double DegreesToRadians = Math.PI / 180.0;
    private const double RadiansToDegrees = 180.0 / Math.PI;
    private const double ArcsecondsPerDegree = 3600.0;
    private const double MinDeclinationDegrees = -90.0;
    private const double MaxDeclinationDegrees = 90.0;

    public static Result<double> ComputeArcseconds(
        double rightAscension1Degrees, double declination1Degrees, double rightAscension2Degrees, double declination2Degrees)
    {
        if (declination1Degrees is < MinDeclinationDegrees or > MaxDeclinationDegrees
            || declination2Degrees is < MinDeclinationDegrees or > MaxDeclinationDegrees)
        {
            return Error.Validation("astrometry.invalid_declination", "declination must be between -90 and 90 degrees.");
        }

        var delta1 = declination1Degrees * DegreesToRadians;

        var delta2 = declination2Degrees * DegreesToRadians;

        var deltaAlpha = (rightAscension2Degrees - rightAscension1Degrees) * DegreesToRadians;

        var sinDelta1 = Math.Sin(delta1);

        var cosDelta1 = Math.Cos(delta1);

        var sinDelta2 = Math.Sin(delta2);

        var cosDelta2 = Math.Cos(delta2);

        var sinDeltaAlpha = Math.Sin(deltaAlpha);

        var cosDeltaAlpha = Math.Cos(deltaAlpha);

        var crossTermX = cosDelta2 * sinDeltaAlpha;

        var crossTermY = cosDelta1 * sinDelta2 - sinDelta1 * cosDelta2 * cosDeltaAlpha;

        var numerator = Math.Sqrt(crossTermX * crossTermX + crossTermY * crossTermY);

        var denominator = sinDelta1 * sinDelta2 + cosDelta1 * cosDelta2 * cosDeltaAlpha;

        var separationRadians = Math.Atan2(numerator, denominator);

        return separationRadians * RadiansToDegrees * ArcsecondsPerDegree;
    }
}
