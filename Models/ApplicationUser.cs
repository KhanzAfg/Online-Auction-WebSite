using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineAuction.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Address { get; set; }

        public string? ProfileImagePath { get; set; }

        public bool IsBlocked { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Rating properties
        [Column(TypeName = "decimal(18,2)")]
        public decimal Rating { get; set; }
        public int TotalRatings { get; set; }
        public int PositiveRatings { get; set; }
        public int NegativeRatings { get; set; }

        // Navigation properties
        public ICollection<Auction> Auctions { get; set; } = [];
        public ICollection<Bid> Bids { get; set; } = [];
        public ICollection<Notification> Notifications { get; set; } = [];
        public ICollection<AuctionReport> Reports { get; set; } = [];
        public ICollection<Rating> RatingsReceived { get; set; } = [];
        public ICollection<Rating> RatingsGiven { get; set; } = [];
        public ICollection<Watchlist> Watchlist { get; set; } = [];
        
        // New navigation properties
        public ICollection<Payment> PaymentsAsBuyer { get; set; } = [];
        public ICollection<Payment> PaymentsAsSeller { get; set; } = [];
        public ICollection<Message> MessagesSent { get; set; } = [];
        public ICollection<Message> MessagesReceived { get; set; } = [];
        public ICollection<Conversation> ConversationsAsUser1 { get; set; } = [];
        public ICollection<Conversation> ConversationsAsUser2 { get; set; } = [];
        public ICollection<Shipping> ShippingsAsBuyer { get; set; } = [];
        public ICollection<Shipping> ShippingsAsSeller { get; set; } = [];
    }
} 