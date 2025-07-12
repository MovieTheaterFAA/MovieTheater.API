namespace MovieTheater.Domain.DTOs.TicketDTOs;

public class TicketVerificationResultDto
{
    public bool IsValid { get; set; }
    public string Message { get; set; }
    public TicketResponseDto Ticket { get; set; }
}