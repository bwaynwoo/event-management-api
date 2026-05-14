using EventManagementApi.DTOs;
using EventManagementApi.Mappings;
using EventManagementApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventManagementApi.Controllers;

[ApiController]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public BookingsController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [HttpPost("events/{eventId}/book")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<ActionResult<BookingResponseDto>> CreateBooking(Guid eventId)
    {
        var booking = await _bookingService.CreateBookingAsync(eventId);

        var locationUrl = Url.ActionLink(nameof(GetBooking), "Bookings", new { id = booking.Id });
        return Accepted(locationUrl, booking);
    }

    [HttpGet("bookings/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<BookingResponseDto>> GetBooking(Guid id)
    {
        var booking = await _bookingService.GetBookingByIdAsync(id);

        return Ok(booking.ToResponse());
    }
}