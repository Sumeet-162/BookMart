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
using Microsoft.AspNetCore.Http; // Required for HttpContext.Session

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

        public async Task<IActionResult> UserHome(string? searchQuery, int? genreId)
        {
            var viewModel = new HomeViewModel();

            // Load genres for the dropdown
            ViewBag.Genres = await _context.Genres.OrderBy(g => g.Name).ToListAsync();

            viewModel.SearchQuery = searchQuery;
            viewModel.GenreId = genreId;

            if (genreId.HasValue && genreId.Value > 0)
            {
                var genre = await _context.Genres.FirstOrDefaultAsync(g => g.GenreID == genreId.Value);
                viewModel.GenreName = genre?.Name;
            }

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
            }

            // Get search results with ordering
            viewModel.SearchResults = await booksQuery
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
            viewModel.TotalBooks = await booksQuery.CountAsync();
            viewModel.IsFiltered = !string.IsNullOrWhiteSpace(searchQuery) || genreId.HasValue;

            // Only load featured and new arrivals if not filtering
            if (!viewModel.IsFiltered)
            {
                // Load featured books (books with highest discounts)
                viewModel.FeaturedBooks = await _context.Books
                    .Where(b => b.IsActive && b.DiscountedPrice.HasValue)
                    .Include(b => b.Genre)
                    .OrderByDescending(b => (b.Price - b.DiscountedPrice) / b.Price)
                    .Take(4)
                    .ToListAsync();

                // Load new arrivals
                viewModel.NewArrivals = await _context.Books
                    .Where(b => b.IsActive)
                    .Include(b => b.Genre)
                    .OrderByDescending(b => b.CreatedAt)
                    .Take(4)
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
                    .Take(4)
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

            ViewData["Title"] = book.Title;
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
            var cart = await _context.Carts // Changed from _context.Carts to _context.Carts
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
            model.TotalAmount = model.SubTotal + model.ShippingCost + model.TaxAmount;
            model.CartItems = cart.CartItems.ToList(); // Ensure cart items are part of the model to pass

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
                TempData["CheckoutViewModel"] = serializedModel; // Keep for next post
            }

            if (model == null || !model.CartItems.Any())
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
                TempData["CheckoutViewModel"] = serializedModel; // Keep for next post
            }

            if (model == null || !model.CartItems.Any())
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
                TempData["CheckoutViewModel"] = serializedModel; // Keep for next post
            }

            if (model == null || !model.CartItems.Any())
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
                TempData["CheckoutViewModel"] = serializedModel; // Keep for next post
            }

            if (model == null || !model.CartItems.Any())
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

                // Map shipping address details from the received CheckoutViewModel
                ShippingFirstName = model.ShippingFirstName,
                ShippingLastName = model.ShippingLastName,
                ShippingAddressLine1 = model.ShippingAddressLine1,
                ShippingAddressLine2 = model.ShippingAddressLine2,
                ShippingCity = model.ShippingCity,
                ShippingState = model.ShippingState,
                ShippingPinCode = model.ShippingPinCode,
                ShippingPhone = model.ShippingPhone,
                ShippingEmail = model.ShippingEmail,
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
            int pageSize = 8; // Number of books to load per page
            var booksQuery = _context.Books
                .Where(b => b.IsActive)
                .Include(b => b.Genre)
                .OrderByDescending(b => b.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize + 1); // Take one extra to check if there are more

            var books = await booksQuery.ToListAsync();
            bool hasMore = books.Count > pageSize;
            if (hasMore)
            {
                books = books.Take(pageSize).ToList(); // Remove the extra book
            }

            var bookData = books.Select(b => new
            {
                bookID = b.BookID,
                title = b.Title,
                author = b.Author,
                coverImageURL = b.CoverImageURL,
                price = b.Price,
                discountedPrice = b.DiscountedPrice
            });

            return Json(new { success = true, books = bookData, hasMore = hasMore });
        }

        [HttpGet]
        public async Task<IActionResult> GetMoreFeaturedBooks(int page = 1)
        {
            int pageSize = 8;
            var booksQuery = _context.Books
                .Where(b => b.IsActive && b.DiscountedPrice.HasValue)
                .Include(b => b.Genre)
                .OrderByDescending(b => (b.Price - b.DiscountedPrice) / b.Price) // Sort by highest discount percentage
                .Skip((page - 1) * pageSize)
                .Take(pageSize + 1); // Take one extra to check if there are more

            var books = await booksQuery.ToListAsync();
            bool hasMore = books.Count > pageSize;
            if (hasMore)
            {
                books = books.Take(pageSize).ToList();
            }

            var bookData = books.Select(b => new
            {
                bookID = b.BookID,
                title = b.Title,
                author = b.Author,
                coverImageURL = b.CoverImageURL,
                price = b.Price,
                discountedPrice = b.DiscountedPrice,
                genreName = b.Genre?.Name
            });

            return Json(new { success = true, books = bookData, hasMore = hasMore });
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
