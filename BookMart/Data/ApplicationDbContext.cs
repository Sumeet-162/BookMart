// BookMart/Data/ApplicationDbContext.cs
using BookMart.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic; // Required for ICollection if not already there

namespace BookMart.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Book> Books { get; set; }
        public DbSet<Genre> Genres { get; set; }
        public DbSet<StockTransaction> StockTransactions { get; set; }

        public DbSet<UserAddress> UserAddresses { get; set; }
        public DbSet<Carts> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // === Existing User configurations ===
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // === Book configurations ===
            modelBuilder.Entity<Book>()
                .HasIndex(b => b.ISBN)
                .IsUnique();

            // === Cart and CartItem Relationships (NEW ADDITIONS) ===

            // Cart to User (one-to-many)
            modelBuilder.Entity<Carts>()
                .HasOne(c => c.User)           // A Cart has one User
                .WithMany(u => u.Carts)        // A User can have many Carts
                .HasForeignKey(c => c.UserID)  // Foreign key in Cart points to UserID
                .OnDelete(DeleteBehavior.Cascade); // If a User is deleted, their Carts are also deleted

            // CartItem to Cart (many-to-one)
            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.Cart)         // A CartItem belongs to one Cart
                .WithMany(c => c.CartItems)    // A Cart can have many CartItems
                .HasForeignKey(ci => ci.CartID) // Foreign key in CartItem points to CartID
                .OnDelete(DeleteBehavior.Cascade); // If a Cart is deleted, its CartItems are also deleted

            // CartItem to Book (many-to-one)
            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.Book)         // A CartItem refers to one Book
                .WithMany()                    // Book does not necessarily need a navigation property back to CartItem
                .HasForeignKey(ci => ci.BookID) // Foreign key in CartItem points to BookID
                .OnDelete(DeleteBehavior.Restrict); // Prevent deleting a book if it's referenced in a CartItem
                                                    // Consider DeleteBehavior.NoAction if Restrict causes issues with circular dependencies.

            // === Order and OrderItem Relationships (Good to define if not already) ===
            // Assuming your Order and OrderItem models have navigation properties
            modelBuilder.Entity<Order>()
                .HasOne(o => o.User)
                .WithMany() // Or WithMany(u => u.Orders) if User has an Orders collection
                .HasForeignKey(o => o.UserID)
                .OnDelete(DeleteBehavior.Restrict); // Or Cascade, depending on your business logic

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(oi => oi.OrderID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Book)
                .WithMany()
                .HasForeignKey(oi => oi.BookID)
                .OnDelete(DeleteBehavior.Restrict);


            // === Seed initial Genres (CORRECTED: using static DateTime) ===
            modelBuilder.Entity<Genre>().HasData(
                new Genre { GenreID = 1, Name = "Fiction", CreatedAt = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Genre { GenreID = 2, Name = "Science Fiction", CreatedAt = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Genre { GenreID = 3, Name = "Fantasy", CreatedAt = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Genre { GenreID = 4, Name = "Mystery", CreatedAt = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Genre { GenreID = 5, Name = "Thriller", CreatedAt = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Genre { GenreID = 6, Name = "Romance", CreatedAt = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Genre { GenreID = 7, Name = "Biography", CreatedAt = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Genre { GenreID = 8, Name = "History", CreatedAt = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Genre { GenreID = 9, Name = "Self-Help", CreatedAt = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Genre { GenreID = 10, Name = "Cookbook", CreatedAt = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
            );

            // === Seed initial Books (already using static DateTime, which is good) ===
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
                    CoverImageURL = "https://raw.githubusercontent.com/Sumeet-162/eCommerce.Net/refs/heads/main/images/to%20kill.jpg",
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
                    CoverImageURL = "https://raw.githubusercontent.com/Sumeet-162/eCommerce.Net/refs/heads/main/images/the%20power.jpg",
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
}
