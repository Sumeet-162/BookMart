namespace BookMart.Models
{
    public class CartItemViewModel
    {
        public int BookId { get; set; }
        public string Title { get; set; } = string.Empty;
        public decimal Price { get; set; } // The price at which it was added to cart
        public int Quantity { get; set; }
        public string? CoverImageURL { get; set; }
    }
}