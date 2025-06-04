using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookMart.Models
{
    public class Order
    {
        [Key]
        public int OrderID { get; set; }

        [ForeignKey("User")]
        public int UserID { get; set; }
        public User? User { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.Now;

        [ForeignKey("ShippingAddress")]
        public int? ShippingAddressID { get; set; }
        public UserAddress? ShippingAddress { get; set; }

        [ForeignKey("BillingAddress")]
        public int? BillingAddressID { get; set; }
        public UserAddress? BillingAddress { get; set; }

        [Required]
        [Column(TypeName = "decimal(10, 2)")]
        public decimal SubTotal { get; set; }

        [Column(TypeName = "decimal(10, 2)")]
        public decimal ShippingCost { get; set; } = 0;

        [Column(TypeName = "decimal(10, 2)")]
        public decimal? TaxAmount { get; set; }

        [Required]
        [Column(TypeName = "decimal(10, 2)")]
        public decimal TotalAmount { get; set; }

        [StringLength(50)]
        public string? PaymentMethod { get; set; } // 'Card', 'UPI', 'COD'

        [StringLength(20)]
        public string? PaymentStatus { get; set; } = "Pending"; // 'Pending', 'Paid', 'Failed'

        [StringLength(20)]
        public string? OrderStatus { get; set; } = "Pending"; // 'Pending', 'Processing', 'Shipped', 'Delivered', 'Cancelled'

        // Navigation property
        public ICollection<OrderItem>? OrderItems { get; set; }
    }
}