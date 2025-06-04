using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BookMart.Data;
using BookMart.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Claims;
using System;

namespace BookMart.Controllers
{
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UserController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Helper method to get or create a UserId for the current session
        private async Task<int> GetOrCreateUserId()
        {
            if (User.Identity.IsAuthenticated)
            {
                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(userIdString, out int authenticatedUserId))
                {
                    return authenticatedUserId;
                }
            }

            string? userIdFromSession = HttpContext.Session.GetString("CurrentUserId");
            int currentGuestUserId;

            if (!string.IsNullOrEmpty(userIdFromSession) && int.TryParse(userIdFromSession, out currentGuestUserId))
            {
                var guestUserExists = await _context.Users.AnyAsync(u => u.UserID == currentGuestUserId);
                if (guestUserExists)
                {
                    return currentGuestUserId;
                }
            }

            var newGuestUser = new User
            {
                Username = $"guest_{Guid.NewGuid().ToString().Substring(0, 8)}",
                Email = $"guest_{Guid.NewGuid().ToString().Substring(0, 8)}@example.com",
                PasswordHash = "dummy_hashed_password",
                FirstName = "Guest",
                LastName = "User",
                Phone = "0000000000",
                CreatedAt = DateTime.Now,
                IsAdmin = false
            };

            _context.Users.Add(newGuestUser);
            await _context.SaveChangesAsync();

            HttpContext.Session.SetString("CurrentUserId", newGuestUser.UserID.ToString());

            TempData["InfoMessage"] = "A guest user profile was created for your session. Please log in for a personalized experience.";

