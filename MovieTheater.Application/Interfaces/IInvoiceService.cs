using MovieTheater.Domain.DTOs.InvoiceDTOs;
using MovieTheater.Infrastructure.Commons;

namespace MovieTheater.Application.Interfaces
{
    public interface IInvoiceService
    {
        Task<InvoiceDto> GetInvoiceByIdAsync(Guid id);
        Task<InvoiceDto> GetInvoiceByBookingIdAsync(Guid bookingId);
        Task<IEnumerable<InvoiceDto>> GetUserInvoicesAsync(Guid userId);
        Task<InvoiceDto> CreateInvoiceAsync(Guid bookingId);
        Task<InvoiceDto> UpdateInvoiceStatusAsync(Guid id, string status);
        Task<Pagination<InvoiceDto>> GetAllInvoicesAsync(int page = 1,
                                                         int pageSize = 10,
                                                         string? status = null,
                                                         string? sortBy = null,
                                                         bool isDescending = false,
                                                         string? search = null);
    }
}
