using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MovieTheater.Domain.DTOs.BlobDTOs
{
    public class MovieCastUploadDto
    {
        [Required]
        public IFormFile File { get; set; }

        [Required]
        public string ActorName { get; set; }
    }
}
