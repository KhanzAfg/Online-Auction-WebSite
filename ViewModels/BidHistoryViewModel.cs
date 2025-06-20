using OnlineAuction.Models;

namespace OnlineAuction.ViewModels
{
    public class BidHistoryViewModel
    {
        public int AuctionId { get; set; }
        public string AuctionTitle { get; set; } = string.Empty;
        public decimal StartingPrice { get; set; }
        public decimal CurrentBid { get; set; }
        public int TotalBids { get; set; }
        public int UniqueBidders { get; set; }
        public DateTime AuctionEndDate { get; set; }
        public bool IsActive { get; set; }
        public List<BidDetailViewModel> Bids { get; set; } = [];
        public List<BidderStatsViewModel> BidderStats { get; set; } = [];
    }

    public class BidDetailViewModel
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public string BidderName { get; set; } = string.Empty;
        public string BidderId { get; set; } = string.Empty;
        public string? ProfileImagePath { get; set; }
        public DateTime BidTime { get; set; }
        public bool IsWinning { get; set; }
        public bool IsOutbid { get; set; }
    }

    public class BidderStatsViewModel
    {
        public string BidderId { get; set; } = string.Empty;
        public string BidderName { get; set; } = string.Empty;
        public string? ProfileImagePath { get; set; }
        public int TotalBids { get; set; }
        public decimal HighestBid { get; set; }
        public DateTime FirstBid { get; set; }
        public DateTime LastBid { get; set; }
        public bool IsCurrentWinner { get; set; }
    }
} 