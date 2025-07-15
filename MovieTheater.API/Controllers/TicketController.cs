using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Utils;
using MovieTheater.Domain.DTOs.TicketDTOs;
using MovieTheater.Domain.Enums;
using MovieTheater.Infrastructure.Commons;
using MovieTheater.Infrastructure.Interfaces;
using Swashbuckle.AspNetCore.Annotations;
using System.Text.Json;

namespace MovieTheater.API.Controllers
{
    [Route("api/ticket")]
    [ApiController]
    public class TicketController : ControllerBase
    {
        private readonly ITicketService _ticketService;
        private readonly IClaimsService _claimsService;

        public TicketController(ITicketService ticketService, IClaimsService claimsService)
        {
            _ticketService = ticketService;
            _claimsService = claimsService;
        }

        [HttpGet("admin")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(
            Summary = "Get all tickets (Admin)",
            Description = "Retrieve a paginated list of all tickets with optional search, type, and sorting. Admin only."
        )]
        [ProducesResponseType(typeof(ApiResult<Pagination<TicketResponseDto>>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 400)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> GetAllTicketsForAdmin(
            [FromQuery, SwaggerParameter("Page number, starting from 1")] int page = 1,
            [FromQuery, SwaggerParameter("Number of items per page")] int pageSize = 10,
            [FromQuery, SwaggerParameter("Filter by ticket type")] TicketType? ticketType = null,
            [FromQuery, SwaggerParameter("Sort by field: issuedAt, price (optional)")] string? sortBy = null,
            [FromQuery, SwaggerParameter("Sort in descending order? Default: false")] bool isDescending = false,
            [FromQuery, SwaggerParameter("Search by guest phone or movie name (optional)")] string? search = null)
        {
            try
            {
                if (page < 1 || pageSize < 1)
                    return BadRequest(ApiResult<object>.Failure("400", "Invalid pagination parameters."));

                var result = await _ticketService.GetAllTicketsAsync(page, pageSize, ticketType, sortBy, isDescending, search);
                return Ok(ApiResult<Pagination<TicketResponseDto>>.Success(result, "200", "Tickets retrieved successfully."));
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<object>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }

        [HttpGet("{ticketId}")]
        [Authorize]
        [SwaggerOperation(
            Summary = "Get ticket details",
            Description = "Retrieve detailed information for a specific ticket by its ID."
        )]
        [ProducesResponseType(typeof(ApiResult<TicketResponseDto>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 404)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> GetTicket([FromRoute] Guid ticketId)
        {
            try
            {
                var ticket = await _ticketService.GetTicketByIdAsync(ticketId);
                return Ok(ApiResult<TicketResponseDto>.Success(ticket, "200", "Ticket details retrieved successfully."));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResult<object>.Failure("404", ex.Message));
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<object>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }

        [HttpGet("me")]
        [Authorize]
        [SwaggerOperation(
            Summary = "Get all tickets for current user",
            Description = "Retrieve all tickets for the currently authenticated user."
        )]
        [ProducesResponseType(typeof(ApiResult<IEnumerable<TicketResponseDto>>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> GetUserTickets()
        {
            try
            {
                var userId = _claimsService.GetCurrentUserId;
                var tickets = await _ticketService.GetUserTicketsAsync(userId);
                return Ok(ApiResult<IEnumerable<TicketResponseDto>>.Success(tickets, "200", "User tickets retrieved successfully."));
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<object>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }

        [HttpPost("offline")]
        [Authorize(Roles = "Employee")]
        [SwaggerOperation(
            Summary = "Create offline ticket",
            Description = "Create a new offline ticket for a guest."
        )]
        [ProducesResponseType(typeof(ApiResult<TicketResponseDto>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 400)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> CreateOfflineTicket([FromBody] CreateOfflineTicketRequest request)
        {
            try
            {
                var result = await _ticketService.CreateOfflineTicketAsync(request);
                return Ok(ApiResult<TicketResponseDto>.Success(result, "200", "Offline ticket created successfully."));
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<object>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }

        [HttpGet("{id}/qrcode")]
        [Authorize]
        [SwaggerOperation(
            Summary = "Get ticket QR code",
            Description = "Generate a QR code for a specific ticket."
        )]
        [ProducesResponseType(typeof(ApiResult<string>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 400)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> GetTicketQrCode([FromRoute] Guid id)
        {
            try
            {
                var qr = await _ticketService.GenerateTicketQRCodeAsync(id);
                return Ok(ApiResult<string>.Success(qr, "200", "QR code generated successfully."));
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<object>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }

        [HttpPost("verify")]
        [Authorize(Roles = "Employee")]
        [SwaggerOperation(
            Summary = "Verify ticket QR code",
            Description = "Verify a ticket using QR code data."
        )]
        [ProducesResponseType(typeof(ApiResult<TicketVerificationResultDto>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 400)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> VerifyTicket([FromBody] QrCodePayload request)
        {
            try
            {
                var verificationResult = await _ticketService.VerifyTicketQRCodeAsync(request);
                return Ok(ApiResult<TicketVerificationResultDto>.Success(verificationResult, "200", "Ticket verified successfully."));
            }
            catch (JsonException)
            {
                return BadRequest(ApiResult<object>.Failure("400", "Invalid QR code format"));
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