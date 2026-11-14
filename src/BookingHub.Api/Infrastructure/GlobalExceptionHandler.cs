using BookingHub.Application.Common;
using BookingHub.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;

namespace BookingHub.Api.Infrastructure;

/// <summary>
/// Turns application and domain exceptions into RFC 9457 problem details responses.
/// </summary>
internal sealed partial class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            BadHttpRequestException badRequest => (badRequest.StatusCode, "Bad request"),
            InvalidCredentialsException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            NotFoundException => (StatusCodes.Status404NotFound, "Not found"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict"),
            DomainException => (StatusCodes.Status422UnprocessableEntity, "Business rule violated"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred"),
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            LogUnhandledException(logger, exception);
        }

        httpContext.Response.StatusCode = status;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails =
            {
                Status = status,
                Title = title,
                // Never leak internals of unexpected errors to the client.
                Detail = status == StatusCodes.Status500InternalServerError ? null : exception.Message,
            },
        });
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception")]
    private static partial void LogUnhandledException(ILogger logger, Exception exception);
}
