using System.Text.Json.Serialization;

namespace AstroLab.Api.Features.TimeSeries.PhaseFold;

public sealed record PhaseFoldRequest
{
    [JsonConstructor]
    private PhaseFoldRequest(double period, double referenceEpoch = 0.0)
    {
        Period = period;
        ReferenceEpoch = referenceEpoch;
    }

    public double Period { get; }

    public double ReferenceEpoch { get; }

    public static PhaseFoldRequest Create(double period, double referenceEpoch = 0.0)
    {
        var request = new PhaseFoldRequest(period, referenceEpoch);

        request.Validate();

        return request;
    }

    public void Validate()
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(Period);
    }
}
