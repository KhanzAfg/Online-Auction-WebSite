using System.ComponentModel.DataAnnotations;
using OnlineAuction.Models;

namespace OnlineAuction.ViewModels
{
    public class ProcessPaymentViewModel
    {
        public int AuctionId { get; set; }

        [Display(Name = "Auction Title")]
        public string AuctionTitle { get; set; } = string.Empty;

        [Display(Name = "Seller")]
        public string SellerName { get; set; } = string.Empty;

        [Display(Name = "Amount")]
        public decimal Amount { get; set; }

        [Required]
        [Display(Name = "Payment Method")]
        public PaymentMethod PaymentMethod { get; set; }

        [Required]
        [Display(Name = "Shipping Address")]
        [StringLength(200)]
        public string ShippingAddress { get; set; } = string.Empty;

        [Required]
        [Display(Name = "City")]
        [StringLength(100)]
        public string City { get; set; } = string.Empty;

        [Required]
        [Display(Name = "State")]
        [StringLength(100)]
        public string State { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Postal Code")]
        [StringLength(20)]
        public string PostalCode { get; set; } = string.Empty;

        [Display(Name = "Country")]
        [StringLength(100)]
        public string Country { get; set; } = "USA";

        [Display(Name = "Shipping Cost")]
        [Range(0, 1000)]
        public decimal ShippingCost { get; set; }
    }

    public class PaymentHistoryViewModel
    {
        public int Id { get; set; }
        public string AuctionTitle { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public PaymentStatus Status { get; set; }
        public PaymentMethod Method { get; set; }
        public DateTime CreatedAt { get; set; }
        public string OtherPartyName { get; set; } = string.Empty;
        public bool IsBuyer { get; set; }
    }
} 