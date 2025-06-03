// Data/ApplicationDbContext.cs
using BookMart.Models;
using Microsoft.EntityFrameworkCore;
using System; // Make sure this is included for DateTime

namespace BookMart.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<Book> Books { get; set; } // Added DbSet for Book
    public DbSet<Genre> Genres { get; set; } // Added DbSet for Genre

    public DbSet<StockTransaction> StockTransactions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Existing User configurations
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // Seed initial Genres (optional, but good for testing)
        modelBuilder.Entity<Genre>().HasData(
            new Genre { GenreID = 1, Name = "Fiction" },
            new Genre { GenreID = 2, Name = "Science Fiction" },
            new Genre { GenreID = 3, Name = "Fantasy" },
            new Genre { GenreID = 4, Name = "Mystery" },
            new Genre { GenreID = 5, Name = "Thriller" },
            new Genre { GenreID = 6, Name = "Romance" },
            new Genre { GenreID = 7, Name = "Biography" },
            new Genre { GenreID = 8, Name = "History" },
            new Genre { GenreID = 9, Name = "Self-Help" },
            new Genre { GenreID = 10, Name = "Cookbook" }
        );

        // Seed initial Books (optional, for testing display)
        modelBuilder.Entity<Book>().HasData(
            new Book
            {
                BookID = 1,
                ISBN = "9780061120084",
                Title = "To Kill a Mockingbird",
                Author = "Harper Lee",
                GenreID = 1, // Fiction
                Description = "A classic of modern American literature, 'To Kill a Mockingbird' by Harper Lee, published in 1960, explores themes of racial injustice and childhood innocence in the American South. The story is narrated by Scout Finch, a young girl whose lawyer father, Atticus Finch, defends a black man falsely accused of rape. This Pulitzer Prize-winning novel is celebrated for its powerful depiction of morality, prejudice, and courage.",
                Price = 299.00M,
                StockQuantity = 50,
                MinStockLevel = 20,
                PageCount = 336,
                Language = "English",
                PublishedDate = 1960,
                Publisher = "J. B. Lippincott & Co.",
                CoverImageURL = "https://raw.githubusercontent.com/Sumeet-162/eCommerce.Net/refs/heads/main/images/to%20kill.jpg", // Ensure this path exists in wwwroot/images
                CreatedAt = new DateTime(2023, 1, 1, 10, 0, 0),
                IsActive = true
            },
            new Book
            {
                BookID = 2,
                ISBN = "9780812981605",
                Title = "The Power of Habit",
                Author = "Charles Duhigg",
                GenreID = 9, // Self-Help
                Description = "In 'The Power of Habit: Why We Do What We Do in Life and Business,' Charles Duhigg, a Pulitzer Prize-winning reporter, explores the science behind habit formation and reformation. Published in 2012, the book delves into how habits function at individual, organizational, and societal levels, providing insights into how they can be changed for personal and business success.",
                Price = 400.00M,
                DiscountedPrice = 350.00M,
                StockQuantity = 75,
                MinStockLevel = 20,
                PageCount = 400,
                Language = "English",
                PublishedDate = 2012, 
                Publisher = "Random House",
                CoverImageURL = "https://raw.githubusercontent.com/Sumeet-162/eCommerce.Net/refs/heads/main/images/the%20power.jpg", // Ensure this path exists in wwwroot/images
                CreatedAt = new DateTime(2023, 1, 2, 11, 30, 0),
                IsActive = true
            },
             new Book
             {
                 BookID = 3,
                 ISBN = "9780743273565",
                 Title = "The Great Gatsby",
                 Author = "F. Scott Fitzgerald",
                 GenreID = 1, // Fiction
                 Description = "A novel by F. Scott Fitzgerald, 'The Great Gatsby' is a classic of modern American literature, published in 1925. It details the decadent Jazz Age through the eyes of Nick Carraway, who recounts the enigmatic millionaire Jay Gatsby's opulent life and tragic pursuit of Daisy Buchanan. The novel explores themes of wealth, idealism, social upheaval, and the American Dream.",
                 Price = 350.00M,
                 StockQuantity = 60,
                 MinStockLevel = 20,
                 PageCount = 180,
                 Language = "English",
                 PublishedDate = 1925,
                 Publisher = "Charles Scribner's Sons",
                 CoverImageURL = "https://raw.githubusercontent.com/Sumeet-162/eCommerce.Net/refs/heads/main/images/the%20power.jpg",
                 CreatedAt = new DateTime(2023, 1, 3, 12, 45, 0),
                 IsActive = true
             },
            new Book
            {
                BookID = 4,
                ISBN = "9780547928227",
                Title = "The Hobbit",
                Author = "J.R.R. Tolkien",
                GenreID = 3, // Fantasy
                Description = "J.R.R. Tolkien's 'The Hobbit,' published in 1937, is a fantastical journey following Bilbo Baggins, a hobbit who joins a quest with a group of dwarves to reclaim their treasure from the dragon Smaug. This beloved children's fantasy novel introduces readers to the rich world of Middle-earth, serving as a prequel to 'The Lord of the Rings' and filled with adventures, riddles, and magical creatures.",
                Price = 450.00M,
                StockQuantity = 80,
                MinStockLevel = 20,
                PageCount = 310,
                Language = "English",
                PublishedDate = 1937,
                Publisher = "George Allen & Unwin",
                CoverImageURL = "https://raw.githubusercontent.com/Sumeet-162/eCommerce.Net/refs/heads/main/images/the%20power.jpg",
                CreatedAt = new DateTime(2023, 1, 4, 13, 15, 0),
                IsActive = true
            }
        );
    }
}