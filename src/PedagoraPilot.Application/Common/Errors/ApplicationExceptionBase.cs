namespace PedagoraPilot.Application.Common.Errors;
public abstract class ApplicationExceptionBase : Exception
{
    protected ApplicationExceptionBase(string errorKey, int statusCode) : base(errorKey)
    {
        ErrorKey = errorKey;
        StatusCode = statusCode;
    }

    public string ErrorKey { get; }
    public int StatusCode { get; }
    public IReadOnlyDictionary<string, object?> Parameters { get; init; } = new Dictionary<string, object?>();
}

public sealed class NotFoundApplicationException(string errorKey) : ApplicationExceptionBase(errorKey, 404);
public sealed class ConflictApplicationException(string errorKey) : ApplicationExceptionBase(errorKey, 409);
public sealed class ForbiddenApplicationException(string errorKey) : ApplicationExceptionBase(errorKey, 403);
public sealed class ValidationApplicationException(string errorKey) : ApplicationExceptionBase(errorKey, 400);
