using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieTheater.Application.Interfaces;
using MovieTheater.Domain.DTOs.SeatDTOs;
using MovieTheater.Infrastructure.Interfaces;
using System.Net;

namespace MovieTheater.API.Controllers
{
    [ApiController]
    [Route("api/seats")]
    public class SeatController : ControllerBase
    {
        private readonly ISeatService _seatService;
        private readonly IClaimsService _claimsService;

        public SeatController(ISeatService seatService, IClaimsService claimsService)
        {
            _seatService = seatService;
            _claimsService = claimsService;
        }

        /// <summary>
        /// Hold seats for a showtime (temporary lock, not booking yet)
        /// </summary>
        [HttpPost("hold")]
        [Authorize]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 409)]
        public async Task<IActionResult> HoldSeats([FromBody] HoldSeatsRequestDto request)
        {
            if (request == null || request.SeatIds == null || !request.SeatIds.Any())
                return BadRequest(new { Message = "Invalid request data." });

            try
            {
                var userId = _claimsService.GetCurrentUserId;

                var success = await _seatService.HoldSeatsAsync(userId, request.ShowTimeId, request.SeatIds);
                if (success)
                {
                    return Ok(new { Message = "Seats held successfully." });
                }
                else
                {
                    // Business logic: return 409 Conflict if holding failed (seat taken or not available)
                    return Conflict(new { Message = "One or more seats could not be held. They may already be held or unavailable." });
                }
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { Message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new { Message = "An unexpected error occurred.", Detail = ex.Message });
            }
        }

        /// <summary>
        /// Get seat list and status by showtime
        /// </summary>
        [HttpGet("showtime/{showTimeId}/list")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(List<ShowTimeSeatDto>), 200)]
        [ProducesResponseType(typeof(object), 404)]
        public async Task<IActionResult> GetSeatsByShowTime([FromRoute] Guid showTimeId)
        {
            try
            {
                var result = await _seatService.GetSeatsByShowTimeAsync(showTimeId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new { Message = "An unexpected error occurred.", Detail = ex.Message });
            }
        }
    }
}
