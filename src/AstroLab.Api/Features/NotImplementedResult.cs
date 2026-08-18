namespace AstroLab.Api.Features;

/// <summary>
/// The standard response for a roadmap feature slice: its request/response DTOs and route are
/// wired up and visible in OpenAPI, but the Core algorithm behind it does not exist yet (see
/// spec.md §4.1's roadmap note). Calling the route always returns this until that Core work lands.
/// </summary>
public static class NotImplementedResult
{
    public static IResult Value(string code, string message) =>
        Results.Problem(detail: message, statusCode: StatusCodes.Status501NotImplemented, title: code);
}
