using System;
using System.Collections.Generic;

namespace BookMart.Models.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalOrders { get; set; }
        public int TotalBooksInStock { get; set; }
        public int ActiveUsers { get; set; }
        public decimal TotalRevenue { get; set; }
        
        // Recent Orders section
        public List<Order> RecentOrders { get; set; } = new();
        
        // Top Selling Books section
        public List<BookSalesSummary> TopSellingBooks { get; set; } = new();
    }

    public class BookSalesSummary
    {
        public int BookId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string CoverImageURL { get; set; } = string.Empty;
        public int TotalSold { get; set; }
        public decimal Revenue { get; set; }
    }
}