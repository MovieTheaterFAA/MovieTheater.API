using MovieTheater.Domain.DTOs.InvoiceDTOs;
using MovieTheater.Domain.DTOs.PaymentDTOs;

namespace MovieTheater.Application.Interfaces
{
    public interface IInvoiceService
    {
        Task<InvoiceDto> GetInvoiceByIdAsync(Guid id);
        Task<InvoiceDto> GetInvoiceByBookingIdAsync(Guid bookingId);
        Task<IEnumerable<InvoiceDto>> GetUserInvoicesAsync(Guid userId);
        Task<InvoiceDto> CreateInvoiceAsync(Guid bookingId);
        Task<InvoiceDto> UpdateInvoiceStatusAsync(Guid id, string status);
        Task<PaymentDto> ProcessPaymentAsync(CreatePaymentRequest request);
    }
}
