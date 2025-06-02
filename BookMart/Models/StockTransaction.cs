using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookMart.Models
{
    public class StockTransaction
    {
        [Key]
        public int TransactionID { get; set; }

        [Required]
        public int BookID { get; set; }
        [ForeignKey("BookID")]
        public virtual Book? Book { get; set; }

        [StringLength(20)]
        public string? TransactionType { get; set; } // 'Add', 'Remove', 'Adjust'

        public int Quantity { get; set; }
        public int PreviousQuantity { get; set; }
        public int NewQuantity { get; set; }

        [StringLength(50)]
        public string? Reason { get; set; } // 'New Stock', 'Damaged', 'Order', 'Return', 'Adjustment'
        public string? Notes { get; set; }
        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

        public int? UserID { get; set; } // Nullable for simplicity, link to your User model if needed
        // [ForeignKey("UserID")]
        // public virtual User? User { get; set; } // Assuming you have a User model
    }
}