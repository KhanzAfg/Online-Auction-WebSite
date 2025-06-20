using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineAuction.Models
{
    public class Message
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string SenderId { get; set; } = string.Empty;

        [Required]
        public string ReceiverId { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Content { get; set; } = string.Empty;

        public bool IsRead { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ReadAt { get; set; }

        // Optional: Link to auction for auction-specific messages
        public int? AuctionId { get; set; }

        // Navigation properties
        [ForeignKey("SenderId")]
        public ApplicationUser? Sender { get; set; }

        [ForeignKey("ReceiverId")]
        public ApplicationUser? Receiver { get; set; }

        [ForeignKey("AuctionId")]
        public Auction? Auction { get; set; }
    }

    public class Conversation
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string User1Id { get; set; } = string.Empty;

        [Required]
        public string User2Id { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("User1Id")]
        public ApplicationUser? User1 { get; set; }

        [ForeignKey("User2Id")]
        public ApplicationUser? User2 { get; set; }

        public ICollection<Message> Messages { get; set; } = [];
    }
} 