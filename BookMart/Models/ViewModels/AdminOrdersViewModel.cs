using System;
using System.Collections.Generic;

namespace BookMart.Models.ViewModels
{
    public class AdminOrdersViewModel
    {
        public List<Order> Orders { get; set; } = new();
        public OrdersFilterModel Filter { get; set; } = new();
        public OrdersSummaryStats Stats { get; set; } = new();
        
        // For pagination
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 10;
    }

    public class OrdersFilterModel
    {
        public string? StatusFilter { get; set; }
        public string? SearchQuery { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? SortBy { get; set; }
        public bool IsDescending { get; set; } = true;
    }

    public class OrdersSummaryStats
    {
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public int ProcessingOrders { get; set; }
        public int ShippedOrders { get; set; }
        public int DeliveredOrders { get; set; }
        public int CancelledOrders { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}