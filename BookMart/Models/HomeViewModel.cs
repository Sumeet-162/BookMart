using System.Collections.Generic;

namespace BookMart.Models
{
    public class HomeViewModel
    {
        public List<Book> FeaturedBooks { get; set; } = new List<Book>();
        public List<Book> NewArrivals { get; set; } = new List<Book>();
        public List<AuthorViewModel> PopularAuthors { get; set; } = new List<AuthorViewModel>();
    }
}