// Models/User.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookMart.Models
{
    public class User
    {
        [Key]
        [Column("UserID")] // Explicitly map to the database column name
        public int UserID { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        // --- NEW: These are now non-nullable in the model ---
        [Required] // Add Required attribute to match DB if you make them NOT NULL
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty; // Changed from string? to string

        [Required] // Add Required attribute to match DB if you make them NOT NULL
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;  // Changed from string? to string

        [Required] // Add Required attribute to match DB if you make them NOT NULL
        [StringLength(15)]
        public string Phone { get; set; } = string.Empty;     // Changed from string? to string

        public DateTime CreatedAt { get; set; }

        public bool IsAdmin { get; set; }
    }
}