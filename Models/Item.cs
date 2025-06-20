namespace OnlineAuction.Models
{
    public class Item
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal MinimumBidPrice { get; set; }
        public decimal CurrentBidPrice { get; set; }
        public decimal BidIncrement { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; } = "Active"; // Active or Inactive
        public string? ImagePath { get; set; }
        public string? DocumentPath { get; set; }
        
        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;
        
        public string SellerId { get; set; } = string.Empty;
        public ApplicationUser Seller { get; set; } = null!;
        
        public virtual ICollection<Bid> Bids { get; set; } = new List<Bid>();
    }
} 