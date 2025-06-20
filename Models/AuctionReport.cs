using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineAuction.Models
{
    public class AuctionReport
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int AuctionId { get; set; }

        [Required]
        public string ReporterId { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Reason { get; set; } = string.Empty;

        public DateTime ReportedAt { get; set; } = DateTime.UtcNow;

        public bool IsResolved { get; set; }

        // Navigation properties
        [ForeignKey("AuctionId")]
        public Auction? Auction { get; set; }

        [ForeignKey("ReporterId")]
        public ApplicationUser? Reporter { get; set; }
    }
} 