using System.Text.Json;
using AssignmentSubmissionSystem.Application.Common;
using AssignmentSubmissionSystem.Application.Common.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace AssignmentSubmissionSystem.Api.Middleware;

/// <summary>
/// Catches unhandled exceptions and maps them to a consistent ApiResponse error envelope.
/// Keeps client-facing messages safe; full exception details go to the server log only.
/// </summary>
public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (AppException ex)
        {
            logger.LogWarning(ex, "Handled application exception: {Message}", ex.Message);
            await WriteResponseAsync(context, ex.StatusCode, ex.Message);
        }
        catch (DbUpdateException ex)
        {
            // Typically a FK Restrict violation (e.g. deleting a subject that still has
            // assignments) or a unique-index violation (e.g. duplicate teacher assignment).
            logger.LogWarning(ex, "Database update conflict");
            await WriteResponseAsync(context, statusCode: 409, "This action conflicts with existing related data.");
        }
        catch (FluentValidation.ValidationException ex)
        {
            var message = string.Join(" ", ex.Errors.Select(e => e.ErrorMessage));
            logger.LogWarning(ex, "Validation failed: {Message}", message);
            await WriteResponseAsync(context, statusCode: 400, message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception processing {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteResponseAsync(context, statusCode: 500, "An unexpected error occurred. Please try again later.");
        }
    }

    private static async Task WriteResponseAsync(HttpContext context, int statusCode, string error)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var payload = ApiResponse<object>.Fail(error);
        await context.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonOptions));
    }
}
