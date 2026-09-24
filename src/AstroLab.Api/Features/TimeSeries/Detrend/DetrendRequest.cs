using System.Text.Json.Serialization;

namespace AstroLab.Api.Features.TimeSeries.Detrend;

public sealed record DetrendRequest
{
    [JsonConstructor]
    private DetrendRequest(string method, double? windowDuration = null)
    {
        Method = method;
        WindowDuration = windowDuration;
    }

    public string Method { get; }

    public double? WindowDuration { get; }

    public static DetrendRequest Create(string method, double? windowDuration = null)
    {
        var request = new DetrendRequest(method, windowDuration);

        request.Validate();

        return request;
    }

    public void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Method);

        if (WindowDuration is { } windowDuration)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(windowDuration);
        }
    }
}
