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

        // --- Shipping Address Fields (Directly in Order table) ---
        [Required(ErrorMessage = "Shipping First Name is required.")]
        [StringLength(100)]
        public string ShippingFirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Shipping Last Name is required.")]
        [StringLength(100)]
        public string ShippingLastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Shipping Address is required.")]
        [StringLength(250)]
        public string ShippingAddressLine1 { get; set; } = string.Empty;

        [StringLength(250)]
        public string? ShippingAddressLine2 { get; set; } // Apartment, suite, etc. (optional)

        [Required(ErrorMessage = "Shipping City is required.")]
        [StringLength(100)]
        public string ShippingCity { get; set; } = string.Empty;

        [Required(ErrorMessage = "Shipping State is required.")]
        [StringLength(100)]
        public string ShippingState { get; set; } = string.Empty;

        [Required(ErrorMessage = "Shipping PIN Code is required.")]
        [StringLength(20)]
        public string ShippingPinCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Shipping Phone is required.")]
        [StringLength(20)]
        public string ShippingPhone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Shipping Email is required.")]
        [StringLength(250)]
        [EmailAddress(ErrorMessage = "Invalid Email Address.")]
        public string ShippingEmail { get; set; } = string.Empty;
        // --- End Shipping Address Fields ---

        [Required]
        [Column(TypeName = "decimal(18,2)")] // Increased precision for currency
        public decimal SubTotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ShippingCost { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal? TaxAmount { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")] // Increased precision for currency
        public decimal TotalAmount { get; set; }

        [StringLength(50)]
        public string? PaymentMethod { get; set; } // 'Card', 'UPI', 'COD'

        [StringLength(20)]
        public string? PaymentStatus { get; set; } = "Pending"; // 'Pending', 'Paid', 'Failed'

        [StringLength(20)]
        public string? OrderStatus { get; set; } = "Pending"; // 'Pending', 'Processing', 'Shipped', 'Delivered', 'Cancelled'

        [StringLength(50)]
        public string? CouponCode { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CouponDiscount { get; set; }

        // Navigation property
        public ICollection<OrderItem>? OrderItems { get; set; }
    }
}
