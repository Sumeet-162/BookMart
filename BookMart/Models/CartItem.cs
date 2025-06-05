using System.ComponentModel.DataAnnotations;

using System.ComponentModel.DataAnnotations.Schema;

using Newtonsoft.Json; // Import Newtonsoft.Json for [JsonIgnore]

namespace BookMart.Models

{

    public class CartItem

    {

        [Key]

        public int CartItemID { get; set; }

        [ForeignKey("Cart")] // This refers to the foreign key property name, not the class name

        public int CartID { get; set; }

        [JsonIgnore] // Ignore this navigation property to prevent circular reference during JSON serialization

        public Carts? Cart { get; set; } // Changed type from Cart to Carts

        [ForeignKey("Book")]

        public int BookID { get; set; }

        public Book? Book { get; set; } // Keep this, as it's useful for display in the view

        public int Quantity { get; set; }

        [Required]

        [Column(TypeName = "decimal(10, 2)")]

        public decimal Price { get; set; }

    }

}

