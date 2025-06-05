// Models/User.cs

using System;

using System.Collections.Generic; // Make sure this is included for ICollection

using System.ComponentModel.DataAnnotations;

using System.ComponentModel.DataAnnotations.Schema;

using Newtonsoft.Json; // Import Newtonsoft.Json for [JsonIgnore]

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

        [Required]

        [StringLength(50)]

        public string FirstName { get; set; } = string.Empty;

        [Required]

        [StringLength(50)]

        public string LastName { get; set; } = string.Empty;

        [Required]

        [StringLength(15)]

        public string Phone { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public bool IsAdmin { get; set; }

        // Navigation properties (ensure these are present if you use them)

        [JsonIgnore] // Added JsonIgnore to prevent circular serialization

        public ICollection<Carts>? Carts { get; set; } // Changed type from Cart to Carts

        [JsonIgnore] // Added JsonIgnore to prevent circular serialization

        public ICollection<Order>? Orders { get; set; }

        [JsonIgnore] // Added JsonIgnore to prevent circular serialization

        public ICollection<UserAddress>? UserAddresses { get; set; }

    }

}

