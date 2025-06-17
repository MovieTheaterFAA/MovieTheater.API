namespace MovieTheater.Domain.DTOs.PromotionDTOs
{
    public class PromotionResponseDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public decimal DiscountValue { get; set; }
        public string Detail { get; set; }
        public Guid EventId { get; set; }
    }
}