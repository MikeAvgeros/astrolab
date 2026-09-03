using Microsoft.AspNetCore.Diagnostics;

namespace AstroLab.Api;

public sealed class RequestValidationExceptionHandler : IExceptionHandler
{
    private const string InvalidRequestCode = "invalid_request";

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not ArgumentException argumentException)
        {
            return false;
        }

        await Results.Problem(
                title: InvalidRequestCode,
                detail: argumentException.Message,
                statusCode: StatusCodes.Status400BadRequest)
            .ExecuteAsync(httpContext);

        return true;
    }
}
