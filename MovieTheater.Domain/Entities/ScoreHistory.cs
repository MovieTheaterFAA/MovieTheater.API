using MovieTheater.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MovieTheater.Domain.Entities
{
    public class ScoreHistory : BaseEntity
    {
        public Guid MemberId { get; set; }

        [ForeignKey(nameof(MemberId))]
        public User Member { get; set; }

        public DateTime ChangeDate { get; set; }

        public ScoreChangeType ChangeType { get; set; }

        public int ScoreValue { get; set; }

        public Guid? RelatedBookingId { get; set; }

        [ForeignKey(nameof(RelatedBookingId))]
        public Booking RelatedBooking { get; set; }
    }
}
