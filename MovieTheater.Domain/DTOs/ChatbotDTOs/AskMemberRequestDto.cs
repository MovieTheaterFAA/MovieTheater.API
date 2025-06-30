namespace MovieTheater.Domain.DTOs.ChatbotDTOs
{
    public class AskMemberRequestDto
    {
        public string Prompt { get; set; }
        public string? GroupId { get; set; }
    }
}