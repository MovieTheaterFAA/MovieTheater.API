using MovieTheater.Domain.Entities;
using MovieTheater.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MovieTheater.Domain.DTOs.FoodAndDrinkDTOs
{
    public class FoodAndDrinkResponseDto
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public string? Description { get; set; }

        public decimal Price { get; set; }
        public FoodType Type { get; set; } // Enum: Food, Drink, Combo
        public string? ImageUrl { get; set; }
        public bool IsAvailable { get; set; } = true;

        public bool IsAvailable { get; set; }

        public DateTime CreatedAt { get; set; }

        public Guid CreatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public Guid? UpdatedBy { get; set; }

        public DateTime? DeletedAt { get; set; }

        public Guid? DeletedBy { get; set; }
    }
}