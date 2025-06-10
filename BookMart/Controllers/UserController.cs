using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BookMart.Data;
using BookMart.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Claims;
using System;
using Newtonsoft.Json; // Required for serializing/deserializing complex objects to TempData
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http; // Required for HttpContext.Session
using Microsoft.Extensions.Logging; // Required for ILogger

namespace BookMart.Controllers
{
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserController> _logger;

        public UserController(ApplicationDbContext context, ILogger<UserController> logger)
        {
            _context = context;
            _logger = logger;
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

        public async Task<IActionResult> UserHome(string? searchQuery, int? genreId, string? genre)
        {
            var viewModel = new HomeViewModel();

            // Load genres for the dropdown
            ViewBag.Genres = await _context.Genres.OrderBy(g => g.Name).ToListAsync();

            viewModel.SearchQuery = searchQuery;
            viewModel.GenreId = genreId;

            var booksQuery = _context.Books
                                    .Where(b => b.IsActive)
                                    .Include(b => b.Genre)
                                    .AsQueryable();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                booksQuery = booksQuery.Where(b =>
                    EF.Functions.Like(b.Title, $"%{searchQuery}%") ||
                    EF.Functions.Like(b.Author, $"%{searchQuery}%") ||
                    (b.Description != null && EF.Functions.Like(b.Description, $"%{searchQuery}%"))
                );
            }

            // Apply genre filter
            if (genreId.HasValue && genreId.Value > 0)
            {
                booksQuery = booksQuery.Where(b => b.GenreID == genreId.Value);
                var genreName = await _context.Genres
                    .Where(g => g.GenreID == genreId.Value)
                    .Select(g => g.Name)
                    .FirstOrDefaultAsync();
                viewModel.GenreName = genreName;
            }
            else if (!string.IsNullOrWhiteSpace(genre))
            {
                booksQuery = booksQuery.Where(b => b.Genre.Name == genre);
                viewModel.GenreName = genre;
            }

            // Get search results
            viewModel.SearchResults = await booksQuery
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
            viewModel.TotalBooks = await booksQuery.CountAsync();
            viewModel.IsFiltered = !string.IsNullOrWhiteSpace(searchQuery) || genreId.HasValue || !string.IsNullOrWhiteSpace(genre);

            // Only load featured and regular books if not filtering
            if (!viewModel.IsFiltered)
            {
                // Load all active books for explore section
                viewModel.Books = await _context.Books
                    .Where(b => b.IsActive)
                    .Include(b => b.Genre)
                    .OrderByDescending(b => b.CreatedAt)
                    .ToListAsync();

                // Load all featured books (books with discounts)
                viewModel.FeaturedBooks = await _context.Books
                    .Where(b => b.IsActive && b.DiscountedPrice.HasValue)
                    .Include(b => b.Genre)
                    .OrderByDescending(b => (b.Price - b.DiscountedPrice) / b.Price)
                    .ToListAsync();

                // Load popular authors
                viewModel.PopularAuthors = await _context.Books
                    .Where(b => b.IsActive)
                    .Include(b => b.Genre)
                    .GroupBy(b => b.Author)
                    .Select(g => new AuthorViewModel
                    {
                        AuthorName = g.Key,
                        Genre = g.First().Genre != null ? g.First().Genre.Name : "N/A"
                    })
                    .Take(8) // Keep top 8 authors
                    .ToListAsync();
            }

            ViewData["Title"] = "BookMart - Your Online Bookstore";
            return View(viewModel);
        }

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

            var cart = await _context.Carts // Changed from _context.Carts to _context.Carts
                                     .Include(c => c.CartItems!)
                                         .ThenInclude(ci => ci.Book)
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

            var cart = await _context.Carts.Include(c => c.CartItems) // Changed from _context.Carts to _context.Carts
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

            var cart = await _context.Carts.Include(c => c.CartItems) // Changed from _context.Carts to _context.Carts
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

        

