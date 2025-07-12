using System.ComponentModel.DataAnnotations;

namespace MovieTheater.Domain.DTOs.TicketDTOs
{
    public class CreateOfflineTicketRequest
    {
        [Required]
        public string GuestPhoneNumber { get; set; }

        [Required]
        public Guid ShowtimeId { get; set; }

        [Required]
        public List<Guid> SeatIds { get; set; }

        public List<FoodItemRequest> FoodItems { get; set; } = new();
    }
}
