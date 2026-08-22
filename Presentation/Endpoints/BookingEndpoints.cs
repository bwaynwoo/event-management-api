using System.Security.Claims;
using Application.DTOs;
using Application.Services;
using Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Endpoints;

internal static class BookingEndpoints
{
    internal static IEndpointRouteBuilder MapBookingEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/events/{id:guid}/book", async (
                Guid id,
                IBookingService bookingService,
                HttpContext httpContext,
                ClaimsPrincipal user,
                CancellationToken cancellationToken) =>
            {
                var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var booking = await bookingService.CreateBookingAsync(id, userId, cancellationToken);

                var location = $"/bookings/{booking.Id}";
                httpContext.Response.Headers.Location = location;

                return Results.Accepted(location, booking);
            })
            .WithName("CreateBooking")
            .RequireAuthorization()
            .Produces<BookingInfo>(StatusCodes.Status202Accepted)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        app.MapGet("/bookings/{id:guid}", async (
                Guid id,
                IBookingService bookingService,
                ClaimsPrincipal user,
                CancellationToken cancellationToken) =>
            {
                var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var booking = await bookingService.GetBookingByIdAsync(id, cancellationToken);

                if (booking.UserId != userId && !user.IsInRole(nameof(Role.Admin)))
                    return Results.Forbid();

                return Results.Ok(booking);
            })
            .WithName("GetBookingById")
            .RequireAuthorization()
            .Produces<BookingInfo>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        app.MapDelete("/bookings/{id:guid}", async (
                Guid id,
                IBookingService bookingService,
                ClaimsPrincipal user,
                CancellationToken cancellationToken) =>
            {
                var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var userRole = Enum.Parse<Role>(user.FindFirstValue(ClaimTypes.Role)!);

                await bookingService.CancelBookingAsync(id, userId, userRole, cancellationToken);

                return Results.NoContent();
            })
            .WithName("CancelBooking")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        return app;
    }
}