        public async Task<IActionResult> UserOrder(string? statusFilter, string? timePeriodFilter)
        {
            ViewData["Title"] = "My Orders";

            int userId = await GetOrCreateUserId();

            var ordersQuery = _context.Orders
                                       .Where(o => o.UserID == userId)
                                       .Include(o => o.OrderItems!)
                                           .ThenInclude(oi => oi.Book)
                                       .OrderByDescending(o => o.OrderDate)
                                       .AsQueryable(); // Start building the query

            // Apply Status Filter
            if (!string.IsNullOrWhiteSpace(statusFilter) && statusFilter != "All Orders")
            {
                ordersQuery = ordersQuery.Where(o => o.OrderStatus == statusFilter);
            }

            // Apply Time Period Filter
            if (!string.IsNullOrWhiteSpace(timePeriodFilter))
            {
                DateTime cutoffDate = DateTime.MinValue; // Default to no time limit
                switch (timePeriodFilter)
                {
                    case "last30":
                        cutoffDate = DateTime.Now.AddDays(-30);
                        break;
                    case "last90":
                        cutoffDate = DateTime.Now.AddDays(-90);
                        break;
                    case "last180":
                        cutoffDate = DateTime.Now.AddDays(-180);
                        break;
                }
                if (cutoffDate != DateTime.MinValue)
                {
                    ordersQuery = ordersQuery.Where(o => o.OrderDate >= cutoffDate);
                }
            }

            var orders = await ordersQuery.ToListAsync();

            // Pass current filter selections to the view to maintain state
            ViewBag.SelectedStatusFilter = statusFilter;
            ViewBag.SelectedTimePeriodFilter = timePeriodFilter;

            return View(orders);
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

            // Get similar books from the same genre, excluding the current book
            var similarBooks = await _context.Books
                .Where(b => b.IsActive 
                           && b.GenreID == book.GenreID 
                           && b.BookID != book.BookID)
                .Take(4)  // Get 4 similar books
                .ToListAsync();

            ViewData["Title"] = book.Title;
            ViewData["SimilarBooks"] = similarBooks;
            
            return View(book);
        }

        // --- UserCheckout (GET) action to display the checkout form ---
        public async Task<IActionResult> UserCheckout()
        {
            ViewData["Title"] = "Checkout";

            int userId = await GetOrCreateUserId();

            var cart = await _context.Carts // Changed from _context.Carts to _context.Carts
                                     .Include(c => c.CartItems!)
                                         .ThenInclude(ci => ci.Book)
                                     .FirstOrDefaultAsync(c => c.UserID == userId);

            if (cart == null || !cart.CartItems!.Any())
            {
                TempData["ErrorMessage"] = "Your cart is empty. Please add items before checking out.";
                return RedirectToAction("UserCart");
            }

            decimal subtotal = cart.CartItems.Sum(item => item.Quantity * item.Price);
            decimal shippingCost = 0M;
            decimal taxRate = 0.05M;
            decimal taxAmount = subtotal * taxRate;
            decimal totalAmount = subtotal + shippingCost + taxAmount;

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == userId);

            var viewModel = new CheckoutViewModel
            {
                CartItems = cart.CartItems.ToList(),
                SubTotal = subtotal,
                ShippingCost = shippingCost,
                TaxAmount = taxAmount,
                TotalAmount = totalAmount,
                ShippingFirstName = user?.FirstName ?? string.Empty,
                ShippingLastName = user?.LastName ?? string.Empty,
                ShippingEmail = user?.Email ?? string.Empty,
                ShippingPhone = user?.Phone ?? string.Empty,
                // Ensure other address fields are empty or loaded from user's default address if available
                ShippingAddressLine1 = "", // Initialize as empty or from user profile
                ShippingAddressLine2 = "",
                ShippingCity = "",
                ShippingState = "",
                ShippingPinCode = ""
            };

