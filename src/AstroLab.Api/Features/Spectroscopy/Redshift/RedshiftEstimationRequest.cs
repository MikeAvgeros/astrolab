using System.Collections.Immutable;

namespace AstroLab.Api.Features.Spectroscopy.Redshift;

public sealed record RedshiftEstimationRequest(ImmutableList<double> ObservedWavelengths, ImmutableList<double> RestWavelengths);

/// <summary>Static factory accompanying <see cref="RedshiftEstimationRequest"/>. Validates arguments before constructing.</summary>
public static class RedshiftEstimationRequestFactory
{
    public static RedshiftEstimationRequest Create(ImmutableList<double> observedWavelengths, ImmutableList<double> restWavelengths) =>
        new(observedWavelengths, restWavelengths);
}
