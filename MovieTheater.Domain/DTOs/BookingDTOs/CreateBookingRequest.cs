namespace MovieTheater.Domain.DTOs.BookingDTOs
{
    public class CreateBookingRequest
    {
        public Guid ShowTimeId { get; set; }
        public List<Guid> SeatIds { get; set; } = new();
        public List<FoodOrderItem> FoodItems { get; set; } = new();
    }
}
