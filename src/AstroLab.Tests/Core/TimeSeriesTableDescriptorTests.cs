using System.Text;
using AstroLab.Core.Fits;

namespace AstroLab.Tests.Core;

public class TimeSeriesTableDescriptorTests
{
    private static string PadCard(string content) => content.PadRight(FitsCardParser.CardLength);

    private static byte[] BuildHeaderBlock(params string[] cards) =>
        Encoding.ASCII.GetBytes(string.Concat(Array.ConvertAll(cards, PadCard)));

    private static HduDescriptor BuildTableHdu(int rowCount, params string[] columnNames) =>
        BuildTableHduWithForms(rowCount, [.. columnNames.Select(name => (Name: name, Form: "1D"))]);

    private static HduDescriptor BuildTableHduWithForms(int rowCount, (string Name, string Form)[] columns)
    {
        var cards = new List<string>
        {
            "XTENSION= 'BINTABLE'",
            "BITPIX  =                    8",
            "NAXIS   =                    2",
            "NAXIS1  =                   16",
            $"NAXIS2  =           {rowCount,10}",
            "PCOUNT  =                    0",
            "GCOUNT  =                    1",
            $"TFIELDS =                    {columns.Length}",
        };

        for (var i = 0; i < columns.Length; i++)
        {
            cards.Add($"TTYPE{i + 1}  = '{columns[i].Name,-8}'");
            cards.Add($"TFORM{i + 1}  = '{columns[i].Form,-8}'");
        }

        cards.Add("END");

        var header = FitsHeader.Parse(BuildHeaderBlock([.. cards])).Value;

        return HduDescriptor.FromHeader(1, header).Value;
    }

    [Fact]
    public void Resolve_TableWithTimeAndFluxColumns_ReturnsColumnNumbersAndRowCount()
    {
        var hdu = BuildTableHdu(42, "TIME", "FLUX");

        var result = TimeSeriesTableDescriptor.Resolve(hdu);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value.RowCount);
        Assert.Equal(1, result.Value.TimeColumnNumber);
        Assert.Equal(2, result.Value.FluxColumnNumber);
    }

    [Fact]
    public void Resolve_ColumnsInReverseOrder_ResolvesColumnNumbersByPosition()
    {
        var hdu = BuildTableHdu(10, "FLUX", "TIME");

        var result = TimeSeriesTableDescriptor.Resolve(hdu);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.FluxColumnNumber);
        Assert.Equal(2, result.Value.TimeColumnNumber);
    }

    [Fact]
    public void Resolve_MissingFluxColumn_ReturnsValidationFailure()
    {
        var hdu = BuildTableHdu(10, "TIME", "RA");

        var result = TimeSeriesTableDescriptor.Resolve(hdu);

        Assert.True(result.IsFailure);
        Assert.Equal("fits.data.missing_column", result.Error.Code);
    }

    [Fact]
    public void Resolve_MissingTimeColumn_ReturnsValidationFailure()
    {
        var hdu = BuildTableHdu(10, "FLUX", "RA");

        var result = TimeSeriesTableDescriptor.Resolve(hdu);

        Assert.True(result.IsFailure);
        Assert.Equal("fits.data.missing_column", result.Error.Code);
    }

    [Fact]
    public void Resolve_ImageHdu_ReturnsNotATableFailure()
    {
        var header = FitsHeader.Parse(BuildHeaderBlock(
            "XTENSION= 'IMAGE   '",
            "BITPIX  =                    8",
            "NAXIS   =                    0",
            "END")).Value;

        var hdu = HduDescriptor.FromHeader(1, header).Value;

        var result = TimeSeriesTableDescriptor.Resolve(hdu);

        Assert.True(result.IsFailure);
        Assert.Equal("fits.data.not_a_table", result.Error.Code);
    }

    [Fact]
    public void Resolve_FluxColumnHasRepeatCountGreaterThanOne_ReturnsUnsupportedColumnShapeFailure()
    {
        var hdu = BuildTableHduWithForms(10, [("TIME", "1D"), ("FLUX", "3D")]);

        var result = TimeSeriesTableDescriptor.Resolve(hdu);

        Assert.True(result.IsFailure);
        Assert.Equal("fits.data.unsupported_column_shape", result.Error.Code);
    }

    [Fact]
    public void Resolve_TimeColumnFormHasNoExplicitRepeatCount_ResolvesAsScalar()
    {
        var hdu = BuildTableHduWithForms(10, [("TIME", "D"), ("FLUX", "1D")]);

        var result = TimeSeriesTableDescriptor.Resolve(hdu);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Resolve_ColumnFormIsCharacterString_ResolvesAsScalarRegardlessOfWidth()
    {
        var hdu = BuildTableHduWithForms(10, [("TIME", "20A"), ("FLUX", "1D")]);

        var result = TimeSeriesTableDescriptor.Resolve(hdu);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Resolve_FluxColumnIsVariableLengthArray_ReturnsUnsupportedColumnShapeFailure()
    {
        var hdu = BuildTableHduWithForms(10, [("TIME", "1D"), ("FLUX", "PD(100)")]);

        var result = TimeSeriesTableDescriptor.Resolve(hdu);

        Assert.True(result.IsFailure);
        Assert.Equal("fits.data.unsupported_column_shape", result.Error.Code);
    }

    [Fact]
    public void Resolve_MatchedColumnMissingTformKeyword_ReturnsValidationFailure()
    {
        var cards = new List<string>
        {
            "XTENSION= 'BINTABLE'",
            "BITPIX  =                    8",
            "NAXIS   =                    2",
            "NAXIS1  =                   16",
            "NAXIS2  =                   10",
            "PCOUNT  =                    0",
            "GCOUNT  =                    1",
            "TFIELDS =                    2",
            "TTYPE1  = 'TIME    '",
            "TFORM1  = '1D      '",
            "TTYPE2  = 'FLUX    '",
        };

        cards.Add("END");

        var header = FitsHeader.Parse(BuildHeaderBlock([.. cards])).Value;

        var hdu = HduDescriptor.FromHeader(1, header).Value;

        var result = TimeSeriesTableDescriptor.Resolve(hdu);

        Assert.True(result.IsFailure);
        Assert.Equal("fits.header.keyword_missing", result.Error.Code);
    }
}
