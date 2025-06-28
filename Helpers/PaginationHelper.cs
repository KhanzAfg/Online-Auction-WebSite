using System.Collections.Generic;

namespace OnlineAuction.Helpers
{
    public static class PaginationHelper
    {
        public static List<PaginationItem> GeneratePagination(int currentPage, int totalPages, int maxVisiblePages = 7)
        {
            var items = new List<PaginationItem>();

            if (totalPages <= 1)
                return items;

            // Always show first page
            items.Add(new PaginationItem { PageNumber = 1, IsActive = currentPage == 1, IsVisible = true });

            if (totalPages <= maxVisiblePages)
            {
                // Show all pages if total is small
                for (int i = 2; i <= totalPages; i++)
                {
                    items.Add(new PaginationItem { PageNumber = i, IsActive = currentPage == i, IsVisible = true });
                }
            }
            else
            {
                // Calculate start and end of visible range
                int startPage = Math.Max(2, currentPage - (maxVisiblePages - 4) / 2);
                int endPage = Math.Min(totalPages - 1, startPage + maxVisiblePages - 5);
                
                // Adjust start if we're near the end
                if (endPage == totalPages - 1)
                {
                    startPage = Math.Max(2, endPage - maxVisiblePages + 5);
                }

                // Add ellipsis after first page if needed
                if (startPage > 2)
                {
                    items.Add(new PaginationItem { PageNumber = -1, IsVisible = true, IsEllipsis = true });
                }

                // Add visible pages
                for (int i = startPage; i <= endPage; i++)
                {
                    items.Add(new PaginationItem { PageNumber = i, IsActive = currentPage == i, IsVisible = true });
                }

                // Add ellipsis before last page if needed
                if (endPage < totalPages - 1)
                {
                    items.Add(new PaginationItem { PageNumber = -1, IsVisible = true, IsEllipsis = true });
                }

                // Always show last page
                items.Add(new PaginationItem { PageNumber = totalPages, IsActive = currentPage == totalPages, IsVisible = true });
            }

            return items;
        }
    }

    public class PaginationItem
    {
        public int PageNumber { get; set; }
        public bool IsActive { get; set; }
        public bool IsVisible { get; set; }
        public bool IsEllipsis { get; set; }
    }
} 