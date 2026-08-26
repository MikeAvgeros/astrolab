namespace AstroLab.Api.Features;

public static class NotImplementedResult
{
    public static IResult Value(string code, string message) =>
        Results.Problem(detail: message, statusCode: StatusCodes.Status501NotImplemented, title: code);
}
