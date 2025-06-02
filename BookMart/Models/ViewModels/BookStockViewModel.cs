namespace BookMart.Models.ViewModels
{
    public class BookStockViewModel
    {
        public int BookId { get; set; }
        public string? Title { get; set; }
        public string? Author { get; set; }
        public string? GenreName { get; set; }
        public int StockQuantity { get; set; }
        public int MinStockLevel { get; set; }
        public decimal Price { get; set; }
        public string? CoverImageURL { get; set; }
        public string StockStatus { get; set; } = string.Empty; // "In Stock", "Low Stock", "Out of Stock"
        public string StatusBadgeClass { get; set; } = string.Empty; // e.g., "bg-success", "bg-warning", "bg-danger"
    }
}