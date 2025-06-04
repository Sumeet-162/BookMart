using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookMart.Models
{
    public class CartItem
    {
        [Key]
        public int CartItemID { get; set; }

        [ForeignKey("Cart")]
        public int CartID { get; set; }
        public Carts? Cart { get; set; }

        [ForeignKey("Book")]
        public int BookID { get; set; }
        public Book? Book { get; set; }

        public int Quantity { get; set; } = 1;

        [Required]
        [Column(TypeName = "decimal(10, 2)")]
        public decimal Price { get; set; } // Price at the time of adding to cart
    }
}