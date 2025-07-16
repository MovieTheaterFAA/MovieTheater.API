using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Utils;
using MovieTheater.Domain.DTOs.InvoiceDTOs;
using MovieTheater.Infrastructure.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace MovieTheater.API.Controllers
{
    [ApiController]
    [Route("api/invoice")]
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
        [SwaggerOperation(Summary = "Get invoice details by invoice ID")]
        public async Task<IActionResult> GetInvoice(Guid id)
        {
            try
            {
                var invoice = await _invoiceService.GetInvoiceByIdAsync(id);
                if (invoice == null)
                    return NotFound(ApiResult<object>.Failure("404", "Invoice not found"));

                // Check if the user owns this invoice or is an admin
                //if (invoice.Booking.Id != _claimsService.GetCurrentUserId && !User.IsInRole("Admin"))
                //    return Forbid();

                return Ok(ApiResult<InvoiceDto>.Success(invoice, "200", "Fetched invoice successfully"));
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<object>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }

        [HttpGet("booking/{id}")]
        [SwaggerOperation(Summary = "Get invoice by booking ID")]
        public async Task<IActionResult> GetInvoiceByBooking(Guid id)
        {
            try
            {
                var invoice = await _invoiceService.GetInvoiceByBookingIdAsync(id);
                if (invoice == null)
                    return NotFound(ApiResult<object>.Failure("404", "Invoice not found"));

                // Check if the user owns this invoice or is an admin
                //if (invoice.Booking.Id != _claimsService.GetCurrentUserId && !User.IsInRole("Admin"))
                //    return Forbid();

                return Ok(ApiResult<InvoiceDto>.Success(invoice, "200", "Fetched invoice by booking successfully"));
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<object>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }

        [HttpGet("user")]
        [SwaggerOperation(Summary = "Get all invoices for the current user")]
        public async Task<IActionResult> GetUserInvoices()
        {
            try
            {
                var userId = _claimsService.GetCurrentUserId;
                var invoices = await _invoiceService.GetUserInvoicesAsync(userId);
                return Ok(ApiResult<IEnumerable<InvoiceDto>>.Success(invoices, "200", "Fetched user invoices successfully"));
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<object>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }

        [HttpPost("booking/{id}")]
        [SwaggerOperation(Summary = "Create an invoice for a booking")]
        public async Task<IActionResult> CreateInvoice(Guid id, [FromQuery] Guid? promotionId = null, [FromQuery] int? requestedPoints = null)
        {
            try
            {
                var invoice = await _invoiceService.CreateInvoiceAsync(id, promotionId, requestedPoints);
                return Ok(ApiResult<InvoiceDto>.Success(invoice, "200", "Created invoice successfully"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResult<object>.Failure("404", ex.Message));
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

        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(Summary = "Update invoice status (Admin only)")]
        public async Task<IActionResult> UpdateInvoiceStatus(Guid id, [FromBody] InvoiceStatusUpdateRequest request)
        {
            try
            {
                var invoice = await _invoiceService.UpdateInvoiceStatusAsync(id, request.Status);
                return Ok(ApiResult<InvoiceDto>.Success(invoice, "200", "Updated invoice status successfully"));
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

        [HttpGet("admin")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(Summary = "Get all invoices with pagination (Admin only)")]
        public async Task<IActionResult> GetAllInvoices(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool isDescending = false,
        [FromQuery] string? search = null)
        {
            try
            {
                var result = await _invoiceService.GetAllInvoicesAsync(page, pageSize, status, sortBy, isDescending, search);
                return Ok(ApiResult<Pagination<InvoiceDto>>.Success(result, "200", "Fetched all invoices successfully"));
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