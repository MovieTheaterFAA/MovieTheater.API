using MovieTheater.Domain.Enums;

namespace MovieTheater.Domain.DTOs.FoodAndDrinkDTOs
{
    public class FoodAndDrinkResponseDTO
    {

        public Guid Id { get; set; }

        public string Name { get; set; }

        public string? Description { get; set; }

        public decimal Price { get; set; }

        public FoodType Type { get; set; }

        public string? ImageUrl { get; set; }

        public bool IsAvailable { get; set; }

        public DateTime CreatedAt { get; set; }

        public Guid CreatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public Guid? UpdatedBy { get; set; }

        public DateTime? DeletedAt { get; set; }

        public Guid? DeletedBy { get; set; }
    }
}
