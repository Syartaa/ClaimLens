namespace ClaimLens.Application.Common;

public class AppException : Exception
{
    public AppException(string code, int statusCode, string message)
        : base(message)
    {
        Code = code;
        StatusCode = statusCode;
    }

    public string Code { get; }

    public int StatusCode { get; }
}

public sealed class ValidationAppException : AppException
{
    public ValidationAppException(IReadOnlyList<FieldError> details)
        : base("VALIDATION_FAILED", 400, "The request is invalid.")
    {
        Details = details;
    }

    public IReadOnlyList<FieldError> Details { get; }
}

public sealed record FieldError(string Field, string Message);
