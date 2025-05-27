using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MovieTheater.Domain.Entities
{
    public class Movie : BaseEntity
    {
        public string Name { get; set; }

        public DateTime FromDate { get; set; }

        public DateTime ToDate { get; set; }

        public List<string> Actors { get; set; }

        public string ProductionCompany { get; set; }

        public string Director { get; set; }

        public int? RunningTime { get; set; }

        public string Version { get; set; }

        public string TrailerUrl { get; set; }

        public List<string> Genres { get; set; }

        public string ContentSynopsis { get; set; }

        public string PosterImage { get; set; }

        // Navigation
        public ICollection<ShowTime> Showtimes { get; set; }
    }
}
