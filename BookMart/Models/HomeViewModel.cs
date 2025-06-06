using System.Collections.Generic;

namespace BookMart.Models
{
    public class HomeViewModel
    {
        public List<Book> FeaturedBooks { get; set; } = new();
        public List<Book> Books { get; set; } = new();
        public List<Book> Offers { get; set; } = new();
        public List<AuthorViewModel> PopularAuthors { get; set; } = new();
        public List<Book> SearchResults { get; set; } = new();
        public int TotalBooks { get; set; }
        public int TotalFeaturedBooks { get; set; }
        public bool IsFiltered { get; set; }
        public string? SearchQuery { get; set; }
        public int? GenreId { get; set; }
        public string? GenreName { get; set; }
        public int PageSize { get; set; } = 8;
    }
}