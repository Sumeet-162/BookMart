//// ViewModels/GenreViewModel.cs
//using BookMart.Models;
//using System.Collections.Generic;

//namespace BookMart.ViewModels
//{
//    public class GenreViewModel
//    {
//        public List<Genre> Genres { get; set; } = new List<Genre>();
//        // You could add properties here for pagination, search, etc., if needed later.
//    }
//}

// BookMart/ViewModels/GenreViewModel.cs
using System.Collections.Generic;

namespace BookMart.Models // <--- Use your main Models namespace here
{
    public class GenreViewModel
    {
        public List<Genre> Genres { get; set; }
        // You can add other properties here if needed for genre management (e.g., SearchQuery)
    }
}