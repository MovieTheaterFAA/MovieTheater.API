using System.ComponentModel.DataAnnotations.Schema;

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
        public Guid? PromotionId { get; set; }

        [ForeignKey(nameof(PromotionId))]
        public Promotion? Promotion { get; set; }
        public ICollection<Payment> Payments { get; set; }
    }
}
