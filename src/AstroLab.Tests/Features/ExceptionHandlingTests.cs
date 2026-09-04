using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AstroLab.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace AstroLab.Tests.Features;

/// <summary>
/// Covers the two <see cref="Microsoft.AspNetCore.Diagnostics.IExceptionHandler"/> implementations
/// registered in <c>Program.cs</c>. <see cref="RequestValidationExceptionHandler"/> is also
/// exercised end-to-end through the real API host, proving it is actually wired ahead of
/// <see cref="GlobalExceptionHandler"/> and reachable from a genuine validation failure
/// (<see cref="AstroLab.Api.Features.Images.Photometry.AperturePhotometryRequest.Validate"/>).
/// <see cref="GlobalExceptionHandler"/> is tested directly, since the application is deliberately
/// designed so that no reachable HTTP input triggers a genuinely unexpected exception.
/// </summary>
public sealed class ExceptionHandlingTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public ExceptionHandlingTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task InvalidPostBody_IsHandledByRequestValidationExceptionHandler_ReturnsBadRequest()
    {
        var fileId = await UploadAsync();

        var request = new
        {
            CenterX = 0.5,
            CenterY = 0.5,
            ApertureRadius = 0.0,
            AnnulusInnerRadius = 1.0,
            AnnulusOuterRadius = 1.8,
        };

        var response = await _client.PostAsJsonAsync($"/api/images/{fileId}/photometry/aperture", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("invalid_request", body.GetProperty("title").GetString());
    }

    private async Task<string> UploadAsync()
    {
        using var content = new ByteArrayContent(SyntheticFits.SmallGradientImage());

        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

        var response = await _client.PostAsync("/api/fits/upload", content);

        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        return body.GetProperty("fileId").GetString()!;
    }

    [Fact]
    public async Task RequestValidationExceptionHandler_ArgumentException_ReturnsBadRequestWithMessage()
    {
        var handler = new RequestValidationExceptionHandler();

        var argumentException = new ArgumentOutOfRangeException("apertureRadius", "Aperture radius must be positive.");

        var (handled, context) = await InvokeAsync(handler.TryHandleAsync, argumentException);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);

        var body = await ReadBodyAsync(context);

        Assert.Equal("invalid_request", body.GetProperty("title").GetString());
        Assert.Contains("Aperture radius must be positive", body.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task RequestValidationExceptionHandler_NonArgumentException_DoesNotHandleIt()
    {
        var handler = new RequestValidationExceptionHandler();

        var (handled, _) = await InvokeAsync(handler.TryHandleAsync, new InvalidOperationException("not a validation failure"));

        Assert.False(handled);
    }

    [Fact]
    public async Task GlobalExceptionHandler_UnexpectedException_ReturnsGenericProblemDetailsWithoutLeakingExceptionDetails()
    {
        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);

        var exception = new InvalidOperationException("connection string password=hunter2");

        var (handled, context) = await InvokeAsync(handler.TryHandleAsync, exception);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);

        var body = await ReadBodyAsync(context);

        Assert.Equal("unexpected_error", body.GetProperty("title").GetString());
        Assert.DoesNotContain("hunter2", body.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("InvalidOperationException", body.ToString(), StringComparison.Ordinal);
    }

    private static async Task<(bool Handled, DefaultHttpContext Context)> InvokeAsync(
        Func<HttpContext, Exception, CancellationToken, ValueTask<bool>> tryHandleAsync,
        Exception exception)
    {
        var requestServices = new ServiceCollection().AddLogging().AddProblemDetails().BuildServiceProvider();

        var httpContext = new DefaultHttpContext
        {
            RequestServices = requestServices,
            Response = { Body = new MemoryStream() },
        };

        var handled = await tryHandleAsync(httpContext, exception, CancellationToken.None);

        return (handled, httpContext);
    }

    private static async Task<JsonElement> ReadBodyAsync(DefaultHttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);

        return await JsonSerializer.DeserializeAsync<JsonElement>(context.Response.Body);
    }
}
