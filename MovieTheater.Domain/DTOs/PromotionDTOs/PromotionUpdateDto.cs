namespace MovieTheater.Domain.DTOs.PromotionDTOs
{
    public class PromotionUpdateDto
    {
        public string? Title { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public decimal? DiscountValue { get; set; }
        public string? Detail { get; set; }
        public string? Image { get; set; }
    }
}
