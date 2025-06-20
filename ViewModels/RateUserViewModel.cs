using System.ComponentModel.DataAnnotations;

namespace OnlineAuction.ViewModels
{
    public class RateUserViewModel
    {
        public int AuctionId { get; set; }
        public string AuctionTitle { get; set; } = string.Empty;
        public string UserToRateId { get; set; } = string.Empty;
        public string UserToRateName { get; set; } = string.Empty;
        public bool IsRatingBuyer { get; set; }

        [Required]
        [Range(-5, 5, ErrorMessage = "Rating must be between -5 and 5")]
        [Display(Name = "Rating")]
        public int Score { get; set; }

        [StringLength(500)]
        [Display(Name = "Comment (Optional)")]
        public string? Comment { get; set; }
    }
} 