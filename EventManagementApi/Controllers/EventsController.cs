using Microsoft.AspNetCore.Mvc;
using EventManagementApi.DTOs;
using EventManagementApi.Mappings;
using EventManagementApi.Services;

namespace EventManagementApi.Controllers;

[ApiController]
[Route("events")]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;

    public EventsController(IEventService eventService)
    {
        _eventService = eventService;
    }

    [HttpGet]
    public ActionResult<IReadOnlyCollection<EventResponseDto>> GetEvents([FromQuery] GetEventsRequestDto request)
    {
        return Ok(_eventService.GetEvents(request));
    }

    [HttpGet("{id}")]
    public ActionResult<EventResponseDto> GetEvent(int id)
    {
        return Ok(EventMappings.ToResponseDto(_eventService.GetEvent(id)));
    }

    [HttpPost]
    public ActionResult<EventResponseDto> AddEvent([FromBody] EventRequestDto request)
    {
        var newEvent = EventMappings.ToEntity(request);
        _eventService.AddEvent(newEvent);

        return CreatedAtAction(
            nameof(GetEvent),
            new { id = newEvent.Id },
            EventMappings.ToResponseDto(newEvent));
    }

    [HttpPut("{id}")]
    public ActionResult<EventResponseDto> ChangeEvent(int id, [FromBody] EventRequestDto request)
    {
        _eventService.UpdateEvent(id, EventMappings.ToEntity(request));
        return Ok();
    }

    [HttpDelete("{id}")]
    public ActionResult RemoveEvent(int id)
    {
        _eventService.RemoveEvent(id);
        return NoContent();
    }
}