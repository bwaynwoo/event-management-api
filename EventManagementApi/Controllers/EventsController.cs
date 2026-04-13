using EventManagementApi.Models;
using EventManagementApi.Services;
using Microsoft.AspNetCore.Mvc;

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
    public ActionResult<List<Event>> GetEvents()
    {
        return Ok(_eventService.GetEvents());
    }

    [HttpGet("{id}")]
    public ActionResult<Event> GetEvent(int id)
    {
        var eventItem = _eventService.GetEvent(id);
        
        if (eventItem == null)
        {
            return NotFound(new { message = $"Event with id {id} not found" });
        }
        
        return Ok(eventItem);
    }

    [HttpPost]
    public ActionResult AddEvent([FromBody] Event eventItem)
    {
        if (string.IsNullOrWhiteSpace(eventItem.Title))
        {
            return BadRequest(new { message = "Title is required" });
        }
        
        if (eventItem.StartAt == default)
        {
            return BadRequest(new { message = "StartAt is required" });
        }
        
        if (eventItem.EndAt == default)
        {
            return BadRequest(new { message = "EndAt is required" });
        }
        
        if (eventItem.EndAt <= eventItem.StartAt)
        {
            return BadRequest(new { message = "EndAt must be later than StartAt" });
        }
        
        _eventService.AddEvent(eventItem);
        return CreatedAtAction(nameof(GetEvent), new { id = eventItem.Id }, eventItem);
    }

    [HttpPut("{id}")]
    public ActionResult ChangeEvent(int id, [FromBody] Event eventItem)
    {
        if (string.IsNullOrWhiteSpace(eventItem.Title))
        {
            return BadRequest(new { message = "Title is required" });
        }
        
        if (eventItem.StartAt == default)
        {
            return BadRequest(new { message = "StartAt is required" });
        }
        
        if (eventItem.EndAt == default)
        {
            return BadRequest(new { message = "EndAt is required" });
        }
        
        if (eventItem.EndAt <= eventItem.StartAt)
        {
            return BadRequest(new { message = "EndAt must be later than StartAt" });
        }
        
        var existingEvent = _eventService.GetEvent(id);
        if (existingEvent == null)
        {
            return NotFound(new { message = $"Event with id {id} not found" });
        }
        
        _eventService.ChangeEvent(id, eventItem);
        return Ok(_eventService.GetEvent(id));
    }

    [HttpDelete("{id}")]
    public ActionResult RemoveEvent(int id)
    {
        var existingEvent = _eventService.GetEvent(id);
        if (existingEvent == null)
        {
            return NotFound(new { message = $"Event with id {id} not found" });
        }
        
        _eventService.RemoveEvent(id);
        return NoContent();
    }
}