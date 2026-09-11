using AstroLab.Core.Result;

namespace AstroLab.Core.Astrometry;

/// <summary>
/// Pure conversion of an angular size into a physical size given an assumed distance, using the
/// small-angle relation that defines the parsec itself: an object one astronomical unit across
/// subtends exactly one arcsecond as seen from one parsec away, so physical size in AU equals
/// angular size in arcseconds times distance in parsecs.
/// </summary>
public static class PhysicalSizeCalculator
{
    public static Result<double> ComputeAstronomicalUnits(double angularSizeArcsec, double distanceParsecs)
    {
        if (angularSizeArcsec <= 0.0 || !double.IsFinite(angularSizeArcsec))
        {
            return Error.Validation(
                "astrometry.physicalsize.invalid_angular_size", "angularSizeArcsec must be a finite, positive value.");
        }

        if (distanceParsecs <= 0.0 || !double.IsFinite(distanceParsecs))
        {
            return Error.Validation(
                "astrometry.physicalsize.invalid_distance", "distanceParsecs must be a finite, positive value.");
        }

        return angularSizeArcsec * distanceParsecs;
    }
}