            return View(viewModel);
        }
        // --- END UserCheckout (GET) ---

        // --- ProcessCheckout (POST) action - Stores CheckoutViewModel in TempData and redirects ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessCheckout(CheckoutViewModel model)
        {
            ViewData["Title"] = "Checkout";

            int userId = await GetOrCreateUserId();

            // Re-fetch cart items to ensure current data and prevent client-side tampering
            var cart = await _context.Carts
                                     .Include(c => c.CartItems!)
                                         .ThenInclude(ci => ci.Book)
                                     .FirstOrDefaultAsync(c => c.UserID == userId);

            if (cart == null || !cart.CartItems!.Any())
            {
                TempData["ErrorMessage"] = "Your cart is empty or expired. Please add items before checking out.";
                return RedirectToAction("UserCart");
            }

            // Recalculate totals on the server-side
            model.SubTotal = cart.CartItems.Sum(item => item.Quantity * item.Price);
            model.ShippingCost = 0M; // Assuming free shipping
            model.TaxAmount = model.SubTotal * 0.05M;
            model.CartItems = cart.CartItems.ToList();

            // Coupon logic
            string? couponCode = null;
            decimal couponDiscount = 0;
            if (!string.IsNullOrWhiteSpace(model.CouponCode) && model.CouponCode.ToUpper() == "COGNIZANT")
            {
                couponCode = model.CouponCode.ToUpper();
                couponDiscount = Math.Round(model.SubTotal * 0.10M, 0, MidpointRounding.AwayFromZero);
            }
            model.TotalAmount = model.SubTotal + model.ShippingCost + model.TaxAmount - couponDiscount;

            // Validate the incoming model (address fields)
            if (!ModelState.IsValid)
            {
                return View("UserCheckout", model);
            }

            // Store the entire CheckoutViewModel in TempData.
            TempData["CheckoutViewModel"] = JsonConvert.SerializeObject(model);

            // Redirect to the payment selection page. Order is NOT saved yet.
            return RedirectToAction("UserPaymentSelection", "User");
        }
        // --- END ProcessCheckout (POST) ---

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(User model)
        {
            int userId = await GetOrCreateUserId();

            if (userId != model.UserID)
            {
                return Json(new { success = false, message = "Unauthorized profile update attempt." });
            }

            var userToUpdate = await _context.Users.FirstOrDefaultAsync(u => u.UserID == userId);

            if (userToUpdate == null)
            {
                return Json(new { success = false, message = "User not found." });
            }

            userToUpdate.FirstName = model.FirstName;
            userToUpdate.LastName = model.LastName;
            userToUpdate.Email = model.Email;
            userToUpdate.Phone = model.Phone;

            try
            {
                _context.Users.Update(userToUpdate);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Profile updated successfully!" });
            }
            catch (DbUpdateConcurrencyException)
            {
                return Json(new { success = false, message = "Concurrency error. Please try again." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error updating profile: {ex.Message}" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmNewPassword)
        {
            int userId = await GetOrCreateUserId();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == userId);

            if (user == null)
            {
                return Json(new { success = false, message = "User not found." });
            }

            if (string.IsNullOrEmpty(currentPassword) || !BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
            {
                return Json(new { success = false, message = "Incorrect current password." });
            }

            if (newPassword != confirmNewPassword)
            {
                return Json(new { success = false, message = "New password and confirmation do not match." });
            }
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            {
                return Json(new { success = false, message = "New password must be at least 6 characters long." });
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);

            try
            {
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Password changed successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error changing password: {ex.Message}" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccount(string confirmationText)
        {
            int userId = await GetOrCreateUserId();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == userId);

            if (user == null)
            {
                // If user is already not found (e.g., deleted by another session), just report success
                return Json(new { success = true, message = "User already deleted or not found. Redirecting to login." });
            }

            if (confirmationText?.ToLower() != "delete my account")
            {
                return Json(new { success = false, message = "Please type 'delete my account' to confirm account deletion." });
            }

            try
            {
                // Delete related data first (this order is important for foreign key constraints)
                var userCart = await _context.Carts.FirstOrDefaultAsync(c => c.UserID == userId);
                if (userCart != null)
                {
                    _context.CartItems.RemoveRange(_context.CartItems.Where(ci => ci.CartID == userCart.CartID));
                    _context.Carts.Remove(userCart);
                }
                var userOrders = await _context.Orders.Where(o => o.UserID == userId).ToListAsync();
                foreach (var order in userOrders)
                {
                    _context.OrderItems.RemoveRange(_context.OrderItems.Where(oi => oi.OrderID == order.OrderID));
                }
                _context.Orders.RemoveRange(userOrders);

                // Ensure UserAddresses deletion:
                _context.UserAddresses.RemoveRange(_context.UserAddresses.Where(a => a.UserID == userId));

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                // *** IMPORTANT: Sign out the user's authentication cookie server-side ***
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                // Clear session data associated with the user
                HttpContext.Session.Clear();

                return Json(new { success = true, message = "Your account has been successfully deleted. Redirecting..." });
            }
            catch (Exception ex)
            {
                // Log the exception (ex) in a real application
                return Json(new { success = false, message = $"Error deleting account: {ex.Message}" });
            }
        }

        // --- UserPaymentSelection (GET) action - Retrieves data from TempData and keeps it alive ---
        public async Task<IActionResult> UserPaymentSelection()
        {
            ViewData["Title"] = "Payment Selection";

            int userId = await GetOrCreateUserId(); // Ensure user context is established

            CheckoutViewModel? model = null;

            // Retrieve the serialized CheckoutViewModel from TempData using Peek
            // Peek keeps the data in TempData for subsequent requests in the same session.
            if (TempData.ContainsKey("CheckoutViewModel") && TempData["CheckoutViewModel"] is string serializedModel)
            {
                model = JsonConvert.DeserializeObject<CheckoutViewModel>(serializedModel);
                // Important: If you want to keep it for *another* redirect (e.g. to a specific payment page),
                // you must re-add it or use TempData.Keep() after deserializing.
                // TempData.Keep() makes it persist for one more request.
                // Or, if this is the only path that consumes it, we can just let it get consumed on the POST.
                // For a multi-step flow like this, it's safer to re-add.
                TempData["CheckoutViewModel"] = serializedModel; // Re-add to persist for next step
            }

            if (model == null || !model.CartItems.Any())
            {
                TempData["ErrorMessage"] = "Please complete the checkout address details first.";
                return RedirectToAction("UserCheckout");
            }

            // Although model already has calculated totals, re-verify from cart if needed for absolute latest data
            // For simplicity, we'll trust the model passed from ProcessCheckout for display here.
            // Final recalculation will be in ProcessPayment.

            return View(model); // Pass the CheckoutViewModel to the payment selection view
        }
        // --- END UserPaymentSelection (GET) ---

        // --- New Payment-Specific GET actions to display their respective forms ---

        public async Task<IActionResult> UserCardPayment()
        {
            ViewData["Title"] = "Card Payment";
            CheckoutViewModel? model = null;

            if (TempData.ContainsKey("CheckoutViewModel") && TempData["CheckoutViewModel"] is string serializedModel)
            {
                model = JsonConvert.DeserializeObject<CheckoutViewModel>(serializedModel);
                TempData["CheckoutViewModel"] = serializedModel;

                // Validate cart items are still available
                int userId = await GetOrCreateUserId();
                var cart = await _context.Carts
                    .Include(c => c.CartItems!)
                    .ThenInclude(ci => ci.Book)
                    .FirstOrDefaultAsync(c => c.UserID == userId);

                if (cart == null || !cart.CartItems!.Any())
                {
                    TempData["ErrorMessage"] = "Invalid checkout state. Please restart the checkout process.";
                    return RedirectToAction("UserCheckout");
                }
            }

            if (model == null)
            {
                TempData["ErrorMessage"] = "Invalid checkout state. Please restart the checkout process.";
                return RedirectToAction("UserCheckout");
            }
            return View(model);
        }

        public async Task<IActionResult> UserUpiPayment()
        {
            ViewData["Title"] = "UPI Payment";
            CheckoutViewModel? model = null;

            if (TempData.ContainsKey("CheckoutViewModel") && TempData["CheckoutViewModel"] is string serializedModel)
            {
                model = JsonConvert.DeserializeObject<CheckoutViewModel>(serializedModel);
                TempData["CheckoutViewModel"] = serializedModel;

                // Validate cart items are still available
                int userId = await GetOrCreateUserId();
                var cart = await _context.Carts
                    .Include(c => c.CartItems!)
                    .ThenInclude(ci => ci.Book)
                    .FirstOrDefaultAsync(c => c.UserID == userId);

                if (cart == null || !cart.CartItems!.Any())
                {
                    TempData["ErrorMessage"] = "Invalid checkout state. Please restart the checkout process.";
                    return RedirectToAction("UserCheckout");
                }
            }

            if (model == null)
            {
                TempData["ErrorMessage"] = "Invalid checkout state. Please restart the checkout process.";
                return RedirectToAction("UserCheckout");
            }
            return View(model);
        }

        public async Task<IActionResult> usercod()
        {
            ViewData["Title"] = "Cash on Delivery";
            CheckoutViewModel? model = null;

            if (TempData.ContainsKey("CheckoutViewModel") && TempData["CheckoutViewModel"] is string serializedModel)
            {
                model = JsonConvert.DeserializeObject<CheckoutViewModel>(serializedModel);
                TempData["CheckoutViewModel"] = serializedModel;

                // Validate cart items are still available
                int userId = await GetOrCreateUserId();
                var cart = await _context.Carts
                    .Include(c => c.CartItems!)
                    .ThenInclude(ci => ci.Book)
                    .FirstOrDefaultAsync(c => c.UserID == userId);

                if (cart == null || !cart.CartItems!.Any())
                {
                    TempData["ErrorMessage"] = "Invalid checkout state. Please restart the checkout process.";
                    return RedirectToAction("UserCheckout");
                }
            }

            if (model == null)
            {
                TempData["ErrorMessage"] = "Invalid checkout state. Please restart the checkout process.";
                return RedirectToAction("UserCheckout");
            }
            return View(model);
        }

        public async Task<IActionResult> usernetonlinepayment()
        {
            ViewData["Title"] = "Net Banking Payment";
            CheckoutViewModel? model = null;

            if (TempData.ContainsKey("CheckoutViewModel") && TempData["CheckoutViewModel"] is string serializedModel)
            {
                model = JsonConvert.DeserializeObject<CheckoutViewModel>(serializedModel);
                TempData["CheckoutViewModel"] = serializedModel;

                // Validate cart items are still available
                int userId = await GetOrCreateUserId();
                var cart = await _context.Carts
                    .Include(c => c.CartItems!)
                    .ThenInclude(ci => ci.Book)
                    .FirstOrDefaultAsync(c => c.UserID == userId);

                if (cart == null || !cart.CartItems!.Any())
                {
                    TempData["ErrorMessage"] = "Invalid checkout state. Please restart the checkout process.";
                    return RedirectToAction("UserCheckout");
                }
            }

            if (model == null)
            {
                TempData["ErrorMessage"] = "Invalid checkout state. Please restart the checkout process.";
                return RedirectToAction("UserCheckout");
            }
            return View(model);
        }

        // --- ProcessPayment (POST) action - ORDER CREATION HAPPENS HERE ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessPayment(CheckoutViewModel model, string paymentMethod)
        {
            ViewData["Title"] = "Payment Confirmation";

            int userId = await GetOrCreateUserId();

            // IMPORTANT: Re-fetch cart items again to ensure current data and prevent client-side tampering.
            // This is the absolute last point of data validation before order creation.
            var cart = await _context.Carts // Changed from _context.Carts to _context.Carts
                                     .Include(c => c.CartItems!)
                                         .ThenInclude(ci => ci.Book)
                                     .FirstOrDefaultAsync(c => c.UserID == userId);

            if (cart == null || !cart.CartItems!.Any())
            {
                TempData["ErrorMessage"] = "Your cart is empty or expired. Please add items before checking out.";
                return RedirectToAction("UserCart");
            }

            // Recalculate totals on the server-side as the final source of truth for the order
            decimal subtotal = cart.CartItems.Sum(item => item.Quantity * item.Price);
            decimal shippingCost = 0M;
            decimal taxRate = 0.05M;
            decimal taxAmount = subtotal * taxRate;
            decimal totalAmount = subtotal + shippingCost + taxAmount;

            // Handle potential COD fee addition (ONLY if COD applies a fee)
            if (paymentMethod == "COD")
            {
                decimal codFee = 49M; // Define your COD fee
                shippingCost += codFee; // Add COD fee to shipping cost for this specific order
                totalAmount = subtotal + shippingCost + taxAmount; // Re-calculate total
            }

            // Coupon logic
            string? couponCode = null;
            decimal couponDiscount = 0;
            if (!string.IsNullOrWhiteSpace(model.CouponCode) && model.CouponCode.ToUpper() == "COGNIZANT")
            {
                couponCode = model.CouponCode.ToUpper();
                couponDiscount = Math.Round(subtotal * 0.10M, 0, MidpointRounding.AwayFromZero);
            }
            totalAmount = subtotal + shippingCost + taxAmount - couponDiscount;

            // Create the new Order entity now
            var order = new Order
            {
                UserID = userId,
                OrderDate = DateTime.Now,
                SubTotal = subtotal,
                ShippingCost = shippingCost, // This will include COD fee if applicable
                TaxAmount = taxAmount,
                TotalAmount = totalAmount, // This will be the final calculated total for the order
                PaymentMethod = paymentMethod, // Selected payment method
                PaymentStatus = (paymentMethod == "COD") ? "Pending" : "Paid", // COD is pending, others assume paid
                OrderStatus = "Pending", // Initial order status for all new orders
                ShippingFirstName = model.ShippingFirstName,
                ShippingLastName = model.ShippingLastName,
                ShippingAddressLine1 = model.ShippingAddressLine1,
                ShippingAddressLine2 = model.ShippingAddressLine2,
                ShippingCity = model.ShippingCity,
                ShippingState = model.ShippingState,
                ShippingPinCode = model.ShippingPinCode,
                ShippingPhone = model.ShippingPhone,
                ShippingEmail = model.ShippingEmail,
                CouponCode = couponCode,
                CouponDiscount = couponDiscount
            };

            // Add OrderItem entities from the current cart to the new Order
            order.OrderItems = new List<OrderItem>();
            foreach (var cartItem in cart.CartItems)
            {
                order.OrderItems.Add(new OrderItem
                {
                    BookID = cartItem.BookID,
                    Quantity = cartItem.Quantity,
                    Price = cartItem.Price // Price at the time of order
                });

                // Update book stock quantity
                var book = await _context.Books.FindAsync(cartItem.BookID);
                if (book != null)
                {
                    book.StockQuantity -= cartItem.Quantity;
                    _context.Books.Update(book);
                }
            }

            _context.Orders.Add(order); // Add the new order

            // Clear the user's cart after creating the order
            _context.CartItems.RemoveRange(cart.CartItems);
            _context.Carts.Remove(cart); // Changed from _context.Carts.Remove(cart) to _context.Carts.Remove(cart)

            await _context.SaveChangesAsync(); // Save all changes to the database

            // IMPORTANT: Clear TempData after successful order creation as the flow is complete
            TempData.Remove("CheckoutViewModel");

            TempData["SuccessMessage"] = $"Order #{order.OrderID} placed successfully with {paymentMethod}!";
            return RedirectToAction("userpaymentsuccesfull", "User", new { orderId = order.OrderID });
        }
        // --- END ProcessPayment (POST) ---

        // OrderConfirmation (GET) action to display final order details
        public async Task<IActionResult> userpaymentsuccesfull(int orderId)
        {
            ViewData["Title"] = "Order Confirmation";

            int userId = await GetOrCreateUserId();

            var order = await _context.Orders
                                      .Include(o => o.OrderItems!)
                                          .ThenInclude(oi => oi.Book)
                                      .FirstOrDefaultAsync(o => o.OrderID == orderId && o.UserID == userId);

            if (order == null)
            {
                TempData["ErrorMessage"] = "Order not found or you do not have permission to view it.";
                return RedirectToAction("UserOrder");
            }

            return View(order);
        }

        [HttpGet]
        public async Task<IActionResult> LoadMoreBooks(int page = 1)
        {
            try
            {
                int pageSize = 8;
                var query = _context.Books
                    .Where(b => b.IsActive)
                    .OrderByDescending(b => b.CreatedAt);

                var totalBooks = await query.CountAsync();
                var hasMore = (page * pageSize) < totalBooks;

                var books = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(b => new
                    {
                        bookID = b.BookID,
                        title = b.Title,
                        author = b.Author,
                        coverImageURL = b.CoverImageURL ?? "/images/default-book.jpg",
                        price = b.Price,
                        discountedPrice = b.DiscountedPrice
                    })
                    .ToListAsync();

                return Json(new { success = true, books, hasMore });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading more books");
                return Json(new { success = false, message = "Failed to load more books" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetMoreFeaturedBooks(int page = 1)
        {
            try
            {
                int pageSize = 8;
                var query = _context.Books
                    .Where(b => b.IsActive && b.DiscountedPrice.HasValue)
                    .OrderByDescending(b => (b.Price - b.DiscountedPrice!.Value) / b.Price);

                var totalBooks = await query.CountAsync();
                var hasMore = (page * pageSize) < totalBooks;

                var books = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(b => new
                    {
                        bookID = b.BookID,
                        title = b.Title,
                        author = b.Author,
                        coverImageURL = b.CoverImageURL ?? "/images/default-book.jpg",
                        price = b.Price,
                        discountedPrice = b.DiscountedPrice
                    })
                    .ToListAsync();

                return Json(new { success = true, books, hasMore });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading more featured books");
                return Json(new { success = false, message = "Failed to load more featured books" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder([FromBody] CancelOrderModel model)
        {
            try
            {
                int userId = await GetOrCreateUserId();
                
                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.OrderID == model.OrderId && o.UserID == userId);

                if (order == null)
                {
                    return Json(new { success = false, message = "Order not found" });
                }

                if (order.OrderStatus == "Cancelled")
                {
                    return Json(new { success = false, message = "Order is already cancelled" });
                }

                if (order.OrderStatus == "Delivered")
                {
                    return Json(new { success = false, message = "Cannot cancel a delivered order" });
                }

                // Restore book stock quantities
                foreach (var item in order.OrderItems)
                {
                    var book = await _context.Books.FindAsync(item.BookID);
                    if (book != null)
                    {
                        book.StockQuantity += item.Quantity;
                        _context.Books.Update(book);
                    }
                }

                // Update order status
                order.OrderStatus = "Cancelled";
                _context.Orders.Update(order);

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Order cancelled successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling order");
                return Json(new { success = false, message = "An error occurred while cancelling the order" });
            }
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

    public class CancelOrderModel
    {
        public int OrderId { get; set; }
    }
}

