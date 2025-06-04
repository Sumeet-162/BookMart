using System.Collections.Generic;

namespace BookMart.Models
{
    public class GenreViewModel
    {
        public List<Genre> Genres { get; set; } = new List<Genre>();
        public string? SearchQuery { get; set; }
    }
}