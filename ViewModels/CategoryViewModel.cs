using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using OnlineAuction.Models;

namespace OnlineAuction.ViewModels
{
    public class CategoryViewModel
    {
        // Properties for displaying a category page
        public Category Category { get; set; }
        public List<Auction> Auctions { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }

        // Properties for creating/editing a category
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public IFormFile? Image { get; set; }

        public string? ExistingImagePath { get; set; }
    }
} 