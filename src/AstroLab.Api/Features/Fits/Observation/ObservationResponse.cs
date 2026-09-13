using System.Collections.Immutable;
using AstroLab.Core.Fits;

namespace AstroLab.Api.Features.Fits.Observation;

public sealed record ObservationResponse
{
    private ObservationResponse(string fileId, ObservationHeaderMetadataDto header, ObservationDerivedMetadataDto derived)
    {
        FileId = fileId;
        Header = header;
        Derived = derived;
    }

    public string FileId { get; }

    public ObservationHeaderMetadataDto Header { get; }

    public ObservationDerivedMetadataDto Derived { get; }

    public static ObservationResponse Create(string fileId, ImmutableArray<HduDescriptor> hdus)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        var primaryHeader = hdus[0].Header;

        var header = ObservationHeaderMetadataDto.Create(primaryHeader);

        var datasetKind = FitsDatasetClassifier.Classify(hdus);

        var wcsHeader = FindFirstImageHeader(hdus) ?? primaryHeader;

        var derived = ObservationDerivedMetadataDto.Create(datasetKind, wcsHeader);

        return new ObservationResponse(fileId, header, derived);
    }

    private static FitsHeader? FindFirstImageHeader(ImmutableArray<HduDescriptor> hdus)
    {
        foreach (var hdu in hdus)
        {
            if (hdu.Image is { PixelCount: > 0 })
            {
                return hdu.Header;
            }
        }

        return null;
    }
}
