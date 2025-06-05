using System.ComponentModel.DataAnnotations;

using System.ComponentModel.DataAnnotations.Schema;

using Newtonsoft.Json; // Import Newtonsoft.Json for [JsonIgnore]

namespace BookMart.Models

{

    public class UserAddress

    {

        [Key]

        public int AddressID { get; set; }

        [ForeignKey("User")]

        public int UserID { get; set; }

        [JsonIgnore] // Ignore this navigation property to prevent circular reference during JSON serialization

        public User? User { get; set; }

        [StringLength(20)]

        public string? AddressType { get; set; } // 'Billing' or 'Shipping'

        [StringLength(100)]

        public string? AddressLine1 { get; set; }

        [StringLength(100)]

        public string? AddressLine2 { get; set; }

        [StringLength(50)]

        public string? City { get; set; }

        [StringLength(50)]

        public string? State { get; set; }

        [StringLength(10)]

        public string? PostalCode { get; set; }

        [StringLength(50)]

        public string? Country { get; set; }

        public bool IsDefault { get; set; } = false;

    }

}

