using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace AstroLab.Api.Features.Spectroscopy.Redshift;

public sealed record RedshiftEstimationRequest
{
    [JsonConstructor]
    private RedshiftEstimationRequest(
        ImmutableList<double>? observedWavelengths,
        ImmutableList<double>? restWavelengths,
        ImmutableList<double>? observedSpectrumWavelengths,
        ImmutableList<double>? observedFlux,
        ImmutableList<double>? templateWavelengths,
        ImmutableList<double>? templateFlux,
        double? minRedshift,
        double? maxRedshift)
    {
        ObservedWavelengths = observedWavelengths;
        RestWavelengths = restWavelengths;
        ObservedSpectrumWavelengths = observedSpectrumWavelengths;
        ObservedFlux = observedFlux;
        TemplateWavelengths = templateWavelengths;
        TemplateFlux = templateFlux;
        MinRedshift = minRedshift;
        MaxRedshift = maxRedshift;
    }
    
    public ImmutableList<double>? ObservedWavelengths { get; }
    
    public ImmutableList<double>? RestWavelengths { get; }
    
    public ImmutableList<double>? ObservedSpectrumWavelengths { get; }
    
    public ImmutableList<double>? ObservedFlux { get; }
    
    public ImmutableList<double>? TemplateWavelengths { get; }
    
    public ImmutableList<double>? TemplateFlux { get; }

    public double? MinRedshift { get; }

    public double? MaxRedshift { get; }

    public static RedshiftEstimationRequest Create(
        ImmutableList<double>? observedWavelengths = null,
        ImmutableList<double>? restWavelengths = null,
        ImmutableList<double>? observedSpectrumWavelengths = null,
        ImmutableList<double>? observedFlux = null,
        ImmutableList<double>? templateWavelengths = null,
        ImmutableList<double>? templateFlux = null,
        double? minRedshift = null,
        double? maxRedshift = null) =>
        new(observedWavelengths, restWavelengths, observedSpectrumWavelengths, observedFlux, templateWavelengths, templateFlux, minRedshift, maxRedshift);
    
    public bool HasTemplateInputs =>
        ObservedSpectrumWavelengths is { Count: > 0 } &&
        ObservedFlux is { Count: > 0 } &&
        TemplateWavelengths is { Count: > 0 } &&
        TemplateFlux is { Count: > 0 };
}
