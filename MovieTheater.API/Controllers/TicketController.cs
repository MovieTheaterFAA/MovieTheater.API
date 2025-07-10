using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieTheater.Application.Interfaces;
using MovieTheater.Domain.DTOs.TicketDTOs;
using MovieTheater.Infrastructure.Interfaces;
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

        /// <summary>
        /// Generates a ticket from an existing booking
        /// </summary>
        [HttpPost("generate/{bookingId}")]
        [Authorize]
        public async Task<ActionResult<TicketResponseDto>> GenerateTicket(Guid bookingId)
        {
            try
            {
                var ticket = await _ticketService.GenerateTicketFromBookingAsync(bookingId);
                return Ok(ticket);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while generating the ticket", error = ex.Message });
            }
        }

        /// <summary>
        /// Gets ticket details by ID
        /// </summary>
        [HttpGet("{ticketId}")]
        [Authorize]
        public async Task<ActionResult<TicketResponseDto>> GetTicket(Guid ticketId)
        {
            try
            {
                var ticket = await _ticketService.GetTicketByIdAsync(ticketId);
                return Ok(ticket);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving the ticket", error = ex.Message });
            }
        }

        /// <summary>
        /// Gets all tickets for a user
        /// </summary>
        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<TicketResponseDto>>> GetUserTickets()
        {
            try
            {
                var userId = _claimsService.GetCurrentUserId;
                var tickets = await _ticketService.GetUserTicketsAsync(userId);
                return Ok(tickets);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving user tickets", error = ex.Message });
            }
        }

        [HttpGet("{id}/qrcode")]
        public async Task<ActionResult<string>> GetTicketQrCode(Guid id)
        {
            return await _ticketService.GenerateTicketQRCodeAsync(id);
        }
        /// <summary>
        /// Verifies a ticket using QR code data
        /// </summary>
        [HttpPost("verify")]
        public async Task<ActionResult<TicketVerificationResultDto>> VerifyTicket([FromBody] QrCodePayload request)
        {
            try
            {

                var verificationResult = await _ticketService.VerifyTicketQRCodeAsync(request);
                return Ok(verificationResult);
            }
            catch (JsonException)
            {
                return BadRequest(new { message = "Invalid QR code format" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while verifying the ticket", error = ex.Message });
            }
        }
    }
}
