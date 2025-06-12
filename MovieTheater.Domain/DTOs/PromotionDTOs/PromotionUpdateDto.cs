namespace MovieTheater.Domain.DTOs.PromotionDTOs
{
    public class PromotionUpdateDto
    {
        public string? Title { get; set; }
        public decimal? DiscountValue { get; set; }
        public string? Detail { get; set; }
        public string? Image { get; set; }
        public Guid? EventId { get; set; }
    }
}
