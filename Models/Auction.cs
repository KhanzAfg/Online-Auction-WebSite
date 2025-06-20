using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace OnlineAuction.Models
{
    public class Auction
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Item Title")]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(4000)]
        [Display(Name = "Item Description")]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Bid Status")]
        public AuctionStatus Status { get; set; }

        [Required]
        [Display(Name = "Bid Start Date")]
        public DateTime StartDate { get; set; }

        [Required]
        [Display(Name = "Bid End Date")]
        public DateTime EndDate { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Bid Increment")]
        public decimal BidIncrement { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Minimum Bid")]
        public decimal MinimumBidPrice { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Starting Price")]
        public decimal StartingPrice { get; set; }

        [Required]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        [Required]
        public string SellerId { get; set; } = string.Empty;

        public string? SpecificationDocumentPath { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("CategoryId")]
        public Category? Category { get; set; }

        [ForeignKey("SellerId")]
        public ApplicationUser? Seller { get; set; }

        public ICollection<AuctionImage> Images { get; set; } = [];
        public ICollection<Bid> Bids { get; set; } = [];
        public ICollection<AuctionReport> Reports { get; set; } = [];
        public ICollection<Rating> Ratings { get; set; } = [];
        public ICollection<Watchlist> Watchlist { get; set; } = [];

        // Computed property for current highest bid
        [NotMapped]
        public Bid? CurrentBid => Bids.OrderByDescending(b => b.Amount).FirstOrDefault();

        public DateTime EndTime { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsFeatured { get; set; } = false;
    }
} 