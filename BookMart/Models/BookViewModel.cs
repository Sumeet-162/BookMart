//// Models/BookViewModel.cs
//using System.Collections.Generic;
//using BookMart.Models;

//namespace BookMart.Models
//{
//    public class BookViewModel
//    {
//        public List<Book> Books { get; set; } = new List<Book>();
//        public string? SearchQuery { get; set; }
//        // Add properties for pagination if you implement it (e.g., int PageNumber, int PageSize, int TotalPages)
//    }
//}

// BookMart/ViewModels/BookViewModel.cs
using System.Collections.Generic;

namespace BookMart.Models // <--- Use your main Models namespace here
{
    public class BookViewModel
    {
        public List<Book> Books { get; set; }
        public string? SearchQuery { get; set; } // For the search input
    }
}