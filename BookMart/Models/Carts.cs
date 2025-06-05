using System;

using System.Collections.Generic;

using System.ComponentModel.DataAnnotations;

using System.ComponentModel.DataAnnotations.Schema;

using Newtonsoft.Json; // Import Newtonsoft.Json for [JsonIgnore]

namespace BookMart.Models

{

    public class Carts // Class name changed from Cart to Carts to match filename

    {

        [Key]

        public int CartID { get; set; } // This ID remains 'CartID' as it's the primary key for the cart entity

        [ForeignKey("User")]

        public int UserID { get; set; }

        public User? User { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [JsonIgnore] // Ignore this navigation property to prevent circular reference during JSON serialization

        public ICollection<CartItem>? CartItems { get; set; }

    }

}

