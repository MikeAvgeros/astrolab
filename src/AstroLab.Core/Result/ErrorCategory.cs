namespace AstroLab.Core.Result;

/// <summary>
/// Broad classification of a domain failure, used by the API layer to translate
/// a failed <see cref="Result{TValue}"/> into an appropriate HTTP response without
/// the Core project ever depending on ASP.NET Core.
/// </summary>
public enum ErrorCategory
{
    /// <summary>The request or input data failed validation.</summary>
    Validation,

    /// <summary>The requested resource does not exist.</summary>
    NotFound,

    /// <summary>The request conflicts with the current state of a resource.</summary>
    Conflict,

    /// <summary>The caller is not authorized to perform the operation.</summary>
    Unauthorized,

    /// <summary>An upstream or infrastructure dependency failed (disk, network, native interop).</summary>
    Infrastructure,

    /// <summary>The requested operation is a known, real capability that has not been implemented yet.</summary>
    NotImplemented,

    /// <summary>The operation was cancelled before it could complete.</summary>
    Cancelled,

    /// <summary>An unclassified or unexpected failure occurred.</summary>
    Unexpected,
}
