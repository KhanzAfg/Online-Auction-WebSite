using System.ComponentModel.DataAnnotations;

namespace OnlineAuction.ViewModels
{
    public class MergeCategoriesViewModel
    {
        [Required]
        [Display(Name = "Source Category")]
        public int SourceCategoryId { get; set; }

        [Required]
        [Display(Name = "Target Category")]
        public int TargetCategoryId { get; set; }
    }
} 