using EventManagementApi.DTOs;
using EventManagementApi.Models;

namespace EventManagementApi.Mappings;

public static class EventMappings
{
    public static Event ToEntity(EventRequestDto dto)
    {
        return new Event
        {
            Title = dto.Title,
            Description = dto.Description,
            StartAt = dto.StartAt,
            EndAt = dto.EndAt
        };
    }
    
    public static EventResponseDto ToResponseDto(Event entity)
    {
        return new EventResponseDto(
            entity.Id,
            entity.Title,
            entity.Description,
            entity.StartAt,
            entity.EndAt
        );
    }
    
    public static void UpdateEntity(Event existing, EventRequestDto dto)
    {
        existing.Title = dto.Title;
        existing.Description = dto.Description;
        existing.StartAt = dto.StartAt;
        existing.EndAt = dto.EndAt;
    }
}