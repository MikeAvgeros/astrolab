using System.Buffers.Binary;
using System.Text;

namespace AstroLab.Tests.Features;

/// <summary>Builds minimal, valid single-HDU FITS files for integration tests.</summary>
internal static class SyntheticFits
{
    private const int CardLength = 80;
    private const int BlockSize = 2880;
    
    public static byte[] SmallGradientImage() => BuildSingleHdu(
    [
        "SIMPLE  =                    T",
        "BITPIX  =                    8",
        "NAXIS   =                    2",
        "NAXIS1  =                    4",
        "NAXIS2  =                    2",
        "END",
    ]);
    
    public static byte[] SmallGradientImageWithBlankKeywordCards() => BuildSingleHdu(
    [
        "SIMPLE  =                    T",
        "BITPIX  =                    8",
        "NAXIS   =                    2",
        "NAXIS1  =                    4",
        "NAXIS2  =                    2",
        "        / DATA DESCRIPTION KEYWORDS",
        "",
        "END",
    ]);

    public static byte[] SmallGradientImageTransposed() => BuildSingleHdu(
    [
        "SIMPLE  =                    T",
        "BITPIX  =                    8",
        "NAXIS   =                    2",
        "NAXIS1  =                    2",
        "NAXIS2  =                    4",
        "END",
    ]);
    
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
    
    public static byte[] SmallSpectrumWithEmissionLine() => BuildMultiHdu(
    [
        (
            [
                "SIMPLE  =                    T",
                "BITPIX  =                    8",
                "NAXIS   =                    2",
                "NAXIS1  =                    9",
                "NAXIS2  =                    3",
                "DISPAXIS=                    1",
                "END",
            ],
            BuildEmissionLinePixelData())
    ]);

    public static byte[] SmallSpectrumWithEmissionLineAndDispersionWcs() => BuildMultiHdu(
    [
        (
            [
                "SIMPLE  =                    T",
                "BITPIX  =                    8",
                "NAXIS   =                    2",
                "NAXIS1  =                    9",
                "NAXIS2  =                    3",
                "DISPAXIS=                    1",
                "CRVAL1  =               5000.0",
                "CDELT1  =                  2.0",
                "END",
            ],
            BuildEmissionLinePixelData())
    ]);
    
    public static byte[] SmallSpectrumWithGaussianBumpAndDispersionWcs() => BuildMultiHdu(
    [
        (
            [
                "SIMPLE  =                    T",
                "BITPIX  =                    8",
                "NAXIS   =                    2",
                "NAXIS1  =                    9",
                "NAXIS2  =                    3",
                "DISPAXIS=                    1",
                "CRVAL1  =               5000.0",
                "CDELT1  =                  2.0",
                "END",
            ],
            BuildGaussianBumpPixelData())
    ]);
    
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
    
    public static byte[] SmallGradientImageWithShiftedWcs() => BuildSingleHdu(
    [
        "SIMPLE  =                    T",
        "BITPIX  =                    8",
        "NAXIS   =                    2",
        "NAXIS1  =                    4",
        "NAXIS2  =                    2",
        "CTYPE1  = 'RA---TAN'",
        "CTYPE2  = 'DEC--TAN'",
        "CRPIX1  =                  3.0",
        "CRPIX2  =                  4.0",
        "CRVAL1  =                180.0",
        "CRVAL2  =                  0.0",
        "CDELT1  =            -0.0002778",
        "CDELT2  =             0.0002778",
        "RADESYS = 'ICRS    '",
        "END",
    ]);
    
    public static byte[] SmallImageWithSourceShifted() => BuildMultiHdu(
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
            BuildSourcePixelData(blockMin: 7, blockMax: 9))
    ]);
    
    public static byte[] TimeSeriesBinaryTable(double[] time, double[] flux)
    {
        if (time.Length != flux.Length)
        {
            throw new ArgumentException("time and flux must have the same length.");
        }

        var primary = (
            Cards: new[]
            {
                "SIMPLE  =                    T",
                "BITPIX  =                    8",
                "NAXIS   =                    0",
                "END",
            },
            Data: Array.Empty<byte>());

        const int bytesPerRow = sizeof(double) * 2;

        var tableExtension = (
            Cards: new[]
            {
                "XTENSION= 'BINTABLE'",
                "BITPIX  =                    8",
                "NAXIS   =                    2",
                $"NAXIS1  =                   {bytesPerRow}",
                $"NAXIS2  =           {time.Length,10}",
                "PCOUNT  =                    0",
                "GCOUNT  =                    1",
                "TFIELDS =                    2",
                "TTYPE1  = 'TIME    '",
                "TFORM1  = '1D      '",
                "TTYPE2  = 'FLUX    '",
                "TFORM2  = '1D      '",
                "END",
            },
            Data: BuildTimeSeriesRows(time, flux));

        return BuildMultiHdu([primary, tableExtension]);
    }

    private static byte[] BuildTimeSeriesRows(double[] time, double[] flux)
    {
        var data = new byte[time.Length * sizeof(double) * 2];

        for (var row = 0; row < time.Length; row++)
        {
            var offset = row * sizeof(double) * 2;

            BinaryPrimitives.WriteDoubleBigEndian(data.AsSpan(offset, sizeof(double)), time[row]);

            BinaryPrimitives.WriteDoubleBigEndian(data.AsSpan(offset + sizeof(double), sizeof(double)), flux[row]);
        }

        return data;
    }

    private static byte[] BuildSourcePixelData(int blockMin = 4, int blockMax = 6)
    {
        const int width = 12;
        const int height = 12;

        var pixels = new byte[width * height];

        for (var i = 0; i < pixels.Length; i++)
        {
            pixels[i] = (byte)(5 + (i % 11));
        }

        for (var y = blockMin; y <= blockMax; y++)
        {
            for (var x = blockMin; x <= blockMax; x++)
            {
                pixels[(y * width) + x] = 200;
            }
        }

        return pixels;
    }

    private static byte[] BuildEmissionLinePixelData()
    {
        const int width = 9;
        const int height = 3;
        const int lineColumn = 4;

        var pixels = new byte[width * height];

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                pixels[(y * width) + x] = (byte)(x == lineColumn ? 100 : 10);
            }
        }

        return pixels;
    }

    private static byte[] BuildGaussianBumpPixelData()
    {
        const int width = 9;
        const int height = 3;

        byte[] columnValues = [10, 10, 12, 20, 40, 20, 12, 10, 10];

        var pixels = new byte[width * height];

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                pixels[(y * width) + x] = columnValues[x];
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
