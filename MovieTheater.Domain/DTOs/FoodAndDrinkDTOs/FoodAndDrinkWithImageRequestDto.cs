using Microsoft.AspNetCore.Http;
using MovieTheater.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MovieTheater.Domain.DTOs.FoodAndDrinkDTOs
{
    public class FoodAndDrinkWithImageRequestDto
    {
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public FoodType Type { get; set; }
        public bool IsAvailable { get; set; } = true;
        public IFormFile? File { get; set; }
    }
}
