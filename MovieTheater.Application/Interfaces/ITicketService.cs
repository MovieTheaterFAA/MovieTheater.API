using MovieTheater.Domain.DTOs.TicketDTOs;
using MovieTheater.Domain.Enums;

namespace MovieTheater.Application.Interfaces
{
    public interface ITicketService
    {
        Task<TicketResponseDto> GenerateTicketFromBookingAsync(Guid bookingId);
        Task<Pagination<TicketResponseDto>> GetAllTicketsAsync(
    int page = 1,
    int pageSize = 10,
    TicketType? ticketType = null,
    string? sortBy = null,
    bool isDescending = false,
    string? search = null);
        Task<TicketResponseDto> GetTicketByIdAsync(Guid ticketId);
        Task<IEnumerable<TicketResponseDto>> GetUserTicketsAsync(Guid userId);
        Task<string> GenerateTicketQRCodeAsync(Guid ticketId);
        Task<TicketVerificationResultDto> VerifyTicketQRCodeAsync(QrCodePayload qrCodeData);
        Task<TicketResponseDto> CreateOfflineTicketAsync(CreateOfflineTicketRequest request);
    }
}
