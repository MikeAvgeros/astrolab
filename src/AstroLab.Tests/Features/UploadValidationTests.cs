using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace AstroLab.Tests.Features;

/// <summary>
/// Upload must reject bodies that are not valid FITS and must not leave them in staging storage.
/// Uses its own <see cref="ApiFactory"/> so the staging directory contents can be asserted exactly.
/// </summary>
public class UploadValidationTests : IClassFixture<ApiFactory>
{
    private const int CardLength = 80;
    private const int BlockSize = 2880;

    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public UploadValidationTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public static TheoryData<string, byte[], string> InvalidUploads => new()
    {
        { "plain text", Encoding.ASCII.GetBytes("this is not a FITS file\n"), "fits.header.truncated_file" },
        { "empty body", [], "fits.header.empty_file" },
        {
            "header without SIMPLE",
            BuildFits(["BITPIX  =                    8", "NAXIS   =                    2", "NAXIS1  =                    4", "NAXIS2  =                    2", "END"]),
            "fits.header.missing_simple"
        },
        {
            "SIMPLE = F",
            BuildFits(["SIMPLE  =                    F", "BITPIX  =                    8", "NAXIS   =                    2", "NAXIS1  =                    4", "NAXIS2  =                    2", "END"]),
            "fits.header.nonconforming"
        },
        {
            "data unit shorter than the header declares",
            SyntheticFits.SmallGradientImage()[..(BlockSize + 4)],
            "fits.header.invalid_data_size"
        },
    };

    [Theory]
    [MemberData(nameof(InvalidUploads))]
    public async Task Upload_InvalidFits_ReturnsBadRequestAndDiscardsStagedFile(string description, byte[] body, string expectedCode)
    {
        var response = await UploadAsync(body);

        Assert.True(HttpStatusCode.BadRequest == response.StatusCode, $"{description}: expected 400, got {(int)response.StatusCode}.");

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(expectedCode, problem.GetProperty("title").GetString());

        Assert.Empty(StagedFiles());
    }

    [Fact]
    public async Task Upload_ValidFits_IsStoredUnchanged()
    {
        var body = SyntheticFits.SmallGradientImage();

        var response = await UploadAsync(body);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var fileId = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("fileId").GetString()!;

        Assert.Equal(body, await File.ReadAllBytesAsync(Path.Combine(_factory.StorageRoot, fileId)));

        File.Delete(Path.Combine(_factory.StorageRoot, fileId));
    }

    private async Task<HttpResponseMessage> UploadAsync(byte[] body)
    {
        using var content = new ByteArrayContent(body);

        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

        return await _client.PostAsync("/api/fits/upload", content);
    }

    private string[] StagedFiles() =>
        Directory.Exists(_factory.StorageRoot) ? Directory.GetFiles(_factory.StorageRoot, "*", SearchOption.AllDirectories) : [];

    private static byte[] BuildFits(string[] cards)
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

        var data = new byte[BlockSize];

        return [.. Encoding.ASCII.GetBytes(header.ToString()), .. data];
    }
}
