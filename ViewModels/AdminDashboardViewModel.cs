using OnlineAuction.Models;

namespace OnlineAuction.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalAuctions { get; set; }
        public int TotalCategories { get; set; }
        public List<AuctionReport> RecentReports { get; set; } = [];
    }
} 