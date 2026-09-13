namespace AstroLab.Api.Features.Images.MultiPhotometry;

public sealed record SourcePhotometryDto
{
    private SourcePhotometryDto(
        int sourceId, double netFlux, double fluxUncertainty, double instrumentalMagnitude, double magnitudeUncertainty,
        double? signalToNoiseRatio)
    {
        SourceId = sourceId;
        NetFlux = netFlux;
        FluxUncertainty = fluxUncertainty;
        InstrumentalMagnitude = instrumentalMagnitude;
        MagnitudeUncertainty = magnitudeUncertainty;
        SignalToNoiseRatio = signalToNoiseRatio;
    }

    public int SourceId { get; }

    public double NetFlux { get; }

    public double FluxUncertainty { get; }

    public double InstrumentalMagnitude { get; }

    public double MagnitudeUncertainty { get; }

    public double? SignalToNoiseRatio { get; }

    public static SourcePhotometryDto Create(
        int sourceId, double netFlux, double fluxUncertainty, double instrumentalMagnitude, double magnitudeUncertainty,
        double? signalToNoiseRatio)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(sourceId);

        return new SourcePhotometryDto(sourceId, netFlux, fluxUncertainty, instrumentalMagnitude, magnitudeUncertainty, signalToNoiseRatio);
    }
}
