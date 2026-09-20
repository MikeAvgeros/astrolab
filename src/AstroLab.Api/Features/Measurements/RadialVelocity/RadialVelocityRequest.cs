namespace AstroLab.Api.Features.Measurements.RadialVelocity;

public sealed record RadialVelocityRequest
{
    private RadialVelocityRequest(double restWavelengthNm, double observedWavelengthNm)
    {
        RestWavelengthNm = restWavelengthNm;
        ObservedWavelengthNm = observedWavelengthNm;
    }

    /// <summary>In nanometers. Note this differs from the spectroscopy endpoints (e.g. /lines, /redshift), which use the file's native dispersion unit (typically Ångström) — convert before passing a value between them.</summary>
    public double RestWavelengthNm { get; }

    /// <summary>In nanometers. Note this differs from the spectroscopy endpoints (e.g. /lines, /redshift), which use the file's native dispersion unit (typically Ångström) — convert before passing a value between them.</summary>
    public double ObservedWavelengthNm { get; }

    public static RadialVelocityRequest Create(double restWavelengthNm, double observedWavelengthNm)
    {
        var request = new RadialVelocityRequest(restWavelengthNm, observedWavelengthNm);

        request.Validate();

        return request;
    }

    private void Validate()
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(RestWavelengthNm);

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(ObservedWavelengthNm);
    }
}
