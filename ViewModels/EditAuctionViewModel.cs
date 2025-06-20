using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using OnlineAuction.Models;

namespace OnlineAuction.ViewModels
{
    public class EditAuctionViewModel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Minimum bid price must be greater than 0")]
        [Display(Name = "Minimum Bid Price")]
        public decimal MinimumBidPrice { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Bid increment must be greater than 0")]
        [Display(Name = "Bid Increment")]
        public decimal BidIncrement { get; set; }

        [Required]
        [Display(Name = "End Date")]
        public DateTime EndDate { get; set; }

        [Required]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        [Display(Name = "New Images")]
        public List<IFormFile>? NewImages { get; set; }

        [Display(Name = "Images to Delete")]
        public List<int>? ImagesToDelete { get; set; }

        [Display(Name = "Specification Document")]
        public IFormFile? SpecificationDocument { get; set; }

        public string? CurrentSpecificationDocumentPath { get; set; }

        public List<AuctionImage> CurrentImages { get; set; } = [];
    }
} 