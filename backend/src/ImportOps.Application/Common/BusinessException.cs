namespace ImportOps.Application.Common;

public sealed class BusinessException : Exception
{
    public string Code { get; }
    public int StatusCode { get; }

    public BusinessException(string code, string message, int statusCode = 409)
        : base(message)
    {
        Code = code;
        StatusCode = statusCode;
    }
}

public static class ErrorCodes
{
    public const string Validation = "VALIDATION_ERROR";
    public const string NotFound = "NOT_FOUND";
    public const string Forbidden = "FORBIDDEN";
    public const string BusinessRule = "BUSINESS_RULE_VIOLATION";
    public const string ConcurrencyConflict = "CONCURRENCY_CONFLICT";
    public const string AuthenticationRequired = "AUTHENTICATION_REQUIRED";
    public const string InvalidIdentity = "INVALID_IDENTITY";
}
