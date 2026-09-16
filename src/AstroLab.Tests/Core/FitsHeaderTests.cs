using System.Linq;
using System.Text;
using AstroLab.Core.Fits;

namespace AstroLab.Tests.Core;

public class FitsHeaderTests
{
    private static string PadCard(string content)
    {
        if (content.Length > FitsCardParser.CardLength)
        {
            throw new ArgumentException("Card content exceeds 80 characters.", nameof(content));
        }

        return content.PadRight(FitsCardParser.CardLength);
    }

    private static byte[] BuildHeaderBlock(params string[] cards) =>
        Encoding.ASCII.GetBytes(string.Concat(Array.ConvertAll(cards, PadCard)));

    [Theory]
    [InlineData("SIMPLE  =                    T / conforms to FITS standard", "SIMPLE", true)]
    public void Parse_LogicalCard_ProducesLogicalValue(string card, string expectedName, bool expectedValue)
    {
        var result = FitsCardParser.Parse(PadCard(card));

        Assert.True(result.IsSuccess);

        Assert.Equal(expectedName, result.Value.Name);

        Assert.Equal(FitsValueKind.Logical, result.Value.Value.Kind);

        Assert.Equal(expectedValue, result.Value.Value.AsLogical);
    }

    [Fact]
    public void Parse_IntegerCard_ProducesIntegerValue()
    {
        var result = FitsCardParser.Parse(PadCard("BITPIX  =                   16 / bits per pixel"));

        Assert.True(result.IsSuccess);

        Assert.Equal("BITPIX", result.Value.Name);

        Assert.Equal(16L, result.Value.Value.AsInteger);

        Assert.Equal("bits per pixel", result.Value.Comment);
    }

    [Fact]
    public void Parse_StringCard_TrimsTrailingSpacesInsideQuotes()
    {
        var result = FitsCardParser.Parse(PadCard("TELESCOP= 'HST     '           / Telescope name"));

        Assert.True(result.IsSuccess);

        Assert.Equal(FitsValueKind.String, result.Value.Value.Kind);

        Assert.Equal("HST", result.Value.Value.AsString);

        Assert.Equal("Telescope name", result.Value.Comment);
    }

    [Fact]
    public void Parse_RealCard_ProducesRealValue()
    {
        var result = FitsCardParser.Parse(PadCard("EXPTIME =                 30.5 / exposure time in seconds"));

        Assert.True(result.IsSuccess);

        Assert.Equal(30.5, result.Value.Value.AsReal, precision: 6);
    }

    [Fact]
    public void Parse_CommentCard_HasNoValue()
    {
        var result = FitsCardParser.Parse(PadCard("COMMENT This is a free-form comment"));

        Assert.True(result.IsSuccess);

        Assert.Equal("COMMENT", result.Value.Name);

        Assert.Equal(FitsValueKind.None, result.Value.Value.Kind);

        Assert.Equal("This is a free-form comment", result.Value.Comment);
    }

    [Fact]
    public void Parse_HierarchCard_ProducesKeywordNamedByThePath()
    {
        var result = FitsCardParser.Parse(PadCard("HIERARCH ESO INS FILT1 NAME = 'R       ' / filter name"));

        Assert.True(result.IsSuccess);

        Assert.Equal("ESO INS FILT1 NAME", result.Value.Name);

        Assert.Equal(FitsValueKind.String, result.Value.Value.Kind);

        Assert.Equal("R", result.Value.Value.AsString);

        Assert.Equal("filter name", result.Value.Comment);
    }

    [Fact]
    public void HeaderParse_MultipleHierarchCards_AreAllIndependentlyAddressable()
    {
        var block = BuildHeaderBlock(
            "HIERARCH ESO INS FILT1 NAME = 'R       ' / filter name",
            "HIERARCH ESO TEL AIRM START =                  1.2 / airmass",
            "END");

        var header = FitsHeader.Parse(block).Value;

        Assert.Equal("R", header.GetString("ESO INS FILT1 NAME").Value);

        Assert.Equal(1.2, header.GetReal("ESO TEL AIRM START").Value, precision: 6);
    }

    [Fact]
    public void HeaderParse_ContinuedLongString_ConcatenatesAcrossContinueCards()
    {
        var block = BuildHeaderBlock(
            "LONGSTRN= 'This is the first part of a &'  / first part",
            "CONTINUE  'long string value.'             / second part",
            "END");

        var header = FitsHeader.Parse(block).Value;

        Assert.Equal(1, header.Count(k => k.Name == "LONGSTRN"));

        Assert.Equal("This is the first part of a long string value.", header.GetString("LONGSTRN").Value);

        Assert.Equal("second part", header.First(k => k.Name == "LONGSTRN").Comment);
    }

    [Fact]
    public void HeaderParse_ContinuedLongStringAcrossMultipleContinueCards_ConcatenatesAllSegments()
    {
        var block = BuildHeaderBlock(
            "LONGSTRN= 'aaa&'",
            "CONTINUE  'bbb&'",
            "CONTINUE  'ccc'",
            "END");

        var header = FitsHeader.Parse(block).Value;

        Assert.Equal("aaabbbccc", header.GetString("LONGSTRN").Value);
    }

