using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;

namespace AstroLab.Api;

/// <summary>
/// Maps request-boundary binding/validation failures to HTTP 400. This covers
/// <see cref="ArgumentException"/> thrown while binding or validating request input — by ASP.NET
/// Core's own model binding, or by the API layer's request DTOs (<c>Create</c>/<c>Validate</c>) — and
/// <see cref="BadHttpRequestException"/> (thrown by minimal API parameter binding for a missing
/// required parameter — but only when <c>RouteHandlerOptions.ThrowOnBadRequest</c> is <c>true</c>,
/// which <c>Program.cs</c> sets explicitly so this failure mode is handled consistently regardless of
/// hosting environment, rather than depending on that option's environment-conditional default).
/// <para>
/// An <see cref="ArgumentException"/> raised below the API layer — a programmer-misuse guard in Core
/// or Infrastructure, or System.Text.Json refusing to write a non-finite number into the
/// response — is not a client error, so it is left to <see cref="GlobalExceptionHandler"/> (generic
/// 500, raw message not exposed) rather than being reported as the client's fault with an internal
/// message.
/// </para>
/// </summary>
public sealed class RequestValidationExceptionHandler : IExceptionHandler
{
    private const string InvalidRequestCode = "invalid_request";
    private const string AstroLabAssemblyPrefix = "AstroLab.";

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (message, statusCode) = exception switch
        {
            ArgumentException argumentException when IsRequestBoundaryFailure(argumentException) =>
                (argumentException.Message, StatusCodes.Status400BadRequest),
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

    private static bool IsRequestBoundaryFailure(ArgumentException exception)
    {
        foreach (var frame in new StackTrace(exception, fNeedFileInfo: false).GetFrames())
        {
            var declaringType = frame.GetMethod()?.DeclaringType;

            if (declaringType is null)
            {
                continue;
            }

            if (declaringType.Assembly == typeof(Utf8JsonWriter).Assembly)
            {
                return false;
            }

            if (declaringType.Assembly.GetName().Name?.StartsWith(AstroLabAssemblyPrefix, StringComparison.Ordinal) == true)
            {
                return declaringType.Assembly == typeof(RequestValidationExceptionHandler).Assembly;
            }
        }

        return true;
    }
}
