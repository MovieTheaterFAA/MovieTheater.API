using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MovieTheater.Domain.DTOs.BlobDTOs
{
    public class FileUploadRequestDto
    {
        [FromForm(Name = "file")]
        public IFormFile File { get; set; }
    }
}
