namespace MovieTheater.Domain.DTOs.TicketDTOs
{
    public class TicketResponseDto
    {
        public Guid Id { get; set; }
        public Guid? BookingId { get; set; }
        public DateTime IssuedAt { get; set; }
        public string GuestPhoneNumber { get; set; }
        public decimal TotalPrice { get; set; }
        public string TicketType { get; set; }
        public string MovieName { get; set; }
        public string ShowTime { get; set; }
        public string CinemaRoom { get; set; }
        public string MoviePosterUrl { get; set; }
        public List<TicketSeatDto> Seats { get; set; } = new List<TicketSeatDto>();
        public List<TicketFoodDto> FoodItems { get; set; } = new List<TicketFoodDto>();
    }
}