using System.Net;
using System.Text;
using AstroLab.Infrastructure.Catalogues;
using Microsoft.Extensions.Logging.Abstractions;

namespace AstroLab.Tests.Infrastructure;

public class VizierTapClientTests
{
    private const string GaiaSchemaVoTable = """
        <?xml version="1.0"?>
        <VOTABLE version="1.4" xmlns="http://www.ivoa.net/xml/VOTable/v1.4">
          <RESOURCE type="results">
            <TABLE>
              <FIELD name="column_name" datatype="char" arraysize="*"/>
              <FIELD name="ucd" datatype="char" arraysize="*"/>
              <DATA>
                <TABLEDATA>
                  <TR><TD>RA_ICRS</TD><TD>pos.eq.ra;meta.main</TD></TR>
                  <TR><TD>DE_ICRS</TD><TD>pos.eq.dec;meta.main</TD></TR>
                  <TR><TD>Source</TD><TD>meta.id;meta.main</TD></TR>
                  <TR><TD>Gmag</TD><TD>phot.mag;meta.main;em.opt</TD></TR>
                </TABLEDATA>
              </DATA>
            </TABLE>
          </RESOURCE>
        </VOTABLE>
        """;

    private const string SchemaWithoutMagnitudeVoTable = """
        <?xml version="1.0"?>
        <VOTABLE version="1.4" xmlns="http://www.ivoa.net/xml/VOTable/v1.4">
          <RESOURCE type="results">
            <TABLE>
              <FIELD name="column_name" datatype="char" arraysize="*"/>
              <FIELD name="ucd" datatype="char" arraysize="*"/>
              <DATA>
                <TABLEDATA>
                  <TR><TD>RAJ2000</TD><TD>pos.eq.ra;meta.main</TD></TR>
                  <TR><TD>DEJ2000</TD><TD>pos.eq.dec;meta.main</TD></TR>
                  <TR><TD>recno</TD><TD>meta.record</TD></TR>
                </TABLEDATA>
              </DATA>
            </TABLE>
          </RESOURCE>
        </VOTABLE>
        """;

    private const string SchemaWithoutPositionColumnsVoTable = """
        <?xml version="1.0"?>
        <VOTABLE version="1.4" xmlns="http://www.ivoa.net/xml/VOTable/v1.4">
          <RESOURCE type="results">
            <TABLE>
              <FIELD name="column_name" datatype="char" arraysize="*"/>
              <FIELD name="ucd" datatype="char" arraysize="*"/>
              <DATA>
                <TABLEDATA>
                  <TR><TD>Name</TD><TD>meta.id;meta.main</TD></TR>
                </TABLEDATA>
              </DATA>
            </TABLE>
          </RESOURCE>
        </VOTABLE>
        """;

    private const string EmptySchemaVoTable = """
        <?xml version="1.0"?>
        <VOTABLE version="1.4" xmlns="http://www.ivoa.net/xml/VOTable/v1.4">
          <RESOURCE type="results">
            <TABLE>
              <FIELD name="column_name" datatype="char" arraysize="*"/>
              <FIELD name="ucd" datatype="char" arraysize="*"/>
              <DATA><TABLEDATA/></DATA>
            </TABLE>
          </RESOURCE>
        </VOTABLE>
        """;

    private const string GaiaConeSearchVoTable = """
        <?xml version="1.0"?>
        <VOTABLE version="1.4" xmlns="http://www.ivoa.net/xml/VOTable/v1.4">
          <RESOURCE type="results">
            <TABLE>
              <FIELD name="catalogue_id" datatype="char" arraysize="*"/>
              <FIELD name="ra" datatype="double"/>
              <FIELD name="dec" datatype="double"/>
              <FIELD name="mag" datatype="double"/>
              <DATA>
                <TABLEDATA>
                  <TR><TD>Gaia DR3 123456</TD><TD>180.001</TD><TD>0.002</TD><TD>15.4</TD></TR>
                </TABLEDATA>
              </DATA>
            </TABLE>
          </RESOURCE>
        </VOTABLE>
        """;

