using System.Text;
using AstroLab.Core.Fits;

namespace AstroLab.Tests.Core;

public class FitsConformanceTests
{
    private static FitsHeader Header(params string[] cards) =>
        FitsHeader.Parse(Encoding.ASCII.GetBytes(string.Concat(cards.Select(c => c.PadRight(FitsCardParser.CardLength))))).Value;

    [Fact]
    public void ValidatePrimaryHeader_SimpleTrueFirst_Succeeds()
    {
        var result = FitsConformance.ValidatePrimaryHeader(Header("SIMPLE  =                    T", "BITPIX  =                    8", "NAXIS   =                    0", "END"));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void ValidatePrimaryHeader_SimpleNotFirst_FailsWithMissingSimple()
    {
        var result = FitsConformance.ValidatePrimaryHeader(Header("BITPIX  =                    8", "SIMPLE  =                    T", "NAXIS   =                    0", "END"));

        Assert.True(result.IsFailure);
        Assert.Equal("fits.header.missing_simple", result.Error.Code);
    }

    [Fact]
    public void ValidatePrimaryHeader_SimpleFalse_FailsAsNonconforming()
    {
        var result = FitsConformance.ValidatePrimaryHeader(Header("SIMPLE  =                    F", "BITPIX  =                    8", "NAXIS   =                    0", "END"));

        Assert.True(result.IsFailure);
        Assert.Equal("fits.header.nonconforming", result.Error.Code);
    }

    [Fact]
    public void ValidatePrimaryHeader_SimpleNotLogical_FailsAsNonconforming()
    {
        var result = FitsConformance.ValidatePrimaryHeader(Header("SIMPLE  =                    1", "BITPIX  =                    8", "NAXIS   =                    0", "END"));

        Assert.True(result.IsFailure);
        Assert.Equal("fits.header.nonconforming", result.Error.Code);
    }
}
