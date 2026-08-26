namespace AstroLab.Core.Result;

public readonly record struct Error
{
    private Error(string code, string message, ErrorCategory category)
    {
        Code = code;
        Message = message;
        Category = category;
    }

    public string Code { get; }

    public string Message { get; }

    public ErrorCategory Category { get; }

    public static Error Validation(string code, string message) => Create(code, message, ErrorCategory.Validation);

    public static Error NotFound(string code, string message) => Create(code, message, ErrorCategory.NotFound);

    public static Error Conflict(string code, string message) => Create(code, message, ErrorCategory.Conflict);

    public static Error Unauthorized(string code, string message) => Create(code, message, ErrorCategory.Unauthorized);

    public static Error Infrastructure(string code, string message) => Create(code, message, ErrorCategory.Infrastructure);

    public static Error NotImplemented(string code, string message) => Create(code, message, ErrorCategory.NotImplemented);

    public static Error Cancelled(string code, string message) => Create(code, message, ErrorCategory.Cancelled);

    public static Error Unexpected(string code, string message) => Create(code, message, ErrorCategory.Unexpected);

    public override string ToString() => $"[{Category}:{Code}] {Message}";

    private static Error Create(string code, string message, ErrorCategory category)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        return new Error(code, message, category);
    }
}
