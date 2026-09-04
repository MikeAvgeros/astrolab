namespace AstroLab.Api.Features.Measurements.RadialVelocity;

public sealed record RadialVelocityRequest
{
    private RadialVelocityRequest(double restWavelengthNm, double observedWavelengthNm)
    {
        RestWavelengthNm = restWavelengthNm;
        ObservedWavelengthNm = observedWavelengthNm;
    }

    public double RestWavelengthNm { get; }

    public double ObservedWavelengthNm { get; }

    public static RadialVelocityRequest Create(double restWavelengthNm, double observedWavelengthNm)
    {
        var request = new RadialVelocityRequest(restWavelengthNm, observedWavelengthNm);

        request.Validate();

        return request;
    }

    public void Validate()
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(RestWavelengthNm);

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(ObservedWavelengthNm);
    }
}