    private const string ConeSearchWithNullMagnitudeVoTable = """
        <?xml version="1.0"?>
        <VOTABLE version="1.4" xmlns="http://www.ivoa.net/xml/VOTable/v1.4">
          <RESOURCE type="results">
            <TABLE>
              <FIELD name="catalogue_id" datatype="char" arraysize="*"/>
              <FIELD name="ra" datatype="double"/>
              <FIELD name="dec" datatype="double"/>
              <FIELD name="mag" datatype="double"/>
              <DATA>
                <TABLEDATA>
                  <TR><TD>12345</TD><TD>180.001</TD><TD>0.002</TD><TD/></TR>
                </TABLEDATA>
              </DATA>
            </TABLE>
          </RESOURCE>
        </VOTABLE>
        """;

    private const string QueryErrorVoTable = """
        <?xml version="1.0"?>
        <VOTABLE version="1.4" xmlns="http://www.ivoa.net/xml/VOTable/v1.4">
          <RESOURCE type="results">
            <INFO name="QUERY_STATUS" value="ERROR">Unknown table "not/a/catalogue"</INFO>
          </RESOURCE>
        </VOTABLE>
        """;

    private static (VizierTapClient Client, StubHttpMessageHandler Handler) CreateClient(
        Func<HttpRequestMessage, Task<HttpResponseMessage>> responder)
    {
        var handler = new StubHttpMessageHandler(responder);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://vizier.test/tap/") };
        var client = new VizierTapClient(httpClient, NullLogger<VizierTapClient>.Instance);
        return (client, handler);
    }

    private static (VizierTapClient Client, StubHttpMessageHandler Handler) CreateSequencedClient(params string[] xmlResponsesInOrder)
    {
        var callIndex = 0;

        return CreateClient(_ =>
        {
            var index = Math.Min(callIndex, xmlResponsesInOrder.Length - 1);
            callIndex++;
            return Task.FromResult(XmlResponse(xmlResponsesInOrder[index]));
        });
    }

