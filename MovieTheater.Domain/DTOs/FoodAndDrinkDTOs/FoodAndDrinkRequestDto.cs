using MovieTheater.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MovieTheater.Domain.DTOs.FoodAndDrinkDTOs
{
    public class FoodAndDrinkRequestDto
    {
        [Required(ErrorMessage = "Name is required.")]
        [StringLength(100, ErrorMessage = "Name can't be longer than 100 characters.")]
        public string Name { get; set; }

        [StringLength(500, ErrorMessage = "Description can't be longer than 500 characters.")]
        public string? Description { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than zero.")]
        public decimal Price { get; set; }
        [Required(ErrorMessage = "Food type is required.")]
        public FoodType Type { get; set; } // Enum: Food, Drink, Combo

        [Url(ErrorMessage = "Invalid URL format.")]
        public string? ImageUrl { get; set; }

        public bool IsAvailable { get; set; } = true;
    }
}