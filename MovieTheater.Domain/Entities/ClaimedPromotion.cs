using System.ComponentModel.DataAnnotations.Schema;

namespace MovieTheater.Domain.Entities
{
    public class ClaimedPromotion : BaseEntity
    {
        public Guid UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User User { get; set; }

        public Guid PromotionId { get; set; }

        [ForeignKey(nameof(PromotionId))]
        public Promotion Promotion { get; set; }

        public DateTime ClaimedAt { get; set; } = DateTime.UtcNow;

        public bool IsUsed { get; set; } = false;
    }
}