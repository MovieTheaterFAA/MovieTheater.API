namespace MovieTheater.Domain.DTOs.BookingDTOs
{
    public class BookingDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid ShowTimeId { get; set; }
        public DateTime BookingDate { get; set; }
        public decimal TotalAmount { get; set; }
        public List<BookingSeatDto> BookingSeats { get; set; } = new();
        public List<BookingFoodDto> BookingFoods { get; set; } = new();
    }
}
