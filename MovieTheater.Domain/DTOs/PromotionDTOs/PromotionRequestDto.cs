using System.ComponentModel.DataAnnotations;

namespace MovieTheater.Domain.DTOs.PromotionDTOs
{
    public class PromotionRequestDto
    {
        [Required(ErrorMessage = "Title is required.")]
        [StringLength(200, ErrorMessage = "Title can't be longer than 200 characters.")]
        public string Title { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Discount value must be greater than zero.")]
        public decimal DiscountValue { get; set; }

        [Required(ErrorMessage = "Detail is required.")]
        [StringLength(1000, ErrorMessage = "Detail can't be longer than 1000 characters.")]
        public string Detail { get; set; }

        [Url(ErrorMessage = "Invalid URL format for Image.")]
        public string Image { get; set; }

        public bool IsDeleted { get; set; } = false;

    }


}
