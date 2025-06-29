using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Utils;
using MovieTheater.Domain.DTOs.SeatDTOs;
using MovieTheater.Infrastructure.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace MovieTheater.API.Controllers
{
    [ApiController]
    [Route("api/seat")]
    public class SeatController : ControllerBase
    {
        private readonly ISeatService _seatService;
        private readonly IClaimsService _claimsService;

        public SeatController(ISeatService seatService, IClaimsService claimsService)
        {
            _seatService = seatService;
            _claimsService = claimsService;
        }

        [HttpGet("cinema-room/{id}")]
        [SwaggerOperation(Summary = "Get all seats in a cinema room (for editing layout)")]
        public async Task<IActionResult> GetSeatsByCinemaRoom(Guid id)
        {
            try
            {
                var result = await _seatService.GetSeatsByCinemaRoomAsync(id);
                return Ok(ApiResult<List<SeatDto>>.Success(result, "200", "Fetched seats successfully"));
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<object>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }

        [HttpPost("batch/{id}")]
        [Authorize(Policy = "AdminPolicy")]
        [SwaggerOperation(Summary = "Create seats for a room")]
        public async Task<IActionResult> BatchCreateSeats(Guid id, [FromBody] BatchCreateSeatDto dto)
        {
            try
            {
                var adminId = _claimsService.GetCurrentUserId;
                var result = await _seatService.BatchCreateSeatsAsync(id, dto, adminId);
                return Ok(ApiResult<List<SeatDto>>.Success(result, "200", "Batch created seats successfully"));
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<object>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "AdminPolicy")]
        [SwaggerOperation(Summary = "Update seat info (type, position)")]
        public async Task<IActionResult> UpdateSeat(Guid id, [FromBody] UpdateSeatDto dto)
        {
            try
            {
                var adminId = _claimsService.GetCurrentUserId;
                var result = await _seatService.UpdateSeatAsync(id, dto, adminId);
                if (result == null)
                    return NotFound(ApiResult<object>.Failure("404", "Seat not found"));
                return Ok(ApiResult<SeatDto>.Success(result, "200", "Updated seat successfully"));
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<object>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminPolicy")]
        [SwaggerOperation(Summary = "Soft delete a seat")]
        public async Task<IActionResult> DeleteSeat(Guid id)
        {
            try
            {
                var adminId = _claimsService.GetCurrentUserId;
                var success = await _seatService.SoftDeleteSeatAsync(id, adminId);
                if (!success)
                    return NotFound(ApiResult<object>.Failure("404", "Seat not found"));
                return Ok(ApiResult<object>.Success(null, "200", "Deleted seat successfully"));
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<object>(ex);
                return StatusCode(statusCode, errorResponse);
            }
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


        [HttpGet("showtime/{id}")]
        [ProducesResponseType(typeof(List<ShowTimeSeatDto>), 200)]
        [ProducesResponseType(typeof(object), 404)]
        public async Task<IActionResult> GetSeatsByShowTimeAsync([FromRoute] Guid id)
        {
            try
            {
                var result = await _seatService.GetSeatsByShowTimeAsync(id);
                return Ok(ApiResult<List<ShowTimeSeatDto>>.Success(result, "200", "Fetched seats and status by showtime successfully"));
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
