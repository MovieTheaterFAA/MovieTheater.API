using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieTheater.Application.Interfaces;
using MovieTheater.Domain.DTOs.InvoiceDTOs;
using MovieTheater.Domain.DTOs.PaymentDTOs;
using MovieTheater.Infrastructure.Interfaces;

namespace MovieTheater.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class InvoiceController : ControllerBase
    {
        private readonly IInvoiceService _invoiceService;
        private readonly IClaimsService _claimsService;

        public InvoiceController(IInvoiceService invoiceService, IClaimsService claimsService)
        {
            _invoiceService = invoiceService;
            _claimsService = claimsService;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<InvoiceDto>> GetInvoice(Guid id)
        {
            try
            {
                var invoice = await _invoiceService.GetInvoiceByIdAsync(id);
                if (invoice == null)
                    return NotFound();

                // Check if the user owns this invoice or is an admin
                if (invoice.Booking.Id != _claimsService.GetCurrentUserId && !User.IsInRole("Admin"))
                    return Forbid();

                return Ok(invoice);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("booking/{bookingId}")]
        public async Task<ActionResult<InvoiceDto>> GetInvoiceByBooking(Guid bookingId)
        {
            try
            {
                var invoice = await _invoiceService.GetInvoiceByBookingIdAsync(bookingId);
                if (invoice == null)
                    return NotFound();

                // Check if the user owns this invoice or is an admin
                if (invoice.Booking.Id != _claimsService.GetCurrentUserId && !User.IsInRole("Admin"))
                    return Forbid();

                return Ok(invoice);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("user")]
        public async Task<ActionResult<IEnumerable<InvoiceDto>>> GetUserInvoices()
        {
            try
            {
                var userId = _claimsService.GetCurrentUserId;
                var invoices = await _invoiceService.GetUserInvoicesAsync(userId);
                return Ok(invoices);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("booking/{bookingId}")]
        public async Task<ActionResult<InvoiceDto>> CreateInvoice(Guid bookingId)
        {
            try
            {
                var invoice = await _invoiceService.CreateInvoiceAsync(bookingId);
                return CreatedAtAction(nameof(GetInvoice), new { id = invoice.Id }, invoice);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<InvoiceDto>> UpdateInvoiceStatus(Guid id, [FromBody] InvoiceStatusUpdateRequest request)
        {
            try
            {
                var invoice = await _invoiceService.UpdateInvoiceStatusAsync(id, request.Status);
                return Ok(invoice);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("payment")]
        public async Task<ActionResult<PaymentDto>> ProcessPayment([FromBody] CreatePaymentRequest request)
        {
            try
            {
                var payment = await _invoiceService.ProcessPaymentAsync(request);
                return Ok(payment);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
