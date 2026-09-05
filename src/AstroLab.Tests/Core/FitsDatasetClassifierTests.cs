using System.Text;
using AstroLab.Core.Fits;

namespace AstroLab.Tests.Core;

public class FitsDatasetClassifierTests
{
    private static string PadCard(string content) => content.PadRight(FitsCardParser.CardLength);

    private static byte[] BuildHeaderBlock(params string[] cards) =>
        Encoding.ASCII.GetBytes(string.Concat(Array.ConvertAll(cards, PadCard)));

    private static HduDescriptor BuildHdu(int index, params string[] cards)
    {
        var header = FitsHeader.Parse(BuildHeaderBlock(cards)).Value;

        return HduDescriptor.FromHeader(index, header);
    }

    private static HduDescriptor PrimaryHdu() => BuildHdu(
        0,
        "SIMPLE  =                    T",
        "BITPIX  =                    8",
        "NAXIS   =                    0",
        "END");

    private static HduDescriptor ImageHdu(int index) => BuildHdu(
        index,
        "XTENSION= 'IMAGE   '",
        "BITPIX  =                  -32",
        "NAXIS   =                    2",
        "NAXIS1  =                  100",
        "NAXIS2  =                   50",
        "END");

    private static HduDescriptor SpectrumHdu(int index) => BuildHdu(
        index,
        "XTENSION= 'IMAGE   '",
        "BITPIX  =                  -32",
        "NAXIS   =                    1",
        "NAXIS1  =                  256",
        "END");

    private static HduDescriptor TableHdu(int index, int fieldCount, params string[] columnNames)
    {
        var cards = new List<string>
        {
            "XTENSION= 'BINTABLE'",
            "BITPIX  =                    8",
            "NAXIS   =                    2",
            "NAXIS1  =                   16",
            "NAXIS2  =                  100",
            "PCOUNT  =                    0",
            "GCOUNT  =                    1",
            $"TFIELDS =                    {fieldCount}",
        };

        for (var i = 0; i < columnNames.Length; i++)
        {
            cards.Add($"TTYPE{i + 1}  = '{columnNames[i],-8}'");
        }

        cards.Add("END");

        return BuildHdu(index, [.. cards]);
    }

    [Fact]
    public void Classify_ImageHdu_ReturnsImage()
    {
        var hdus = new[] { PrimaryHdu(), ImageHdu(1) };

        Assert.Equal(FitsDatasetKind.Image, FitsDatasetClassifier.Classify(hdus));
    }

    [Fact]
    public void Classify_SingleAxisImageHdu_ReturnsSpectrum()
    {
        var hdus = new[] { PrimaryHdu(), SpectrumHdu(1) };

        Assert.Equal(FitsDatasetKind.Spectrum, FitsDatasetClassifier.Classify(hdus));
    }

    [Fact]
    public void Classify_TableWithOnlyTimeColumn_DoesNotReportTimeSeries()
    {
        var hdus = new[] { PrimaryHdu(), TableHdu(1, 1, "TIME") };

        Assert.Equal(FitsDatasetKind.Table, FitsDatasetClassifier.Classify(hdus));

        Assert.False(FitsDatasetClassifier.EnsureKind(hdus, FitsDatasetKind.TimeSeries).IsSuccess);
    }

    [Fact]
    public void Classify_TableWithTimeAndMeasurementColumn_ReturnsTimeSeries()
    {
        var hdus = new[] { PrimaryHdu(), TableHdu(1, 2, "TIME", "FLUX") };

        Assert.Equal(FitsDatasetKind.TimeSeries, FitsDatasetClassifier.Classify(hdus));

        Assert.True(FitsDatasetClassifier.EnsureKind(hdus, FitsDatasetKind.TimeSeries).IsSuccess);
    }

    [Fact]
    public void Classify_TableWithoutTimeColumn_ReturnsTable()
    {
        var hdus = new[] { PrimaryHdu(), TableHdu(1, 2, "RA", "DEC") };

        Assert.Equal(FitsDatasetKind.Table, FitsDatasetClassifier.Classify(hdus));
    }

    [Fact]
    public void HasCapability_ImageAndTimeSeriesTableCoexist_BothCapabilitiesRecognised()
    {
        var hdus = new[] { PrimaryHdu(), ImageHdu(1), TableHdu(2, 2, "TIME", "FLUX") };

        Assert.True(FitsDatasetClassifier.EnsureKind(hdus, FitsDatasetKind.Image).IsSuccess);

        Assert.True(FitsDatasetClassifier.EnsureKind(hdus, FitsDatasetKind.TimeSeries).IsSuccess);

        Assert.True(FitsDatasetClassifier.EnsureKind(hdus, FitsDatasetKind.Table).IsSuccess);
    }

    [Fact]
    public void EnsureKind_RequiredCapabilityMissing_ReturnsValidationFailure()
    {
        var hdus = new[] { PrimaryHdu(), TableHdu(1, 1, "RA") };

        var result = FitsDatasetClassifier.EnsureKind(hdus, FitsDatasetKind.Image);

        Assert.True(result.IsFailure);

        Assert.Equal("fits.data.unsupported_type", result.Error.Code);
    }

    [Fact]
    public void EnsureKind_RequiredCapabilityPresent_ReturnsSuccess()
    {
        var hdus = new[] { PrimaryHdu(), ImageHdu(1) };

        var result = FitsDatasetClassifier.EnsureKind(hdus, FitsDatasetKind.Image);

        Assert.True(result.IsSuccess);

        Assert.Equal(FitsDatasetKind.Image, result.Value);
    }

    [Fact]
    public void MatchesKind_SpectrumHdu_MatchesSpectrumNotImage()
    {
        var hdu = SpectrumHdu(1);

        Assert.True(FitsDatasetClassifier.MatchesKind(hdu, FitsDatasetKind.Spectrum));

        Assert.False(FitsDatasetClassifier.MatchesKind(hdu, FitsDatasetKind.Image));
    }

    [Fact]
    public void MatchesKind_ImageHdu_MatchesImageNotSpectrum()
    {
        var hdu = ImageHdu(1);

        Assert.True(FitsDatasetClassifier.MatchesKind(hdu, FitsDatasetKind.Image));

        Assert.False(FitsDatasetClassifier.MatchesKind(hdu, FitsDatasetKind.Spectrum));
    }
}
