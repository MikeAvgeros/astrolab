using Microsoft.AspNetCore.Diagnostics;

namespace AstroLab.Api;

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
