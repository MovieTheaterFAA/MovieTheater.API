using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MovieTheater.Domain.Entities
{
    public class Payment : BaseEntity
    {
        public Guid InvoiceId { get; set; }

        [ForeignKey(nameof(InvoiceId))]
        public Invoice Invoice { get; set; }

        public DateTime PaymentDate { get; set; }

        public decimal Amount { get; set; }

        public string Provider { get; set; }
        public string PaymentReference { get; set; }

        public string Status { get; set; }
    }
}
