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
    public static byte[] SmallGradientImage()
    {
        byte[] pixels = [10, 20, 30, 40, 50, 60, 70, 80];
        string[] cards =
        [
            "SIMPLE  =                    T",
            "BITPIX  =                    8",
            "NAXIS   =                    2",
            "NAXIS1  =                    4",
            "NAXIS2  =                    2",
            "END",
        ];

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
        var result = new byte[headerBytes.Length + pixels.Length];
        headerBytes.CopyTo(result, 0);
        pixels.CopyTo(result, headerBytes.Length);
        return result;
    }
}
