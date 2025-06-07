using System.Collections.Generic;

namespace BookMart.Models
{
    public class BookViewModel
    {
        public List<Book> Books { get; set; } = new List<Book>();
        public string? SearchQuery { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalBooks { get; set; }
    }
}