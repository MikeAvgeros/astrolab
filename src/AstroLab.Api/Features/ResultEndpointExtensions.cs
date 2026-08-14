using AstroLab.Core.Result;

namespace AstroLab.Api.Features;

/// <summary>
/// Translates <see cref="Result{TValue}"/> outcomes from Core/Infrastructure into HTTP responses
/// using C# pattern matching, so individual endpoints never need to know how an <see cref="ErrorCategory"/>
/// maps onto a status code.
/// </summary>
public static class ResultEndpointExtensions
{
    extension(Error error)
    {
        public IResult ToProblem() => error.Category switch
        {
            ErrorCategory.Validation => Results.Problem(detail: error.Message, statusCode: StatusCodes.Status400BadRequest, title: error.Code),
            ErrorCategory.NotFound => Results.Problem(detail: error.Message, statusCode: StatusCodes.Status404NotFound, title: error.Code),
            ErrorCategory.Conflict => Results.Problem(detail: error.Message, statusCode: StatusCodes.Status409Conflict, title: error.Code),
            ErrorCategory.Unauthorized => Results.Problem(detail: error.Message, statusCode: StatusCodes.Status401Unauthorized, title: error.Code),
            ErrorCategory.Cancelled => Results.Problem(detail: error.Message, statusCode: StatusCodes.Status408RequestTimeout, title: error.Code),
            ErrorCategory.Infrastructure => Results.Problem(detail: error.Message, statusCode: StatusCodes.Status502BadGateway, title: error.Code),
            ErrorCategory.Unexpected => Results.Problem(detail: error.Message, statusCode: StatusCodes.Status500InternalServerError, title: error.Code),
            _ => Results.Problem(detail: error.Message, statusCode: StatusCodes.Status500InternalServerError, title: error.Code),
        };
    }

    extension<TValue>(Result<TValue> result)
    {
        /// <summary>Maps a <see cref="Result{TValue}"/> onto an <see cref="IResult"/> via pattern matching: success is shaped by the caller, failure always becomes a problem response.</summary>
        public IResult ToApiResult(Func<TValue, IResult> onSuccess) =>
            result switch
            {
                { IsSuccess: true } success => onSuccess(success.Value),
                { IsSuccess: false } failure => failure.Error.ToProblem(),
            };
    }
}
