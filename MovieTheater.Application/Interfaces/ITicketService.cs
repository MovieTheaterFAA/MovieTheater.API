using MovieTheater.Domain.DTOs.TicketDTOs;

namespace MovieTheater.Application.Interfaces
{
    public interface ITicketService
    {
        Task<TicketResponseDto> GenerateTicketFromBookingAsync(Guid bookingId);
        Task<TicketResponseDto> GetTicketByIdAsync(Guid ticketId);
        Task<IEnumerable<TicketResponseDto>> GetUserTicketsAsync(Guid userId);
        Task<string> GenerateTicketQRCodeAsync(Guid ticketId);
        Task<TicketVerificationResultDto> VerifyTicketQRCodeAsync(QrCodePayload qrCodeData);
    }
}
