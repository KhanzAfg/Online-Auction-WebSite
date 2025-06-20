using System.ComponentModel.DataAnnotations;

namespace OnlineAuction.ViewModels
{
    public class SearchViewModel
    {
        [Display(Name = "Search Term")]
        public string? SearchTerm { get; set; }

        [Display(Name = "Category")]
        public int? CategoryId { get; set; }

        [Display(Name = "Minimum Price")]
        [Range(0, double.MaxValue, ErrorMessage = "Minimum price must be positive")]
        public decimal? MinPrice { get; set; }

        [Display(Name = "Maximum Price")]
        [Range(0, double.MaxValue, ErrorMessage = "Maximum price must be positive")]
        public decimal? MaxPrice { get; set; }

        [Display(Name = "Start Date")]
        public DateTime? StartDate { get; set; }

        [Display(Name = "End Date")]
        public DateTime? EndDate { get; set; }

        [Display(Name = "Sort By")]
        public string SortBy { get; set; } = "newest";

        [Display(Name = "Sort Order")]
        public string SortOrder { get; set; } = "desc";

        [Display(Name = "Show Ended Auctions")]
        public bool ShowEnded { get; set; } = false;

        [Display(Name = "Show Only Active Auctions")]
        public bool ShowActiveOnly { get; set; } = true;

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 12;
    }
} 