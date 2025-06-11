using System.ComponentModel.DataAnnotations;

namespace MovieTheater.Domain.DTOs.MovieDTOs
{
    public class MovieRequestDTO
    {
        [Required(ErrorMessage = "Movie name is required.")]
        [StringLength(255, ErrorMessage = "Movie name cannot be longer than 255 characters.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Start date is required.")]
        public DateTime FromDate { get; set; }

        [Required(ErrorMessage = "End date is required.")]
        public DateTime ToDate { get; set; }

        [Required(ErrorMessage = "Actor list is required.")]
        [MinLength(1, ErrorMessage = "At least one actor is required.")]
        public List<string> Actors { get; set; }

        [Required(ErrorMessage = "Actors URL list is required.")]
        [MinLength(1, ErrorMessage = "At least one actor's URL is required.")]
        public List<string> ActorsUrl { get; set; }

        [Required(ErrorMessage = "Director is required.")]
        [StringLength(255, ErrorMessage = "Director's name cannot be longer than 255 characters.")]
        public string Director { get; set; }

        [Range(1, 300, ErrorMessage = "Running time must be between 1 and 300 minutes.")]
        public int? RunningTime { get; set; }

        [Url(ErrorMessage = "Trailer URL must be a valid URL.")]
        public string TrailerUrl { get; set; }

        [Required(ErrorMessage = "At least one genre is required.")]
        [MinLength(1, ErrorMessage = "At least one genre is required.")]
        public List<string> Genres { get; set; }

        [Required(ErrorMessage = "Description is required.")]
        [StringLength(1000, ErrorMessage = "Description cannot be longer than 1000 characters.")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Poster image URL is required.")]
        [Url(ErrorMessage = "Poster image must be a valid URL.")]
        public string PosterImage { get; set; }

        [Required(ErrorMessage = "Background image URL is required.")]
        [Url(ErrorMessage = "Background image must be a valid URL.")]
        public string BackgroundImage { get; set; }

        [Range(0, 10, ErrorMessage = "Rating must be between 0 and 10.")]
        public float Rating { get; set; }

        // Custom validation to ensure FromDate is not greater than ToDate
        [CustomDateRange(ErrorMessage = "ToDate cannot be earlier than FromDate.")]
        public bool IsValidDateRange()
        {
            return FromDate <= ToDate;
        }

    }

    public class CustomDateRangeAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var movieRequestDto = (MovieRequestDTO)validationContext.ObjectInstance;

            if (movieRequestDto.FromDate > movieRequestDto.ToDate)
            {
                return new ValidationResult("FromDate cannot be greater than ToDate.");
            }

            return ValidationResult.Success;
        }
    }


}
