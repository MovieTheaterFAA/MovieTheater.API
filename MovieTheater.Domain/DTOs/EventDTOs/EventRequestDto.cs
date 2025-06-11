using System.ComponentModel.DataAnnotations;

namespace MovieTheater.Domain.DTOs.EventDTOs
{
    public class EventRequestDto
    {

        [Required(ErrorMessage = "Name is required.")]
        [StringLength(200, ErrorMessage = "Name can't be longer than 200 characters.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Start time is required.")]
        public DateTime StartTime { get; set; }

        [Required(ErrorMessage = "End time is required.")]
        [DateGreaterThan("StartTime", ErrorMessage = "End time must be greater than start time.")]
        public DateTime EndTime { get; set; }

        [StringLength(1000, ErrorMessage = "Detail can't be longer than 1000 characters.")]
        public string Detail { get; set; }

        [Url(ErrorMessage = "Invalid URL format for Image.")]
        public string Image { get; set; }

    }

    public class DateGreaterThanAttribute : ValidationAttribute
    {
        private readonly string _startDateProperty;

        public DateGreaterThanAttribute(string startDateProperty)
        {
            _startDateProperty = startDateProperty;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var startDateProperty = validationContext.ObjectType.GetProperty(_startDateProperty);
            if (startDateProperty == null)
                return new ValidationResult($"Unknown property: {_startDateProperty}");

            var startDate = (DateTime?)startDateProperty.GetValue(validationContext.ObjectInstance);
            if (startDate == null)
                return new ValidationResult("Start time is required.");

            var endDate = (DateTime)value;

            if (endDate <= startDate)
                return new ValidationResult("End time must be greater than start time.");

            return ValidationResult.Success;
        }
    }
}