    private static HttpResponseMessage XmlResponse(string xml) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(xml, Encoding.UTF8, "application/x-votable+xml")
    };

    [Fact]
    public async Task ConeSearchAsync_ResolvesColumnsByUcd_AndMapsRecords()
    {
        var (client, handler) = CreateSequencedClient(GaiaSchemaVoTable, GaiaConeSearchVoTable);

        var query = CatalogueConeSearchQuery.Create("I/355/gaiadr3", rightAscension: 180.0, declination: 0.0, radiusArcsec: 5.0);

        var result = await client.ConeSearchAsync(query);

        Assert.True(result.IsSuccess);

        var record = Assert.Single(result.Value);

        Assert.Equal("Gaia DR3 123456", record.Identifier);
        Assert.Equal(180.001, record.RightAscension);
        Assert.Equal(0.002, record.Declination);
        Assert.Equal(15.4, record.Magnitude);

        Assert.Equal(2, handler.Requests.Count);

        var schemaRequestBody = await handler.Requests[0].Content!.ReadAsStringAsync();
        var decodedSchemaBody = Uri.UnescapeDataString(schemaRequestBody.Replace('+', ' '));
        Assert.Contains("TAP_SCHEMA.columns", decodedSchemaBody);
        Assert.Contains("table_name='I/355/gaiadr3'", decodedSchemaBody);

        var coneSearchBody = await handler.Requests[1].Content!.ReadAsStringAsync();
        var decodedConeSearchBody = Uri.UnescapeDataString(coneSearchBody.Replace('+', ' '));
        Assert.Contains("\"RA_ICRS\"", decodedConeSearchBody);
        Assert.Contains("\"DE_ICRS\"", decodedConeSearchBody);
        Assert.Contains("\"Source\" AS catalogue_id", decodedConeSearchBody);
        Assert.Contains("\"Gmag\" AS mag", decodedConeSearchBody);
        Assert.Contains("CONTAINS(POINT('ICRS'", decodedConeSearchBody);
        Assert.Contains("CIRCLE('ICRS',180,0,", decodedConeSearchBody);
    }

    [Fact]
    public async Task ConeSearchAsync_NoMagnitudeColumnInSchema_SelectsNullMagnitude()
    {
        var (client, handler) = CreateSequencedClient(SchemaWithoutMagnitudeVoTable, ConeSearchWithNullMagnitudeVoTable);

        var query = CatalogueConeSearchQuery.Create("II/246/out", rightAscension: 180.0, declination: 0.0, radiusArcsec: 5.0);

        var result = await client.ConeSearchAsync(query);

        Assert.True(result.IsSuccess);

        var record = Assert.Single(result.Value);

        Assert.Null(record.Magnitude);

        var coneSearchBody = await handler.Requests[1].Content!.ReadAsStringAsync();
        var decoded = Uri.UnescapeDataString(coneSearchBody.Replace('+', ' '));
        Assert.Contains("NULL AS mag", decoded);
        Assert.Contains("\"recno\" AS catalogue_id", decoded);
    }

    [Fact]
    public async Task ConeSearchAsync_UnknownCatalogue_ReturnsNotFoundError()
    {
        var (client, _) = CreateSequencedClient(EmptySchemaVoTable);

        var query = CatalogueConeSearchQuery.Create("not/a/catalogue", rightAscension: 0.0, declination: 0.0, radiusArcsec: 1.0);

        var result = await client.ConeSearchAsync(query);

        Assert.True(result.IsFailure);
        Assert.Equal("catalogues.vizier.unknown_catalogue", result.Error.Code);
    }

    [Fact]
    public async Task ConeSearchAsync_SchemaMissingPositionColumns_ReturnsNotFoundError()
    {
        var (client, _) = CreateSequencedClient(SchemaWithoutPositionColumnsVoTable);

        var query = CatalogueConeSearchQuery.Create("some/catalogue", rightAscension: 0.0, declination: 0.0, radiusArcsec: 1.0);

        var result = await client.ConeSearchAsync(query);

        Assert.True(result.IsFailure);
        Assert.Equal("catalogues.vizier.unresolved_position_columns", result.Error.Code);
    }

    [Fact]
    public async Task ConeSearchAsync_TapQueryStatusError_ReturnsInfrastructureError()
    {
        var (client, _) = CreateSequencedClient(QueryErrorVoTable);

        var query = CatalogueConeSearchQuery.Create("not/a/catalogue", rightAscension: 0.0, declination: 0.0, radiusArcsec: 1.0);

        var result = await client.ConeSearchAsync(query);

        Assert.True(result.IsFailure);
        Assert.Equal("catalogues.vizier.query_error", result.Error.Code);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, "catalogues.vizier.invalid_request")]
    [InlineData(HttpStatusCode.NotFound, "catalogues.vizier.not_found")]
    [InlineData(HttpStatusCode.TooManyRequests, "catalogues.vizier.rate_limited")]
    [InlineData(HttpStatusCode.InternalServerError, "catalogues.vizier.upstream_error")]
    public async Task ConeSearchAsync_MapsHttpStatusToDistinctErrorCodes(HttpStatusCode statusCode, string expectedCode)
    {
        var (client, _) = CreateClient(_ => Task.FromResult(new HttpResponseMessage(statusCode)
        {
            Content = new StringContent("boom")
        }));

        var query = CatalogueConeSearchQuery.Create("I/355/gaiadr3", rightAscension: 0.0, declination: 0.0, radiusArcsec: 1.0);

        var result = await client.ConeSearchAsync(query);

        Assert.True(result.IsFailure);
        Assert.Equal(expectedCode, result.Error.Code);
    }
}
