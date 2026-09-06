using Microsoft.AspNetCore.Diagnostics;

namespace AstroLab.Api;

/// <summary>
/// Maps request-boundary binding/validation failures to HTTP 400. This covers both
/// <see cref="ArgumentException"/> (thrown by ASP.NET Core's own model binding for malformed
/// request-bound input) and <see cref="BadHttpRequestException"/> (thrown by minimal API
/// parameter binding for a missing required parameter — but only when
/// <c>RouteHandlerOptions.ThrowOnBadRequest</c> is <c>true</c>, which <c>Program.cs</c> sets
/// explicitly so this failure mode is handled consistently regardless of hosting environment,
/// rather than depending on that option's environment-conditional default).
/// </summary>
public sealed class RequestValidationExceptionHandler : IExceptionHandler
{
    private const string InvalidRequestCode = "invalid_request";

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (message, statusCode) = exception switch
        {
            ArgumentException argumentException => (argumentException.Message, StatusCodes.Status400BadRequest),
            BadHttpRequestException badHttpRequestException => (badHttpRequestException.Message, badHttpRequestException.StatusCode),
            _ => ((string?)null, 0),
        };

        if (message is null)
        {
            return false;
        }

        await Results.Problem(title: InvalidRequestCode, detail: message, statusCode: statusCode)
            .ExecuteAsync(httpContext);

        return true;
    }
}
