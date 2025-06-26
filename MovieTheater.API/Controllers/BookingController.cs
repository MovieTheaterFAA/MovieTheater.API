using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Domain.DTOs.BookingDTOs;
using MovieTheater.Infrastructure.Interfaces;

namespace MovieTheater.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BookingController : ControllerBase
{
    private readonly IBookingService _bookingService;
    private readonly IClaimsService _claimsService;
    private readonly ILoggerService _loggerService;
    private readonly IPaymentService _paymentService;

    public BookingController(IBookingService bookingService, IClaimsService claimsService, ILoggerService loggerService, IPaymentService paymentService)
    {
        _bookingService = bookingService;
        _claimsService = claimsService;
        _loggerService = loggerService;
        _paymentService = paymentService;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<BookingDto>> GetBooking(Guid id)
    {
        var booking = await _bookingService.GetBookingByIdAsync(id);

        if (booking == null)
            return NotFound();

        // Check if the user owns this booking or is an admin
        if (booking.UserId != _claimsService.GetCurrentUserId && !User.IsInRole("Admin"))
            return Forbid();

        return Ok(booking);
    }

    [HttpGet("user")]
    public async Task<ActionResult> GetUserBookings()
    {
        var userId = _claimsService.GetCurrentUserId;
        var bookings = await _bookingService.GetUserBookingsAsync(userId);
        return Ok(bookings);
    }

    [HttpPost]
    public async Task<ActionResult<BookingDto>> CreateBooking(CreateBookingRequest request)
    {
        try
        {
            var userId = _claimsService.GetCurrentUserId;
            var booking = await _bookingService.CreateBookingAsync(userId, request);
            return CreatedAtAction(nameof(GetBooking), new { id = booking.Id }, booking);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception)
        {
            return StatusCode(500, "An error occurred while processing your booking");
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> CancelBooking(Guid id)
    {
        var booking = await _bookingService.GetBookingByIdAsync(id);

        if (booking == null)
            return NotFound();

        // Check if the user owns this booking or is an admin
        if (booking.UserId != _claimsService.GetCurrentUserId && !User.IsInRole("Admin"))
            return Forbid();

        var result = await _bookingService.CancelBookingAsync(id);

        if (result)
            return NoContent();

        return BadRequest("Failed to cancel booking");
    }

    //[HttpPost("reserve")]
    //[Authorize]
    //public async Task<ActionResult<ReservationResult>> ReserveSeats(ReserveSeatRequest request)
    //{
    //    try
    //    {
    //        var result = await _bookingService.ReserveSeatsAsync(
    //            request.ShowTimeId,
    //            request.SeatIds,
    //            TimeSpan.FromMinutes(request.ReservationMinutes ?? 10));

    //        if (!result.Success)
    //            return BadRequest(new { message = "Some seats are unavailable", unavailableSeats = result.UnavailableSeats });

    //        return Ok(result);
    //    }
    //    catch (Exception ex)
    //    {
    //        _loggerService.Error($"Error reserving seats: {ex.Message}");
    //        return StatusCode(500, "An error occurred while reserving seats");
    //    }
    //}

    //[HttpPost("complete")]
    //[Authorize]
    //public async Task<ActionResult<BookingResult>> CompleteBooking([FromBody] CreateBookingRequest request)
    //{
    //    try
    //    {
    //        var userId = _claimsService.GetCurrentUserId;
    //        var bookingResult = await _bookingService.CreateBookingWithInvoiceAsync(userId, request);

    //        // Generate the payment URL
    //        var returnUrl = $"{Request.Scheme}://{Request.Host}/api/payment/success";
    //        var paymentUrl = await _paymentService.InitiatePaymentAsync(bookingResult.InvoiceId, returnUrl);

    //        return Ok(new
    //        {
    //            booking = bookingResult,
    //            paymentUrl = paymentUrl
    //        });
    //    }
    //    catch (InvalidOperationException ex)
    //    {
    //        return BadRequest(ex.Message);
    //    }
    //    catch (Exception ex)
    //    {
    //        _loggerService.Error($"Error completing booking: {ex.Message}");
    //        return StatusCode(500, "An error occurred while processing your booking");
    //    }
    //}
}
