using Microsoft.AspNetCore.Http;
using MovieTheater.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MovieTheater.Domain.DTOs.MovieDTOs
{
    public class MovieWithImagesAndCastsRequestDto
    {
        public string Name { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string Director { get; set; }
        public int? RunningTime { get; set; }
        public string TrailerUrl { get; set; }
        public List<string> Genres { get; set; }
        public string Description { get; set; }
        public MovieStatus Status { get; set; }
        public float Rating { get; set; }
        public IFormFile? PosterImageFile { get; set; }
        public IFormFile? BackgroundImageFile { get; set; }
        public string[] ActorNames { get; set; }
        public IFormFile[] ActorFiles { get; set; }
    }
}
