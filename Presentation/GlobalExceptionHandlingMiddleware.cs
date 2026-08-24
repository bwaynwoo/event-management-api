using Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Presentation;

internal sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);

        var (statusCode, title) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Not Found"),
            ValidationException => (StatusCodes.Status400BadRequest, "Validation Error"),
            NoAvailableSeatsException => (StatusCodes.Status409Conflict, "No Available Seats"),
            UnauthorizedException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            ForbiddenException  => (StatusCodes.Status403Forbidden, "Forbidden"),
            BookingLimitExceededException  => (StatusCodes.Status409Conflict, "Booking Limit Exceeded"),
            EventAlreadyStartedException  => (StatusCodes.Status400BadRequest, "Event already started"),
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error")
        };

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = exception.Message,
            Instance = context.Request.Path
        };
        
        if (exception is ValidationException validationEx)
        {
            problemDetails.Extensions["errors"] = validationEx.Errors.ToDictionary(
                k => k.Key,
                v => v.Value.ToArray());
        }

        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}