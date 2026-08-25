using System.Collections.Immutable;

namespace AstroLab.Api.Features.Spectroscopy.Extract;

public sealed record SpectrumExtractionResponse(string FileId, ImmutableList<double>? Wavelengths, ImmutableList<double> Flux);

/// <summary>Static factory accompanying <see cref="SpectrumExtractionResponse"/>. Validates arguments before constructing.</summary>
public static class SpectrumExtractionResponseFactory
{
    public static SpectrumExtractionResponse Create(string fileId, ImmutableList<double>? wavelengths, ImmutableList<double> flux)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        return new SpectrumExtractionResponse(fileId, wavelengths, flux);
    }
}
