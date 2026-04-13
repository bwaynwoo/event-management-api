using EventManagementApi.Constants;
using Microsoft.AspNetCore.Mvc;
using EventManagementApi.DTOs;
using EventManagementApi.Mappings;
using EventManagementApi.Services;

namespace EventManagementApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;

    public EventsController(IEventService eventService)
    {
        _eventService = eventService;
    }

    [HttpGet]
    public ActionResult<IReadOnlyCollection<EventResponseDto>> GetEvents()
    {
        var events = _eventService.GetEvents();
        return Ok(events);
    }

    [HttpGet("{id}")]
    public ActionResult<EventResponseDto> GetEvent(int id)
    {
        var eventItem = _eventService.GetEvent(id);
        
        if (eventItem == null)
        {
            return NotFound(new { message = string.Format(ErrorMessages.NotFound, id) });
        }
        
        return Ok(EventMappings.ToResponseDto(eventItem));
    }

    [HttpPost]
    public ActionResult<EventResponseDto> AddEvent([FromBody] EventRequestDto request)
    {
        if (request.EndAt <= request.StartAt)
        {
            return BadRequest(new { message = ErrorMessages.EndAtMustBeLater });
        }
        
        var newEvent = EventMappings.ToEntity(request);
        _eventService.AddEvent(newEvent);
        
        return Created();
    }

    [HttpPut("{id}")]
    public ActionResult<EventResponseDto> ChangeEvent(int id, [FromBody] EventRequestDto request)
    {
        if (request.EndAt <= request.StartAt)
        {
            return BadRequest(new { message = ErrorMessages.EndAtMustBeLater });
        }
        
        var existingEvent = _eventService.GetEvent(id);
        if (existingEvent == null)
        {
            return NotFound(new { message = string.Format(ErrorMessages.NotFound, id) });
        }
        
        EventMappings.UpdateEntity(existingEvent, request);
        
        return Ok(EventMappings.ToResponseDto(existingEvent));
    }

    [HttpDelete("{id}")]
    public ActionResult RemoveEvent(int id)
    {
        var existingEvent = _eventService.GetEvent(id);
        if (existingEvent == null)
        {
            return NotFound(new { message = string.Format(ErrorMessages.NotFound, id) });
        }
        
        _eventService.RemoveEvent(id);
        return NoContent();
    }
}