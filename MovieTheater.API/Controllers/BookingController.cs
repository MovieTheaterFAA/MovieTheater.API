using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Utils;
using MovieTheater.Domain.DTOs.BookingDTOs;
using MovieTheater.Domain.Enums;
using MovieTheater.Infrastructure.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace MovieTheater.API.Controllers
{
    [ApiController]
    [Route("api/booking")]
    [Authorize]
    public class BookingController : ControllerBase
    {
        private readonly IBookingService _bookingService;
        private readonly IClaimsService _claimsService;

        public BookingController(
            IBookingService bookingService,
            IClaimsService claimsService)
        {
            _bookingService = bookingService;
            _claimsService = claimsService;
        }

        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "Get booking details by booking ID")]
        public async Task<IActionResult> GetBooking(Guid id)
        {
            try
            {
                var booking = await _bookingService.GetBookingByIdAsync(id);

                if (booking == null)
                    return NotFound(ApiResult<object>.Failure("404", "Booking not found"));

                return Ok(ApiResult<BookingResponseDto>.Success(booking, "200", "Fetched booking successfully"));
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<object>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }

        [HttpGet("user")]
        [SwaggerOperation(Summary = "Get all bookings for the current user")]
        public async Task<IActionResult> GetUserBookings()
        {
            try
            {
                var userId = _claimsService.GetCurrentUserId;
                var bookings = await _bookingService.GetUserBookingsAsync(userId);
                return Ok(ApiResult<IEnumerable<BookingResponseDto>>.Success(bookings, "200", "Fetched user bookings successfully"));
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<object>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }

        [HttpPost]
        [SwaggerOperation(Summary = "Create a new booking")]
        public async Task<IActionResult> CreateBooking([FromBody] CreateBookingRequest request)
        {
            try
            {
                var userId = _claimsService.GetCurrentUserId;
                var booking = await _bookingService.CreateBookingAsync(userId, request);
                return Ok(ApiResult<BookingDto>.Success(booking, "200", "Created booking successfully"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResult<object>.Failure("400", ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResult<object>.Failure("400", ex.Message));
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<object>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> CancelBooking(Guid id)
        {
            try
            {
                var booking = await _bookingService.GetBookingByIdAsync(id);

                if (booking == null)
                    return NotFound(ApiResult<object>.Failure("404", "Booking not found"));

                var result = await _bookingService.CancelBookingAsync(id);

                if (result)
                    return Ok(ApiResult<object>.Success(null!, "200", "Cancelled booking successfully"));

                return BadRequest(ApiResult<object>.Failure("400", "Failed to cancel booking"));
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<object>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(Summary = "Get all bookings (admin only)")]
        public async Task<IActionResult> GetAllBookings(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] BookingStatus? status = null,
            [FromQuery] string? sortBy = null,
            [FromQuery] bool isDescending = false,
            [FromQuery] string? search = null)
        {
            try
            {
                var bookings = await _bookingService.GetAllBookingsAsync(
                    page, pageSize, status, sortBy, isDescending, search);

                return Ok(ApiResult<Pagination<BookingResponseDto>>.Success(
                    bookings, "200", "Fetched bookings successfully"));
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<object>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }
    }
}