using System.Text;

namespace AstroLab.Tests.Features;

/// <summary>Builds minimal, valid single-HDU FITS files for integration tests.</summary>
internal static class SyntheticFits
{
    private const int CardLength = 80;
    private const int BlockSize = 2880;

    /// <summary>
    /// A 4x2, 8-bit-per-pixel image with pixel values 10, 20, ..., 80 (row-major), which gives
    /// every downstream computation (statistics, aperture flux, spectral extraction) an exact,
    /// hand-checkable expected result.
    /// </summary>
    public static byte[] SmallGradientImage() => BuildSingleHdu(
    [
        "SIMPLE  =                    T",
        "BITPIX  =                    8",
        "NAXIS   =                    2",
        "NAXIS1  =                    4",
        "NAXIS2  =                    2",
        "END",
    ]);

    /// <summary>
    /// The same 4x2 gradient pixel data as <see cref="SmallGradientImage"/>, but carrying a
    /// standard TAN-projection WCS solution (no rotation, 1 arcsec/pixel), so astrometry endpoints
    /// have a usable pixel/sky mapping to exercise.
    /// </summary>
    public static byte[] SmallGradientImageWithWcs() => BuildSingleHdu(
    [
        "SIMPLE  =                    T",
        "BITPIX  =                    8",
        "NAXIS   =                    2",
        "NAXIS1  =                    4",
        "NAXIS2  =                    2",
        "CTYPE1  = 'RA---TAN'",
        "CTYPE2  = 'DEC--TAN'",
        "CRPIX1  =                  1.0",
        "CRPIX2  =                  1.0",
        "CRVAL1  =                180.0",
        "CRVAL2  =                  0.0",
        "CDELT1  =            -0.0002778",
        "CDELT2  =             0.0002778",
        "RADESYS = 'ICRS    '",
        "END",
    ]);

    /// <summary>
    /// The same 4x2 gradient pixel data as <see cref="SmallGradientImage"/>, but carrying a
    /// <c>DISPAXIS</c> keyword — the standard FITS marker for a 2D spectroscopic frame (a
    /// long-slit spectrogram with a spatial axis and a dispersion axis) — so
    /// <c>FitsDatasetClassifier</c> identifies it as <c>Spectrum</c> rather than <c>Image</c>.
    /// </summary>
    public static byte[] SmallGradientSpectrumFrame() => BuildSingleHdu(
    [
        "SIMPLE  =                    T",
        "BITPIX  =                    8",
        "NAXIS   =                    2",
        "NAXIS1  =                    4",
        "NAXIS2  =                    2",
        "DISPAXIS=                    1",
        "END",
    ]);

    /// <summary>
    /// A 3-HDU file where the ONLY HDU with pixel data (extension 1, a plain 4x2 gradient image)
    /// carries no spectral marker of its own, but an unrelated, dataless extension (2) carries a
    /// stray <c>DISPAXIS</c> card. Regression fixture for the classify/load HDU-selection mismatch:
    /// <c>FitsDatasetClassifier</c> must classify based on the same HDU
    /// <c>FitsDatasetReader</c> actually loads pixels from, not on whichever HDU happens to carry a
    /// marker — otherwise this file misclassifies as <c>Spectrum</c> even though the HDU that gets
    /// analyzed is a plain image.
    /// </summary>
    public static byte[] MultiHduImageWithUnrelatedSpectralMarker()
    {
        var primary = (
            Cards: new[]
            {
                "SIMPLE  =                    T",
                "BITPIX  =                    8",
                "NAXIS   =                    0",
                "END",
            },
            Data: Array.Empty<byte>());

        var imageExtension = (
            Cards: new[]
            {
                "XTENSION= 'IMAGE   '",
                "BITPIX  =                    8",
                "NAXIS   =                    2",
                "NAXIS1  =                    4",
                "NAXIS2  =                    2",
                "END",
            },
            Data: new byte[] { 10, 20, 30, 40, 50, 60, 70, 80 });

        var strayMarkerExtension = (
            Cards: new[]
            {
                "XTENSION= 'IMAGE   '",
                "BITPIX  =                    8",
                "NAXIS   =                    0",
                "DISPAXIS=                    1",
                "END",
            },
            Data: Array.Empty<byte>());

        return BuildMultiHdu([primary, imageExtension, strayMarkerExtension]);
    }

    /// <summary>
    /// A 12x12, 8-bit image with a low-contrast cyclic background (values 5..15) and a single
    /// 3x3, constant-value-200 block at rows/columns 4-6 — orders of magnitude above the
    /// background's robust noise estimate, so the default detection threshold finds exactly one
    /// source at a hand-checkable centroid/pixel count.
    /// </summary>
    public static byte[] SmallImageWithSource() => BuildMultiHdu(
    [
        (
            [
                "SIMPLE  =                    T",
                "BITPIX  =                    8",
                "NAXIS   =                    2",
                "NAXIS1  =                   12",
                "NAXIS2  =                   12",
                "END",
            ],
            BuildSourcePixelData())
    ]);

    /// <summary>The same source field as <see cref="SmallImageWithSource"/>, but with a TAN WCS solution so detected sources can be resolved to RA/Dec.</summary>
    public static byte[] SmallImageWithSourceAndWcs() => BuildMultiHdu(
    [
        (
            [
                "SIMPLE  =                    T",
                "BITPIX  =                    8",
                "NAXIS   =                    2",
                "NAXIS1  =                   12",
                "NAXIS2  =                   12",
                "CTYPE1  = 'RA---TAN'",
                "CTYPE2  = 'DEC--TAN'",
                "CRPIX1  =                  1.0",
                "CRPIX2  =                  1.0",
                "CRVAL1  =                180.0",
                "CRVAL2  =                  0.0",
                "CDELT1  =            -0.0002778",
                "CDELT2  =             0.0002778",
                "RADESYS = 'ICRS    '",
                "END",
            ],
            BuildSourcePixelData())
    ]);

    private static byte[] BuildSourcePixelData()
    {
        const int width = 12;
        const int height = 12;

        var pixels = new byte[width * height];

        for (var i = 0; i < pixels.Length; i++)
        {
            pixels[i] = (byte)(5 + (i % 11));
        }

        for (var y = 4; y <= 6; y++)
        {
            for (var x = 4; x <= 6; x++)
            {
                pixels[(y * width) + x] = 200;
            }
        }

        return pixels;
    }

    private static byte[] BuildSingleHdu(string[] cards)
    {
        byte[] pixels = [10, 20, 30, 40, 50, 60, 70, 80];
        return BuildMultiHdu([(cards, pixels)]);
    }

    private static byte[] BuildMultiHdu(IReadOnlyList<(string[] Cards, byte[] Data)> hdus)
    {
        using var output = new MemoryStream();

        foreach (var (cards, data) in hdus)
        {
            var header = new StringBuilder();
            foreach (var card in cards)
            {
                header.Append(card.PadRight(CardLength));
            }

            while (header.Length % BlockSize != 0)
            {
                header.Append(' ', CardLength);
            }

            var headerBytes = Encoding.ASCII.GetBytes(header.ToString());
            output.Write(headerBytes);

            if (data.Length > 0)
            {
                output.Write(data);
                var padding = (BlockSize - (data.Length % BlockSize)) % BlockSize;
                if (padding > 0)
                {
                    output.Write(new byte[padding]);
                }
            }
        }

        return output.ToArray();
    }
}
