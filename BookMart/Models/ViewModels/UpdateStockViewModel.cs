using System.ComponentModel.DataAnnotations;

namespace BookMart.Models.ViewModels
{
    public class UpdateStockViewModel
    {
        public int BookId { get; set; }
        public string? Title { get; set; }
        public string? Author { get; set; }
        public string? CoverImageURL { get; set; }
        public string? GenreName { get; set; } // For display
        public int CurrentStockQuantity { get; set; }
        public int MinStockLevel { get; set; } // For display context

        [Required(ErrorMessage = "Please select an operation.")]
        public string Operation { get; set; } = string.Empty; // "add", "remove", "adjust"

        [Required(ErrorMessage = "Please enter a quantity.")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be a positive number.")]
        public int Quantity { get; set; }

        [Required(ErrorMessage = "Please select a reason.")]
        public string Reason { get; set; } = string.Empty;
        public string? Notes { get; set; }

        // For display on the page, not part of the form submission directly but useful
        public int OnOrderQuantity { get; set; } // Example: you might calculate this
    }
}