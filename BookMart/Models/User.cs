// Models/User.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookMart.Models
{
    public class User
    {
        [Key]
        [Column("UserID")]
        public int UserID { get; set; } // Matches your INT IDENTITY(1,1) PRIMARY KEY

        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [StringLength(15)]
        public string Phone { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } // Matches your DATETIME DEFAULT GETDATE()

        // Removed: public DateTime? UpdatedAt { get; set; } // This property is removed to match your existing DB schema

        public bool IsAdmin { get; set; }

        // Navigation properties (ensure these are present if you use them)
        public ICollection<Carts>? Carts { get; set; }
        public ICollection<Order>? Orders { get; set; }
        public ICollection<UserAddress>? UserAddresses { get; set; }
    }
}
