using System.Collections.Generic;
using OnlineAuction.Models;

namespace OnlineAuction.ViewModels
{
    public class HomePageViewModel
    {
        public List<Auction> Auctions { get; set; } = new();
        public List<Auction> FeaturedAuctions { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
    }
} 