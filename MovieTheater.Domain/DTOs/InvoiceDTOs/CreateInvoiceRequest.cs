using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MovieTheater.Domain.DTOs.InvoiceDTOs
{
    public class CreateInvoiceRequest
    {
        public Guid? PromotionId { get; set; }
        public int? RequestedPoints { get; set; }
    }
}
