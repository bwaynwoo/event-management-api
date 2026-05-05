using EventManagementApi.DTOs;
using EventManagementApi.Mappings;
using EventManagementApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventManagementApi.Controllers;

[ApiController]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public BookingsController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [HttpPost("events/{eventId}/book")]
    public async Task<ActionResult<BookingResponseDto>> CreateBooking(Guid eventId)
    {
        var booking = await _bookingService.CreateBookingAsync(eventId);

        if (booking == null)
        {
            return NotFound($"Event with id {eventId} was not found");
        }

        return CreatedAtAction(
            nameof(GetBooking),
            new { id = booking.Id },
            booking.ToResponse());
    }

    [HttpGet("bookings/{id}")]
    public async Task<ActionResult<BookingResponseDto>> GetBooking(Guid id)
    {
        var booking = await _bookingService.GetBookingByIdAsync(id);

        return booking != null ? Ok(booking.ToResponse()) : NotFound($"Booking with id {id} was not found");
    }
}