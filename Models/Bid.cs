using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineAuction.Models
{
    public class Bid
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int AuctionId { get; set; }

        [Required]
        public string BidderId { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public DateTime BidTime { get; set; } = DateTime.UtcNow;

        [NotMapped]
        public bool IsWinning { get; set; }

        // Navigation properties
        [ForeignKey("AuctionId")]
        public Auction? Auction { get; set; }

        [ForeignKey("BidderId")]
        public ApplicationUser? Bidder { get; set; }
    }
} 