namespace AssignmentSubmissionSystem.Application.Common.Exceptions;

/// <summary>
/// Base type for exceptions that carry an intended HTTP status code.
/// Caught by the API's global exception middleware and mapped to a consistent error envelope.
/// </summary>
public abstract class AppException : Exception
{
    protected AppException(string message, int statusCode) : base(message)
    {
        StatusCode = statusCode;
    }

    public int StatusCode { get; }
}

public sealed class UnauthorizedAppException(string message) : AppException(message, 401);

public sealed class ForbiddenAppException(string message) : AppException(message, 403);

public sealed class NotFoundAppException(string message) : AppException(message, 404);

public sealed class ConflictAppException(string message) : AppException(message, 409);
