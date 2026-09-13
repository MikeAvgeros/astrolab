using AstroLab.Core.Astrometry;
using AstroLab.Core.Fits;

namespace AstroLab.Api.Features.Fits.Observation;

public sealed record ObservationDerivedMetadataDto
{
    private ObservationDerivedMetadataDto(
        FitsDatasetKind datasetKind, bool hasWcs, WcsProjection? projection,
        double? pixelScaleXArcsecPerPixel, double? pixelScaleYArcsecPerPixel, double? rotationDegrees)
    {
        DatasetKind = datasetKind;
        HasWcs = hasWcs;
        Projection = projection;
        PixelScaleXArcsecPerPixel = pixelScaleXArcsecPerPixel;
        PixelScaleYArcsecPerPixel = pixelScaleYArcsecPerPixel;
        RotationDegrees = rotationDegrees;
    }

    public FitsDatasetKind DatasetKind { get; }

    public bool HasWcs { get; }

    public WcsProjection? Projection { get; }

    public double? PixelScaleXArcsecPerPixel { get; }

    public double? PixelScaleYArcsecPerPixel { get; }

    public double? RotationDegrees { get; }

    public static ObservationDerivedMetadataDto Create(FitsDatasetKind datasetKind, FitsHeader wcsHeader)
    {
        ArgumentNullException.ThrowIfNull(wcsHeader);

        var wcsResult = Wcs.FromHeader(wcsHeader);

        if (wcsResult.IsFailure)
        {
            return new ObservationDerivedMetadataDto(datasetKind, hasWcs: false, null, null, null, null);
        }

        var wcs = wcsResult.Value;

        return new ObservationDerivedMetadataDto(
            datasetKind, hasWcs: true, wcs.Projection, wcs.PixelScaleXArcsecPerPixel, wcs.PixelScaleYArcsecPerPixel, wcs.RotationDegrees);
    }
}
