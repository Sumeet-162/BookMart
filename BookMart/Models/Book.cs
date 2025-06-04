// Models/Book.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookMart.Models
{
    public class Book
    {
        [Key]
        [Column("BookID")]
        public int BookID { get; set; }

        [StringLength(13, MinimumLength = 13, ErrorMessage = "ISBN must be 13 characters.")]
        [Display(Name = "ISBN")]
        public string? ISBN { get; set; } // Can be NULL, so string?

        [Required(ErrorMessage = "Title is required.")]
        [StringLength(255, ErrorMessage = "Title cannot exceed 255 characters.")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Author is required.")]
        [StringLength(100, ErrorMessage = "Author cannot exceed 100 characters.")]
        public string Author { get; set; } = string.Empty;

        // Foreign Key to Genre
        [Display(Name = "Genre")]
        public int? GenreID { get; set; } // Nullable if a book might not have a genre initially

        [ForeignKey("GenreID")]
        public Genre? Genre { get; set; } // Navigation property

        [Column(TypeName = "TEXT")] // For larger text content in SQL Server
        public string? Description { get; set; }

        [Required(ErrorMessage = "Price is required.")]
        [Range(0.01, 10000.00, ErrorMessage = "Price must be between 0.01 and 10000.00.")]
        [Column(TypeName = "DECIMAL(10,2)")]
        public decimal Price { get; set; }

        [Column(TypeName = "DECIMAL(10,2)")]
        public decimal? DiscountedPrice { get; set; }

        [Display(Name = "Stock Quantity")]
        public int StockQuantity { get; set; } = 0;

        [Display(Name = "Min Stock Level")]
        public int MinStockLevel { get; set; } = 20;

        [Display(Name = "Page Count")]
        public int? PageCount { get; set; }

        [StringLength(50, ErrorMessage = "Language cannot exceed 50 characters.")]
        public string? Language { get; set; }

        [Display(Name = "Publication Year")] 
        [Range(1800, 2100, ErrorMessage = "Publication year must be between 1800 and 2100.")] 
        public int? PublishedDate { get; set; }

        [StringLength(100, ErrorMessage = "Publisher cannot exceed 100 characters.")]
        public string? Publisher { get; set; }

        [StringLength(255, ErrorMessage = "Cover Image URL cannot exceed 255 characters.")]
        [Display(Name = "Cover Image URL")]
        public string? CoverImageURL { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime CreatedAt { get; set; } = DateTime.Now; // Set default in C#

        [DataType(DataType.DateTime)]
        public DateTime? UpdatedAt { get; set; }

        public bool IsActive { get; set; } = true;

        [NotMapped] // This property is not mapped to the database
        public string StockStatus
        {
            get
            {
                if (StockQuantity == 0)
                    return "Out of Stock";
                else if (StockQuantity <= MinStockLevel)
                    return "Low Stock";
                else
                    return "In Stock";
            }
        }
        [NotMapped]
        public string StockStatusBadgeClass
        {
            get
            {
                if (StockQuantity == 0)
                    return "bg-danger text-danger";
                else if (StockQuantity <= MinStockLevel)
                    return "bg-warning text-warning";
                else
                    return "bg-success text-success";
            }
        }

        [NotMapped]
        public double StockLevelPercentage
        {
            get
            {
                // Assuming a max stock level for visual representation, e.g., 100 or some dynamic value
                // For simplicity, let's cap it at 100 for now or use a reasonable max.
                // A better approach would be to compare against a "full capacity" or simply show raw number.
                // For now, let's just make it proportional to a fixed max for the bar.
                double maxVisualStock = 100.0; // Or define a more dynamic max for the bar
                return (double)StockQuantity / maxVisualStock * 100;
            }
        }
    }
}