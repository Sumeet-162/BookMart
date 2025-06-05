using System.Collections.Generic;

namespace BookMart.Models
{
    public class HomeViewModel
    {
        public List<Book> FeaturedBooks { get; set; } = new List<Book>();
        public List<Book> NewArrivals { get; set; } = new List<Book>();
        public List<AuthorViewModel> PopularAuthors { get; set; } = new List<AuthorViewModel>();
        public List<Book> SearchResults { get; set; } = new List<Book>();
        public List<Book> Books { get; set; } = new List<Book>();
        public List<Book> Offers { get; set; } = new List<Book>();
        public int TotalBooks { get; set; }
        public bool IsFiltered { get; set; }
        public string SearchQuery { get; set; }
        public int? GenreId { get; set; }
        public string GenreName { get; set; }
    }
}