using System.ComponentModel.DataAnnotations;

namespace MovieTheater.Domain.DTOs.ShowTimeDTOs
{
    public class ShowTimeRequestDto
    {
        [Required(ErrorMessage = "MovieId is required.")]
        public Guid MovieId { get; set; }

        [Required(ErrorMessage = "Cinema Room ID is required.")]
        public Guid CinemaRoomId { get; set; }

        [Required(ErrorMessage = "Show date is required.")]
        public DateTime ShowDate { get; set; }

    }
}
