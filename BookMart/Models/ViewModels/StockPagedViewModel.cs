using System.Collections.Generic;

namespace BookMart.Models.ViewModels
{
    public class StockPagedViewModel
    {
        public List<BookStockViewModel> Books { get; set; } = new();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalBooks { get; set; }
    }
} 