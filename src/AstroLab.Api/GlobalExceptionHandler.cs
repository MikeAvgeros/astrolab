using Microsoft.AspNetCore.Diagnostics;

namespace AstroLab.Api;

/// <summary>
/// Last-resort handler for exceptions that escape every endpoint's <c>Result&lt;T&gt;</c>-based
/// error handling (spec.md §5.1) — genuinely unexpected failures (a native interop crash, a
/// programmer error), never expected domain/validation failures, which are always represented as
/// a <c>Result&lt;T&gt;</c> and mapped to a response via <c>ResultEndpointExtensions</c> instead.
/// Logs the full exception server-side and returns a generic <see cref="ProblemDetails"/> body
/// that never leaks exception details or stack traces to the caller.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private const string UnexpectedErrorCode = "unexpected_error";

    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(
            exception, "Unhandled exception processing {Method} {Path}.", httpContext.Request.Method, httpContext.Request.Path);

        await Results.Problem(
                title: UnexpectedErrorCode,
                detail: "An unexpected error occurred while processing the request.",
                statusCode: StatusCodes.Status500InternalServerError)
            .ExecuteAsync(httpContext);

        return true;
    }
}
