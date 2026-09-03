using System.Text.Json.Serialization;

namespace AstroLab.Api.Features.TimeSeries.Detrend;

public sealed record DetrendRequest
{
    [JsonConstructor]
    private DetrendRequest(string method)
    {
        Method = method;
    }

    public string Method { get; }

    public static DetrendRequest Create(string method)
    {
        var request = new DetrendRequest(method);

        request.Validate();

        return request;
    }

    public void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Method);
    }
}
