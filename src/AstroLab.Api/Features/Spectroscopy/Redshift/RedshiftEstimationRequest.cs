using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace AstroLab.Api.Features.Spectroscopy.Redshift;

public sealed record RedshiftEstimationRequest
{
    [JsonConstructor]
    private RedshiftEstimationRequest(ImmutableList<double> observedWavelengths, ImmutableList<double> restWavelengths)
    {
        ObservedWavelengths = observedWavelengths;
        RestWavelengths = restWavelengths;
    }

    public ImmutableList<double> ObservedWavelengths { get; }

    public ImmutableList<double> RestWavelengths { get; }

    public static RedshiftEstimationRequest Create(ImmutableList<double> observedWavelengths, ImmutableList<double> restWavelengths) =>
        new(observedWavelengths, restWavelengths);
}