            return newGuestUser.UserID;
        }


        // --- UPDATED: UserHome action to handle search and genre filtering ---
        public async Task<IActionResult> UserHome(string? searchQuery, int? genreId)
        {
            var viewModel = new HomeViewModel();

            // Fetch all genres for the layout's dropdown
            ViewBag.Genres = await _context.Genres.OrderBy(g => g.Name).ToListAsync();

            // Start with a base query for active books
            var booksQuery = _context.Books.Where(b => b.IsActive).AsQueryable();

            // Apply search filter if a query is provided
            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                booksQuery = booksQuery.Where(b =>
                    b.Title.Contains(searchQuery) ||
                    b.Author.Contains(searchQuery) ||
                    b.Description.Contains(searchQuery) // You can add more fields to search
                );
            }

            // Apply genre filter if a genreId is provided
            if (genreId.HasValue && genreId.Value > 0)
            {
                booksQuery = booksQuery.Where(b => b.GenreID == genreId.Value);
            }

            // Populate Featured Books (filtered)
            viewModel.FeaturedBooks = await booksQuery
                                            .Where(b => b.DiscountedPrice.HasValue)
                                            .Take(4)
                                            .ToListAsync();

            // Populate New Arrivals (filtered)
            viewModel.NewArrivals = await booksQuery
                                            .OrderByDescending(b => b.CreatedAt)
                                            .Take(4)
                                            .ToListAsync();

            // Populate Popular Authors (filtered, but grouping might need adjustment if filtered set is too small)
            viewModel.PopularAuthors = await booksQuery
                                            .Include(b => b.Genre)
                                            .GroupBy(b => b.Author)
                                            .Select(g => new AuthorViewModel
                                            {
                                                AuthorName = g.Key,
                                                Genre = g.First().Genre != null ? g.First().Genre.Name : "N/A"
                                            })
                                            .Take(4)
                                            .ToListAsync();

            ViewData["Title"] = "BookMart - Your Online Bookstore";
            return View(viewModel);
        }
        // --- END UPDATED UserHome action ---


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int bookId, int quantity = 1)
        {
            int userId = await GetOrCreateUserId();

            var book = await _context.Books.FindAsync(bookId);
            if (book == null)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Book not found!" });
                }
                TempData["ErrorMessage"] = "Book not found!";
                return RedirectToAction("UserHome");
            }

            var cart = await _context.Carts
                            .Include(c => c.CartItems)
                            .FirstOrDefaultAsync(c => c.UserID == userId);

            if (cart == null)
            {
                cart = new Carts
                {
                    UserID = userId,
                    CreatedAt = DateTime.Now,
                    CartItems = new List<CartItem>()
                };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            var existingCartItem = cart.CartItems?
                              .FirstOrDefault(ci => ci.BookID == bookId);

            if (existingCartItem != null)
            {
                existingCartItem.Quantity += quantity;
                existingCartItem.Price = book.DiscountedPrice ?? book.Price;
                _context.CartItems.Update(existingCartItem);
            }
            else
            {
                var newCartItem = new CartItem
                {
                    CartID = cart.CartID,
                    BookID = book.BookID,
                    Quantity = quantity,
                    Price = book.DiscountedPrice ?? book.Price
                };
                _context.CartItems.Add(newCartItem);
            }

            cart.UpdatedAt = DateTime.Now;
            _context.Carts.Update(cart);

            await _context.SaveChangesAsync();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, message = $"{book.Title} added to cart successfully!" });
            }

            TempData["SuccessMessage"] = $"{book.Title} added to cart successfully!";
            return RedirectToAction("UserHome");
        }

        public async Task<IActionResult> UserCart()
        {
            ViewData["Title"] = "My Cart";

            int userId = await GetOrCreateUserId();

            var cart = await _context.Carts
                                     .Include(c => c.CartItems!)
                                         .ThenInclude(ci => ci.Book)
                                             .ThenInclude(b => b.Genre)
                                     .FirstOrDefaultAsync(c => c.UserID == userId);

            if (cart == null || cart.CartItems == null)
            {
                return View(new List<CartItem>());
            }

            return View(cart.CartItems.ToList());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCartItemQuantity([FromBody] CartUpdateModel model)
        {
            int userId = await GetOrCreateUserId();

            if (model == null)
            {
                return Json(new { success = false, message = "Invalid request data." });
            }

            var cart = await _context.Carts.Include(c => c.CartItems)
                                         .FirstOrDefaultAsync(c => c.UserID == userId);

            if (cart == null)
            {
                return Json(new { success = false, message = "Cart not found." });
            }

            var cartItem = cart.CartItems?.FirstOrDefault(ci => ci.BookID == model.BookId);
            if (cartItem == null)
            {
                return Json(new { success = false, message = "Item not found in cart." });
            }

            int newQuantity = cartItem.Quantity + model.QuantityChange;

            if (newQuantity <= 0)
            {
                _context.CartItems.Remove(cartItem);
            }
            else
            {
                var book = await _context.Books.FindAsync(model.BookId);
                if (book != null && newQuantity > book.StockQuantity)
                {
                    return Json(new { success = false, message = $"Not enough stock for {book.Title}. Max available: {book.StockQuantity}" });
                }

                cartItem.Quantity = newQuantity;
                _context.CartItems.Update(cartItem);
            }

            cart.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Cart updated successfully." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveCartItem([FromBody] CartRemoveModel model)
        {
            int userId = await GetOrCreateUserId();

            if (model == null)
            {
                return Json(new { success = false, message = "Invalid request data." });
            }

            var cart = await _context.Carts.Include(c => c.CartItems)
                                         .FirstOrDefaultAsync(c => c.UserID == userId);

            if (cart == null)
            {
                return Json(new { success = false, message = "Cart not found." });
            }

            var cartItem = cart.CartItems?.FirstOrDefault(ci => ci.BookID == model.BookId);
            if (cartItem == null)
            {
                return Json(new { success = false, message = "Item not found in cart." });
            }

            _context.CartItems.Remove(cartItem);
            cart.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Item removed from cart." });
        }

        public async Task<IActionResult> UserProfile()
        {
            ViewData["Title"] = "My Profile";

            int userId = await GetOrCreateUserId();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == userId);

            if (user == null)
            {
                TempData["ErrorMessage"] = "User profile not found. Please log in.";
                return RedirectToAction("Login", "Account");
            }

            return View(user);
        }

        public IActionResult UserOrder()
        {
            ViewData["Title"] = "My Orders";
            return View();
        }

        public async Task<IActionResult> UserViewBookDetails(int id)
        {
            var book = await _context.Books
                                     .Include(b => b.Genre)
                                     .FirstOrDefaultAsync(b => b.BookID == id && b.IsActive);

            if (book == null)
            {
                return NotFound();
            }

            ViewData["Title"] = book.Title;
            return View(book);
        }
    }

    public class CartUpdateModel
    {
        public int BookId { get; set; }
        public int QuantityChange { get; set; }
    }

    public class CartRemoveModel
    {
        public int BookId { get; set; }
    }
}
