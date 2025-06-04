// Models/Genre.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // If you need [Column] attributes

namespace BookMart.Models
{
    public class Genre
    {
        [Key]
        public int GenreID { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty; // Initialize to avoid null warnings

        [Column(TypeName = "TEXT")] // Maps to SQL TEXT type
        public string? Description { get; set; } // Nullable, as it's optional

        [StringLength(50)]
        public string? IconClass { get; set; } // Nullable, for FontAwesome icons

        [StringLength(20)]
        public string? ColorTheme { get; set; } // Nullable, for UI styling

        // For CreatedAt, typically you wouldn't set a default in the model
        // but let the database handle GETDATE() or set it programmatically on add.
        // If you want EF Core to track it, you can just define it:
        public DateTime CreatedAt { get; set; }
        // If you want a default value when *inserting via EF Core*, you can
        // configure it in OnModelCreating, but GETDATE() is a DB-level default.

        // Navigation property (if you have one, e.g., for books in this genre)
        public ICollection<Book>? Books { get; set; }
    }
}