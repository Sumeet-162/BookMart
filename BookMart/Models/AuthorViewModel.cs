using System.ComponentModel.DataAnnotations;

namespace BookMart.Models
{
    public class AuthorViewModel
    {
        [Display(Name = "Author Name")]
        public string AuthorName { get; set; } = string.Empty;

        [Display(Name = "Genre")]
        public string Genre { get; set; } = string.Empty; // Assuming genre is a string for simplicity in this view model
                                                          // If you have a Genre model with an ID, you might adapt this.
    }
}