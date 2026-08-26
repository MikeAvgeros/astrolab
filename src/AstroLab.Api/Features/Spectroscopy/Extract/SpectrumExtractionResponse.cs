using System.Collections.Immutable;

namespace AstroLab.Api.Features.Spectroscopy.Extract;

public sealed record SpectrumExtractionResponse
{
    private SpectrumExtractionResponse(string fileId, ImmutableList<double>? wavelengths, ImmutableList<double> flux)
    {
        FileId = fileId;
        Wavelengths = wavelengths;
        Flux = flux;
    }

    public string FileId { get; }

    public ImmutableList<double>? Wavelengths { get; }

    public ImmutableList<double> Flux { get; }

    public static SpectrumExtractionResponse Create(string fileId, ImmutableList<double>? wavelengths, ImmutableList<double> flux)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        return new SpectrumExtractionResponse(fileId, wavelengths, flux);
    }
}
