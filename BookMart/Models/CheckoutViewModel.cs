// Models/CheckoutViewModel.cs
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BookMart.Models
{
    public class CheckoutViewModel
    {
        public List<CartItem> CartItems { get; set; } = new List<CartItem>();

        // Order Summary Details
        public decimal SubTotal { get; set; }
        public decimal ShippingCost { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }

        // Shipping Address Fields (for the form)
        [Required(ErrorMessage = "First Name is required.")]
        [StringLength(100)]
        [Display(Name = "First Name")]
        public string ShippingFirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last Name is required.")]
        [StringLength(100)]
        [Display(Name = "Last Name")]
        public string ShippingLastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Address is required.")]
        [StringLength(250)]
        [Display(Name = "Street Address")]
        public string ShippingAddressLine1 { get; set; } = string.Empty;

        [StringLength(250)]
        [Display(Name = "Apartment, suite, etc. (optional)")]
        public string? ShippingAddressLine2 { get; set; }

        [Required(ErrorMessage = "City is required.")]
        [StringLength(100)]
        public string ShippingCity { get; set; } = string.Empty;

        [Required(ErrorMessage = "State is required.")]
        [StringLength(100)]
        public string ShippingState { get; set; } = string.Empty;

        [Required(ErrorMessage = "PIN Code is required.")]
        [StringLength(20)]
        [Display(Name = "PIN Code")]
        public string ShippingPinCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone is required.")]
        [StringLength(20)]
        [Phone(ErrorMessage = "Invalid Phone Number.")]
        public string ShippingPhone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [StringLength(250)]
        [EmailAddress(ErrorMessage = "Invalid Email Address.")]
        public string ShippingEmail { get; set; } = string.Empty;

        public bool SaveAddressForFuture { get; set; }

        public string? CouponCode { get; set; }
        public decimal CouponDiscount { get; set; }
    }
}