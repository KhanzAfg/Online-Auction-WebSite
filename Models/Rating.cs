using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineAuction.Models
{
    public class Rating
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string RatedUserId { get; set; } = string.Empty;

        [Required]
        public string RaterUserId { get; set; } = string.Empty;

        [Required]
        public int AuctionId { get; set; }

        [Required]
        [Range(-5, 5)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Score { get; set; }

        [StringLength(500)]
        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("RatedUserId")]
        public ApplicationUser? RatedUser { get; set; }

        [ForeignKey("RaterUserId")]
        public ApplicationUser? RaterUser { get; set; }

        [ForeignKey("AuctionId")]
        public Auction? Auction { get; set; }
    }
} 