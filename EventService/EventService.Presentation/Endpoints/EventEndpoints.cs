using EventService.Application.DTOs;
using EventService.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventService.Presentation.Endpoints;

internal static class EventEndpoints
{
    internal static IEndpointRouteBuilder MapEventEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/events");

        group.MapGet("/", async (
                IEventService eventService,
                CancellationToken cancellationToken,
                int page = 1,
                int pageSize = 10,
                DateTime? from = null,
                DateTime? to = null,
                string? title = null) =>
            {
                var events = await eventService.GetAllEventsAsync(page, pageSize, from, to, title, cancellationToken);
                return Results.Ok(events);
            })
            .WithName("GetAllEvents")
            .Produces<PaginatedResult<EventInfo>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        group.MapGet("/top", async (IEventService eventService) =>
            {
                var events = await eventService.GetTopEventsAsync();
                return Results.Ok(events);
            })
            .WithName("GetTopEvents")
            .Produces<IReadOnlyCollection<EventInfo>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        group.MapGet("/{id:guid}", async (Guid id, IEventService eventService, CancellationToken cancellationToken) =>
            {
                var @event = await eventService.GetEventByIdAsync(id, cancellationToken);
                return Results.Ok(@event);
            })
            .WithName("GetEventById")
            .Produces<EventInfo>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapPost("/",
                async (CreateEvent request, IEventService eventService, CancellationToken cancellationToken) =>
                {
                    var createdEvent = await eventService.CreateEventAsync(request, cancellationToken);
                    return Results.Created($"/events/{createdEvent.Id}", createdEvent);
                })
            .WithName("CreateEvent")
            .RequireAuthorization("Admin")
            .Produces<EventInfo>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapPut("/{id:guid}",
                async (Guid id, UpdateEvent request, IEventService eventService, CancellationToken cancellationToken) =>
                {
                    var updatedEvent = await eventService.UpdateEventAsync(id, request, cancellationToken);
                    return Results.Ok(updatedEvent);
                })
            .WithName("UpdateEvent")
            .RequireAuthorization("Admin")
            .Produces<EventInfo>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapDelete("/{id:guid}",
                async (Guid id, IEventService eventService, CancellationToken cancellationToken) =>
                {
                    await eventService.DeleteEventAsync(id, cancellationToken);
                    return Results.NoContent();
                })
            .WithName("DeleteEvent")
            .RequireAuthorization("Admin")
            .Produces<EventInfo>(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        return app;
    }
}