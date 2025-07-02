namespace MovieTheater.Domain.DTOs.BookingDTOs
{
    public class BookingResponseDto
    {
        public Guid Id { get; set; }
        public String MemberName { get; set; }
        public String Movie { get; set; }
        public DateTime BookingDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }
        public List<BookingSeatDto> BookingSeats { get; set; } = new();
        public List<BookingFoodDto> BookingFoods { get; set; } = new();
    }
}