    [Fact]
    public void Parse_WrongCardLength_Fails()
    {
        var result = FitsCardParser.Parse("TOO SHORT");

        Assert.True(result.IsFailure);

        Assert.Equal("fits.header.invalid_card_length", result.Error.Code);
    }

    [Fact]
    public void HeaderParse_StopsAtEndCard_AndSupportsTypedLookups()
    {
        var block = BuildHeaderBlock(
            "SIMPLE  =                    T / conforms to FITS standard",
            "BITPIX  =                   16 / bits per pixel",
            "NAXIS   =                    2 / number of axes",
            "NAXIS1  =                  100",
            "NAXIS2  =                  200",
            "BSCALE  =                  2.0",
            "BZERO   =                 32768",
            "END");

        var result = FitsHeader.Parse(block);

        Assert.True(result.IsSuccess);

        var header = result.Value;

        Assert.Equal(16L, header.GetInteger("BITPIX").Value);

        Assert.Equal(2L, header.GetInteger("NAXIS").Value);

        Assert.Equal(100L, header.GetInteger("NAXIS1").Value);

        Assert.True(header.GetLogical("SIMPLE").Value);

        Assert.True(header.Get("MISSING").IsFailure);
    }

    [Fact]
    public void HeaderParse_MisalignedBlock_Fails()
    {
        var result = FitsHeader.Parse(new byte[79]);

        Assert.True(result.IsFailure);

        Assert.Equal("fits.header.misaligned_block", result.Error.Code);
    }

    [Fact]
    public void FitsImageDescriptor_FromHeader_ComputesShapeAndDataSize()
    {
        var block = BuildHeaderBlock(
            "SIMPLE  =                    T",
            "BITPIX  =                  -32",
            "NAXIS   =                    2",
            "NAXIS1  =                  100",
            "NAXIS2  =                   50",
            "END");

        var header = FitsHeader.Parse(block).Value;

        var descriptor = FitsImageDescriptor.FromHeader(header);

        Assert.True(descriptor.IsSuccess);

        Assert.Equal(BitPixType.Float32, descriptor.Value.BitPix);

        Assert.Equal(5000L, descriptor.Value.PixelCount);

        Assert.Equal(20000L, descriptor.Value.DataSizeBytes);

        Assert.Equal(0.0, descriptor.Value.BZero, precision: 6);

        Assert.Equal(1.0, descriptor.Value.BScale, precision: 6);
    }

    [Fact]
    public void FitsImageDescriptor_FromHeader_RejectsNaxisAboveStandardLimit()
    {
        var block = BuildHeaderBlock(
            "SIMPLE  =                    T",
            "BITPIX  =                    8",
            "NAXIS   =                 1000",
            "END");

        var header = FitsHeader.Parse(block).Value;

        var descriptor = FitsImageDescriptor.FromHeader(header);

        Assert.True(descriptor.IsFailure);

        Assert.Equal("fits.header.invalid_naxis", descriptor.Error.Code);
    }

    [Fact]
    public void FitsImageDescriptor_FromHeader_RejectsPixelCountOverflow()
    {
        var block = BuildHeaderBlock(
            "SIMPLE  =                    T",
            "BITPIX  =                    8",
            "NAXIS   =                    3",
            "NAXIS1  =           2000000000",
            "NAXIS2  =           2000000000",
            "NAXIS3  =           2000000000",
            "END");

        var header = FitsHeader.Parse(block).Value;

        var descriptor = FitsImageDescriptor.FromHeader(header);

        Assert.True(descriptor.IsFailure);

        Assert.Equal("fits.header.image_too_large", descriptor.Error.Code);
    }

    [Fact]
    public void FitsImageDescriptor_FromHeader_RejectsBitPixValueThatWrapsToAValidValue()
    {
        var block = BuildHeaderBlock(
            "SIMPLE  =                    T",
            "BITPIX  =           4294967304",
            "NAXIS   =                    0",
            "END");

        var header = FitsHeader.Parse(block).Value;

        var descriptor = FitsImageDescriptor.FromHeader(header);

        Assert.True(descriptor.IsFailure);

        Assert.Equal("fits.header.invalid_bitpix", descriptor.Error.Code);
    }

    [Fact]
    public void FitsImageDescriptor_FromHeader_RejectsWrongTypedBzero()
    {
        var block = BuildHeaderBlock(
            "SIMPLE  =                    T",
            "BITPIX  =                    8",
            "NAXIS   =                    0",
            "BZERO   = 'not-a-number'",
            "END");

        var header = FitsHeader.Parse(block).Value;

        var descriptor = FitsImageDescriptor.FromHeader(header);

        Assert.True(descriptor.IsFailure);

        Assert.Equal("fits.header.keyword_wrong_type", descriptor.Error.Code);
    }

    [Fact]
    public void HduDescriptor_FromHeader_ClassifiesPrimaryHdu()
    {
        var block = BuildHeaderBlock(
            "SIMPLE  =                    T",
            "BITPIX  =                    8",
            "NAXIS   =                    0",
            "END");

        var header = FitsHeader.Parse(block).Value;

        var hdu = HduDescriptor.FromHeader(0, header).Value;

        Assert.Equal(HduType.Primary, hdu.Type);

        Assert.NotNull(hdu.Image);

        Assert.Equal(0L, hdu.Image!.Value.PixelCount);
    }
}
