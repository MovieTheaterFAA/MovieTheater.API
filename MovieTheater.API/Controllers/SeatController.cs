using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Utils;
using MovieTheater.Domain.DTOs.SeatDTOs;
using MovieTheater.Infrastructure.Interfaces;

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
        public async Task<IActionResult> HoldSeatsAsync([FromBody] HoldSeatsRequestDto request)
        {
            if (request == null || request.SeatIds == null || !request.SeatIds.Any())
                return BadRequest(new { Message = "Invalid request data." });

            try
            {
                var userId = _claimsService.GetCurrentUserId;

                var heldSeats = await _seatService.HoldSeatsAsync(userId, request.ShowTimeId, request.SeatIds);
                if (heldSeats.Any())
                {
                    return Ok(new
                    {
                        Message = "Seats held successfully.",
                        HeldSeats = heldSeats
                    });
                }
                else
                {
                    return Conflict(new { Message = "One or more seats could not be held. They may already be held or unavailable." });
                }
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<object>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }

        /// <summary>
        /// Get seat list and status by showtime
        /// </summary>
        [HttpGet("showtime/{showTimeId}/list")]
        [ProducesResponseType(typeof(List<ShowTimeSeatDto>), 200)]
        [ProducesResponseType(typeof(object), 404)]
        public async Task<IActionResult> GetSeatsByShowTimeAsync([FromRoute] Guid showTimeId)
        {
            try
            {
                var result = await _seatService.GetSeatsByShowTimeAsync(showTimeId);
                return Ok(result);
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
