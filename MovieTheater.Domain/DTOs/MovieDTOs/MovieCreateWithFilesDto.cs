using Microsoft.AspNetCore.Http;
using MovieTheater.Domain.DTOs.BlobDTOs;

namespace MovieTheater.Domain.DTOs.MovieDTOs
{
    public class MovieCreateWithFilesDto
    {
        public MovieRequestDto Movie { get; set; }
        public IFormFile Poster { get; set; }
        public IFormFile Background { get; set; }
        public List<MovieCastUploadDto> CastImages { get; set; } = new();
    }
}
