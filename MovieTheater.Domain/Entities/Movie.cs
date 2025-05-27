using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MovieTheater.Domain.Enums;

namespace MovieTheater.Domain.Entities
{
    public class Movie : BaseEntity
    {
        public string Name { get; set; }

        public DateTime FromDate { get; set; }

        public DateTime ToDate { get; set; }

        public List<string> Actors { get; set; }

        public string Director { get; set; }

        public int? RunningTime { get; set; }         // Thời lượng chiếu

        public MovieVersion Version { get; set; }     // Phiên bản phim (2D, 3D, IMAX, v.v.)

        public string TrailerUrl { get; set; }

        public List<string> Genres { get; set; }      // Thể loại phim - MovieType

        public string Description { get; set; }

        public string PosterImage { get; set; }       // Poster film trên MinIO

        // Navigation
        public ICollection<ShowTime> Showtimes { get; set; }
    }
}