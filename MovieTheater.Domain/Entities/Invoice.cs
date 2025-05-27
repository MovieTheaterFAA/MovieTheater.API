using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MovieTheater.Domain.Entities
{
    public class Invoice : BaseEntity
    {
        public Guid BookingId { get; set; }

        [ForeignKey(nameof(BookingId))]
        public Booking Booking { get; set; }

        public DateTime InvoiceDate { get; set; }

        public decimal Amount { get; set; }

        public string Status { get; set; }

        // Navigation
        public ICollection<Payment> Payments { get; set; }
    }
}
