using MovieTheater.Domain.DTOs.MovieDTOs;
using System.ComponentModel.DataAnnotations;

namespace MovieTheater.Domain.DTOs.PromotionDTOs
{
    public class PromotionRequestDto
    {

        [Required(ErrorMessage = "Title is required.")]
        [StringLength(255, ErrorMessage = "Title cannot be longer than 255 characters.")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Start time is required.")]
        public DateTime StartTime { get; set; }

        [Required(ErrorMessage = "End time is required.")]
        public DateTime EndTime { get; set; }

        [Required(ErrorMessage = "Discount value is required.")]
        [Range(0.01, 100, ErrorMessage = "Discount value must be between 0.01 and 100.")]
        public decimal DiscountValue { get; set; }

        [Required(ErrorMessage = "Detail is required.")]
        [StringLength(1000, ErrorMessage = "Detail cannot be longer than 1000 characters.")]
        public string Detail { get; set; }

        [Required(ErrorMessage = "Image URL is required.")]
        [Url(ErrorMessage = "Image must be a valid URL.")]
        public string Image { get; set; }

        // Custom validation to ensure that EndTime is after StartTime
        [CustomDateRange(ErrorMessage = "End time must be later than start time.")]
        public bool IsValidDateRange()
        {
            return StartTime < EndTime;
        }
    }

    public class CustomDateRangeAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var promotionRequestDto = (PromotionRequestDto)validationContext.ObjectInstance;

            if (promotionRequestDto.StartTime >= promotionRequestDto.EndTime)
            {
                return new ValidationResult("End time must be later than start time.");
            }

            return ValidationResult.Success;
        }
    }
}
