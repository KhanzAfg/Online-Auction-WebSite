using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace OnlineAuction.ViewModels
{
    public class SellAuctionViewModel
    {
        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Starting price must be greater than 0")]
        [Display(Name = "Starting Price")]
        public decimal StartingPrice { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Minimum bid price must be greater than 0")]
        [Display(Name = "Minimum Bid Price")]
        public decimal MinimumBidPrice { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Bid increment must be greater than 0")]
        [Display(Name = "Bid Increment")]
        public decimal BidIncrement { get; set; }

        [Required]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; } = DateTime.UtcNow;

        [Required]
        [Display(Name = "End Date")]
        public DateTime EndDate { get; set; } = DateTime.UtcNow.AddDays(7);

        [Required]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        [Display(Name = "Images")]
        public List<IFormFile>? Images { get; set; }

        [Display(Name = "Specification Document")]
        public IFormFile? SpecificationDocument { get; set; }
    }
} 