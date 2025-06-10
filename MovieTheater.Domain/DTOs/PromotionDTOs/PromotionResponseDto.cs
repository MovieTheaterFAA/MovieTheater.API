using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MovieTheater.Domain.DTOs.PromotionDTOs
{
    public class PromotionResponseDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public decimal DiscountValue { get; set; }
        public string Detail { get; set; }
        public string Image { get; set; }
    }
}
