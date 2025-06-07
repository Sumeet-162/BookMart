using BookMart.Data;
using BookMart.Models;
using BookMart.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BookMart.Controllers
{
    // [Authorize(Roles = "Admin")] // Consider adding this once authentication/authorization is setup
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(ApplicationDbContext context, ILogger<AdminController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // --- Dashboard & General Admin Operations ---

        // GET: /Admin/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            ViewData["Title"] = "Admin Dashboard";

            var viewModel = new AdminDashboardViewModel();

            try
            {
                // Calculate total orders and revenue
                viewModel.TotalOrders = await _context.Orders.CountAsync();
                viewModel.TotalRevenue = await _context.Orders
                    .Where(o => o.OrderStatus != "Cancelled")
                    .SumAsync(o => o.TotalAmount);

                // Calculate total books in stock
                viewModel.TotalBooksInStock = await _context.Books.SumAsync(b => b.StockQuantity);

                // Get active users count
                viewModel.ActiveUsers = await _context.Users.CountAsync();

                // Get recent orders
                viewModel.RecentOrders = await _context.Orders
                    .Include(o => o.OrderItems!)
                    .OrderByDescending(o => o.OrderDate)
                    .Take(5)
                    .ToListAsync();

                // Get top selling books
                var topBooks = await _context.OrderItems
                    .Include(oi => oi.Book)
                    .GroupBy(oi => new { oi.BookID, oi.Book!.Title, oi.Book.Author, oi.Book.CoverImageURL })
                    .Select(g => new BookSalesSummary
                    {
                        BookId = g.Key.BookID,
                        Title = g.Key.Title,
                        Author = g.Key.Author,
                        CoverImageURL = g.Key.CoverImageURL,
                        TotalSold = g.Sum(oi => oi.Quantity),
                        Revenue = g.Sum(oi => oi.Quantity * oi.Price)
                    })
                    .OrderByDescending(b => b.TotalSold)
                    .Take(5)
                    .ToListAsync();

                viewModel.TopSellingBooks = topBooks;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching dashboard data");
                TempData["ErrorMessage"] = "Error loading dashboard data.";
            }

            return View(viewModel);
        }

        // GET: /Admin/Orders
        public async Task<IActionResult> Orders(
            string? statusFilter,
            string? searchQuery,
            DateTime? startDate,
            DateTime? endDate,
            string? sortBy,
            bool isDescending = true,
            int page = 1)
        {
            ViewData["Title"] = "Manage Orders";

            var viewModel = new AdminOrdersViewModel
            {
                Filter = new OrdersFilterModel
                {
                    StatusFilter = statusFilter,
                    SearchQuery = searchQuery,
                    StartDate = startDate,
                    EndDate = endDate,
                    SortBy = sortBy,
                    IsDescending = isDescending
                },
                CurrentPage = page
            };

            try
            {
                // Start with base query
                var query = _context.Orders
                    .Include(o => o.OrderItems!)
                        .ThenInclude(oi => oi.Book)
                    .AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(statusFilter))
                {
                    query = query.Where(o => o.OrderStatus == statusFilter);
                }

                if (!string.IsNullOrEmpty(searchQuery))
                {
                    query = query.Where(o =>
                        o.OrderID.ToString().Contains(searchQuery) ||
                        o.ShippingFirstName.Contains(searchQuery) ||
                        o.ShippingLastName.Contains(searchQuery) ||
                        o.ShippingEmail.Contains(searchQuery)
                    );
                }

                if (startDate.HasValue)
                {
                    query = query.Where(o => o.OrderDate.Date >= startDate.Value.Date);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(o => o.OrderDate.Date <= endDate.Value.Date);
                }

                // Calculate summary stats
                viewModel.Stats = new OrdersSummaryStats
                {
                    TotalOrders = await query.CountAsync(),
                    PendingOrders = await query.CountAsync(o => o.OrderStatus == "Pending"),
                    ProcessingOrders = await query.CountAsync(o => o.OrderStatus == "Processing"),
                    ShippedOrders = await query.CountAsync(o => o.OrderStatus == "Shipped"),
                    DeliveredOrders = await query.CountAsync(o => o.OrderStatus == "Delivered"),
                    CancelledOrders = await query.CountAsync(o => o.OrderStatus == "Cancelled"),
                    TotalRevenue = await query.Where(o => o.OrderStatus != "Cancelled").SumAsync(o => o.TotalAmount)
                };

                // Apply sorting
                query = (sortBy?.ToLower(), isDescending) switch
                {
                    ("date", true) => query.OrderByDescending(o => o.OrderDate),
                    ("date", false) => query.OrderBy(o => o.OrderDate),
                    ("status", true) => query.OrderByDescending(o => o.OrderStatus),
                    ("status", false) => query.OrderBy(o => o.OrderStatus),
                    ("amount", true) => query.OrderByDescending(o => o.TotalAmount),
                    ("amount", false) => query.OrderBy(o => o.TotalAmount),
                    _ => query.OrderByDescending(o => o.OrderDate) // Default sorting
                };

                // Apply pagination
                int totalItems = await query.CountAsync();
                viewModel.TotalPages = (int)Math.Ceiling(totalItems / (double)viewModel.PageSize);
                viewModel.CurrentPage = Math.Min(Math.Max(1, page), viewModel.TotalPages);

                viewModel.Orders = await query
                    .Skip((viewModel.CurrentPage - 1) * viewModel.PageSize)
                    .Take(viewModel.PageSize)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching orders data");
                TempData["ErrorMessage"] = "Error loading orders data.";
            }

            return View(viewModel);
        }

        // POST: /Admin/UpdateOrderStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrderStatus([FromBody] UpdateOrderStatusModel model)
        {
            try
            {
                if (model == null)
                {
                    return Json(new { success = false, message = "Invalid request data." });
                }

                var order = await _context.Orders.FindAsync(model.OrderId);
                if (order == null)
                {
                    return Json(new { success = false, message = "Order not found." });
                }

                order.OrderStatus = model.NewStatus;
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = $"Order #{model.OrderId} status updated to {model.NewStatus}." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status for order ID {OrderId}", model.OrderId);
                return Json(new { success = false, message = "Error updating order status." });
            }
        }

        // GET: /Admin/AdminOperations (Placeholder for other admin tasks)
        public IActionResult AdminOperations()
        {
            ViewData["Title"] = "Admin Operations";
            return View();
        }

        // GET: Admin/Stock
        public async Task<IActionResult> Stock(int page = 1)
        {
            int pageSize = 15;
            var booksQuery = _context.Books
                .Include(b => b.Genre)
                .OrderBy(b => b.Title)
                .Select(b => new BookStockViewModel
                {
                    BookId = b.BookID,
                    Title = b.Title,
                    Author = b.Author,
                    GenreName = b.Genre != null ? b.Genre.Name : "N/A",
                    StockQuantity = b.StockQuantity,
                    MinStockLevel = b.MinStockLevel,
                    Price = b.Price,
                    CoverImageURL = b.CoverImageURL ?? "/images/placeholder.png"
                });

            int totalBooks = await booksQuery.CountAsync();
            int totalPages = (int)Math.Ceiling(totalBooks / (double)pageSize);
            page = Math.Max(1, Math.Min(page, totalPages == 0 ? 1 : totalPages));

            var pagedBooks = await booksQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            foreach (var book in pagedBooks)
            {
                if (book.StockQuantity <= 0)
                {
                    book.StockStatus = "Out of Stock";
                    book.StatusBadgeClass = "bg-danger text-danger";
                }
                else if (book.StockQuantity < book.MinStockLevel)
                {
                    book.StockStatus = "Low Stock";
                    book.StatusBadgeClass = "bg-warning text-warning";
                }
                else
                {
                    book.StockStatus = "In Stock";
                    book.StatusBadgeClass = "bg-success text-success";
                }
            }

            ViewData["Title"] = "Stock Management";
            var viewModel = new StockPagedViewModel
            {
                Books = pagedBooks,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalBooks = totalBooks
            };
            return View(viewModel);
        }

        // GET: Admin/UpdateStock/5
        public async Task<IActionResult> UpdateStock(int? bookId)
        {
            if (bookId == null)
            {
                return NotFound();
            }

            var book = await _context.Books
                .Include(b => b.Genre)
                .FirstOrDefaultAsync(b => b.BookID == bookId);

            if (book == null)
            {
                return NotFound();
            }

            var viewModel = new UpdateStockViewModel
            {
                BookId = book.BookID,
                Title = book.Title,
                Author = book.Author,
                CoverImageURL = book.CoverImageURL ?? "/images/placeholder.png",
                GenreName = book.Genre?.Name,
                CurrentStockQuantity = book.StockQuantity,
                MinStockLevel = book.MinStockLevel,
                // OnOrderQuantity = await CalculateOnOrderQuantity(book.BookID) // Implement if needed
            };

            ViewData["Title"] = "Update Stock";
            return View(viewModel);
        }

        // POST: Admin/UpdateStock
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStock(UpdateStockViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Repopulate necessary fields if returning to view with errors
                var bookForDisplay = await _context.Books.Include(b => b.Genre).AsNoTracking().FirstOrDefaultAsync(b => b.BookID == model.BookId);
                if (bookForDisplay != null)
                {
                    model.Title = bookForDisplay.Title;
                    model.Author = bookForDisplay.Author;
                    model.CoverImageURL = bookForDisplay.CoverImageURL ?? "/images/placeholder.png";
                    model.GenreName = bookForDisplay.Genre?.Name;
                    model.MinStockLevel = bookForDisplay.MinStockLevel;
                    // model.OnOrderQuantity = await CalculateOnOrderQuantity(model.BookId);
                }
                ViewData["Title"] = "Update Stock";
                return View(model);
            }

            var book = await _context.Books.FindAsync(model.BookId);
            if (book == null)
            {
                TempData["ErrorMessage"] = "Book not found.";
                return RedirectToAction(nameof(Stock));
            }

            int previousQuantity = book.StockQuantity;
            int newQuantity = book.StockQuantity;

            switch (model.Operation.ToLower())
            {
                case "add":
                    newQuantity += model.Quantity;
                    break;
                case "remove":
                    newQuantity -= model.Quantity;
                    if (newQuantity < 0)
                    {
                        ModelState.AddModelError("Quantity", "Quantity to remove exceeds current stock. Stock cannot be negative.");
                        var bookForDisplay = await _context.Books.Include(b => b.Genre).AsNoTracking().FirstOrDefaultAsync(b => b.BookID == model.BookId);
                        if (bookForDisplay != null)
                        {
                            model.Title = bookForDisplay.Title;
                            model.Author = bookForDisplay.Author;
                            model.CoverImageURL = bookForDisplay.CoverImageURL ?? "/images/placeholder.png";
                            model.GenreName = bookForDisplay.Genre?.Name;
                            model.CurrentStockQuantity = bookForDisplay.StockQuantity; // Use actual current stock
                            model.MinStockLevel = bookForDisplay.MinStockLevel;
                        }
                        ViewData["Title"] = "Update Stock";
                        return View(model);
                    }
                    break;
                case "adjust":
                    newQuantity = model.Quantity;
                    if (newQuantity < 0) // Should not happen with client-side min="1" but good to have server validation
                    {
                        ModelState.AddModelError("Quantity", "Adjusted stock quantity cannot be negative.");
                        var bookForDisplay = await _context.Books.Include(b => b.Genre).AsNoTracking().FirstOrDefaultAsync(b => b.BookID == model.BookId);
                        if (bookForDisplay != null)
                        {
                            model.Title = bookForDisplay.Title;
                            model.Author = bookForDisplay.Author;
                            model.CoverImageURL = bookForDisplay.CoverImageURL ?? "/images/placeholder.png";
                            model.GenreName = bookForDisplay.Genre?.Name;
                            model.CurrentStockQuantity = bookForDisplay.StockQuantity;
                            model.MinStockLevel = bookForDisplay.MinStockLevel;
                        }
                        ViewData["Title"] = "Update Stock";
                        return View(model);
                    }
                    break;
                default:
                    TempData["ErrorMessage"] = "Invalid stock operation.";
                    return RedirectToAction(nameof(Stock));
            }

            book.StockQuantity = newQuantity;
            book.UpdatedAt = DateTime.UtcNow;

            var stockTransaction = new StockTransaction
            {
                BookID = book.BookID,
                TransactionType = model.Operation,
                Quantity = model.Quantity, // This is the quantity from the form
                PreviousQuantity = previousQuantity,
                NewQuantity = newQuantity,
                Reason = model.Reason,
                Notes = model.Notes,
                TransactionDate = DateTime.UtcNow,
                UserID = null // Or get logged-in admin UserID: User.FindFirstValue(ClaimTypes.NameIdentifier)
            };

            _context.Books.Update(book);
            _context.StockTransactions.Add(stockTransaction);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Stock for '{book.Title}' updated successfully.";
            return RedirectToAction(nameof(Stock));
        }

        // Optional: Helper method to calculate on-order quantity if needed
        // private async Task<int> CalculateOnOrderQuantity(int bookId)
        // {
        //     return await _context.OrderItems
        //         .Where(oi => oi.BookID == bookId && oi.Order != null && 
        //                      (oi.Order.OrderStatus == "Pending" || oi.Order.OrderStatus == "Processing"))
        //         .SumAsync(oi => oi.Quantity);
        // }




        //--- Book Management Actions ---

        // GET: /Admin/Books
        public async Task<IActionResult> Books(string? searchQuery, int page = 1)
        {
            ViewData["Title"] = "Manage Books";
            int pageSize = 15;
            IQueryable<Book> books = _context.Books.Include(b => b.Genre);

            if (!string.IsNullOrEmpty(searchQuery))
            {
                books = books.Where(b =>
                    EF.Functions.Like(b.Title, $"%{searchQuery}%") ||
                    EF.Functions.Like(b.Author, $"%{searchQuery}%") ||
                    (b.ISBN != null && EF.Functions.Like(b.ISBN, $"%{searchQuery}%")) ||
                    (b.Genre != null && EF.Functions.Like(b.Genre.Name, $"%{searchQuery}%"))
                );
            }

            int totalBooks = await books.CountAsync();
            int totalPages = (int)Math.Ceiling(totalBooks / (double)pageSize);
            page = Math.Max(1, Math.Min(page, totalPages == 0 ? 1 : totalPages));

            var pagedBooks = await books.OrderBy(b => b.Title)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var viewModel = new BookViewModel
            {
                Books = pagedBooks,
                SearchQuery = searchQuery,
                TotalBooks = totalBooks,
                CurrentPage = page,
                TotalPages = totalPages
            };

            return View(viewModel);
        }

        // GET: /Admin/AddBook
        public async Task<IActionResult> AddBook()
        {
            try
            {
                ViewData["Title"] = "Add New Book";

                // Create SelectList for genres
                var genres = await _context.Genres
                    .OrderBy(g => g.Name)
                    .ToListAsync();

                ViewBag.Genres = genres.Select(g => new SelectListItem
                {
                    Value = g.GenreID.ToString(),
                    Text = g.Name
                });

                return View(new Book());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading genres for AddBook form.");
                TempData["ErrorMessage"] = "Error loading genres. Please try again.";
                return RedirectToAction(nameof(Books));
            }
        }

        // POST: /Admin/AddBook
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddBook([Bind("Title,Author,GenreID,Price,StockQuantity,Description,CoverImageURL,ISBN,Publisher,PublishedDate,MinStockLevel,PageCount,Language,DiscountedPrice,IsActive")] Book book)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    book.CreatedAt = DateTime.UtcNow;
                    book.IsActive = true; // Default to active when adding

                    _context.Add(book);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Book '{book.Title}' added successfully!";
                    return RedirectToAction(nameof(Books));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding book '{BookTitle}'.", book.Title);
                ModelState.AddModelError("", "Failed to save the book. Please try again. " + ex.Message);
            }

            // If we got this far, something failed; reload genres and return to form
            ViewData["Title"] = "Add New Book";

            // Repopulate genres dropdown
            var genres = await _context.Genres
                .OrderBy(g => g.Name)
                .ToListAsync();

            ViewBag.Genres = genres.Select(g => new SelectListItem
            {
                Value = g.GenreID.ToString(),
                Text = g.Name
            });

            return View(book);
        }

        // GET: /Admin/EditBook/5
        public async Task<IActionResult> EditBook(int? id)
        {
            ViewData["Title"] = "Edit Book";

            if (id == null)
            {
                _logger.LogWarning("EditBook GET: No ID provided.");
                TempData["ErrorMessage"] = "No book ID provided for editing.";
                return RedirectToAction(nameof(Books));
            }

            var book = await _context.Books
                                     .Include(b => b.Genre)
                                     .FirstOrDefaultAsync(b => b.BookID == id);

            if (book == null)
            {
                _logger.LogWarning("EditBook GET: Book with ID {BookID} not found.", id);
                TempData["ErrorMessage"] = "Book not found for editing. It might have been deleted.";
                return RedirectToAction(nameof(Books));
            }

            // Populate ViewBag for the Genre dropdown (ALWAYS done before returning view in GET)
            ViewBag.Genres = new SelectList(await _context.Genres.OrderBy(g => g.Name).ToListAsync(), "GenreID", "Name", book.GenreID);

            return View(book);
        }

        // POST: /Admin/EditBook/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBook(int id,
           [Bind("BookID,Title,Author,GenreID,Price,StockQuantity,Description,CoverImageURL,ISBN,Publisher,PublishedDate,MinStockLevel,PageCount,Language,DiscountedPrice,IsActive")] Book book)
        {
            ViewData["Title"] = "Edit Book";

            if (id != book.BookID)
            {
                _logger.LogWarning("EditBook POST: ID mismatch. Route ID: {RouteID}, Model ID: {ModelID}", id, book.BookID);
                TempData["ErrorMessage"] = "Book ID mismatch. Please try again.";
                return RedirectToAction(nameof(Books));
            }

            // Always repopulate dropdowns before returning the view, especially on POST if ModelState is invalid
            ViewBag.Genres = new SelectList(await _context.Genres.OrderBy(g => g.Name).ToListAsync(), "GenreID", "Name", book.GenreID);

            if (ModelState.IsValid)
            {
                try
                {
                    // Fetch the existing entity to apply updates to, for better change tracking
                    var bookToUpdate = await _context.Books.FindAsync(id);
                    if (bookToUpdate == null)
                    {
                        _logger.LogWarning("EditBook POST: Book with ID {BookID} not found during update (might have been deleted by another user).", id);
                        TempData["ErrorMessage"] = "Book not found. It might have been deleted.";
                        return RedirectToAction(nameof(Books));
                    }

                    // Manually map updated properties from the posted 'book' object to the tracked 'bookToUpdate' entity
                    bookToUpdate.Title = book.Title;
                    bookToUpdate.Author = book.Author;
                    bookToUpdate.GenreID = book.GenreID;
                    bookToUpdate.Price = book.Price;
                    bookToUpdate.StockQuantity = book.StockQuantity;
                    bookToUpdate.Description = book.Description;
                    bookToUpdate.CoverImageURL = book.CoverImageURL;
                    bookToUpdate.ISBN = book.ISBN;
                    bookToUpdate.Publisher = book.Publisher;
                    bookToUpdate.PublishedDate = book.PublishedDate;
                    bookToUpdate.MinStockLevel = book.MinStockLevel;
                    bookToUpdate.PageCount = book.PageCount;
                    bookToUpdate.Language = book.Language;
                    bookToUpdate.DiscountedPrice = book.DiscountedPrice;
                    bookToUpdate.IsActive = book.IsActive;

                    bookToUpdate.UpdatedAt = DateTime.Now; // Update timestamp

                    // _context.Update(bookToUpdate); // This line is implicitly handled by EF Core when you modify a tracked entity
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Book '{book.Title}' updated successfully!";
                    return RedirectToAction(nameof(Books));
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    _logger.LogError(ex, "EditBook POST: Concurrency error updating book ID {BookID}.", id);
                    if (!_context.Books.Any(e => e.BookID == book.BookID))
                    {
                        TempData["ErrorMessage"] = "Book not found or deleted by another user. Please try again.";
                        return RedirectToAction(nameof(Books));
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "A concurrency error occurred while saving. Please try again.";
                        // Re-throw if you want the exception to propagate for further debugging or global error handling
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "EditBook POST: An unexpected error occurred while updating book '{BookTitle}' (ID: {BookID}).", book.Title, book.BookID);
                    TempData["ErrorMessage"] = $"An error occurred while updating the book: {ex.Message}";
                    ModelState.AddModelError("", "An unexpected error occurred. Please try again.");
                }
            }
            // If ModelState is invalid, or an error occurred, return the view with the current model
            return View(book);
        }

        // POST: /Admin/DeleteBook/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBook(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null)
            {
                TempData["ErrorMessage"] = "Book not found.";
                return RedirectToAction(nameof(Books));
            }

            try
            {
                _context.Books.Remove(book);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Book deleted successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting book ID {BookID}.", id);
                TempData["ErrorMessage"] = $"Error deleting book: {ex.Message}";
            }

            return RedirectToAction(nameof(Books));
        }


        //--- Genre Management Actions ---

        // GET: /Admin/Genres
        public async Task<IActionResult> Genres()
        {
            ViewData["Title"] = "Manage Genres";
            var genres = await _context.Genres.OrderBy(g => g.Name).ToListAsync();

            var viewModel = new GenreViewModel // <--- CREATE INSTANCE OF GenreViewModel
            {
                Genres = genres
            };

            return View(viewModel); // <--- PASS THE GenreViewModel TO THE VIEW
        }

        // GET: /Admin/AddGenre
        public IActionResult AddGenre()
        {
            ViewData["Title"] = "Add New Genre";
            return View(new Genre());
        }

        // POST: /Admin/AddGenre
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddGenre([Bind("Name,Description,IconClass,ColorTheme")] Genre genre)
        {
            ViewData["Title"] = "Add New Genre";
            if (ModelState.IsValid)
            {
                try
                {
                    genre.CreatedAt = DateTime.Now;
                    _context.Add(genre);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Genre '{genre.Name}' added successfully!";
                    return RedirectToAction(nameof(Genres));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error adding genre '{GenreName}'.", genre.Name);
                    TempData["ErrorMessage"] = $"An error occurred while adding the genre: {ex.Message}";
                }
            }
            return View(genre);
        }

        // GET: /Admin/EditGenre/5
        public async Task<IActionResult> EditGenre(int? id)
        {
            ViewData["Title"] = "Edit Genre";

            if (id == null)
            {
                _logger.LogWarning("EditGenre GET: No ID provided.");
                TempData["ErrorMessage"] = "No genre ID provided for editing.";
                return RedirectToAction(nameof(Genres));
            }

            var genre = await _context.Genres.FindAsync(id);
            if (genre == null)
            {
                _logger.LogWarning("EditGenre GET: Genre with ID {GenreID} not found.", id);
                TempData["ErrorMessage"] = "Genre not found for editing. It might have been deleted.";
                return RedirectToAction(nameof(Genres));
            }
            return View(genre);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditGenre(int id, [Bind("GenreID,Name,Description,IconClass,ColorTheme,CreatedAt")] Genre genre)
        {
            ViewData["Title"] = "Edit Genre";

            if (id != genre.GenreID)
            {
                _logger.LogWarning("EditGenre POST: ID mismatch. Route ID: {RouteID}, Model ID: {ModelID}", id, genre.GenreID);
                TempData["ErrorMessage"] = "Genre ID mismatch. Please try again.";
                return RedirectToAction(nameof(Genres));
            }

            // Fetch the existing genre to preserve CreatedAt and potentially other properties not in Bind
            var existingGenre = await _context.Genres.AsNoTracking().FirstOrDefaultAsync(g => g.GenreID == id);
            if (existingGenre == null)
            {
                _logger.LogWarning("EditGenre POST: Genre with ID {GenreID} not found during update (might have been deleted by another user).", id);
                TempData["ErrorMessage"] = "Genre not found during update validation.";
                return RedirectToAction(nameof(Genres));
            }
            genre.CreatedAt = existingGenre.CreatedAt; // Preserve original creation date
            //genre.UpdatedAt = DateTime.Now; // Uncomment if you have an UpdatedAt property and want to update it here

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(genre); // This will mark the entity as modified if it's not tracked
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Genre '{genre.Name}' updated successfully!";
                    return RedirectToAction(nameof(Genres));
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    _logger.LogError(ex, "EditGenre POST: Concurrency error updating genre ID {GenreID}.", id);
                    if (!_context.Genres.Any(e => e.GenreID == genre.GenreID))
                    {
                        TempData["ErrorMessage"] = "The genre was not found or has been deleted by another user. Please try again.";
                        return RedirectToAction(nameof(Genres));
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "A concurrency error occurred while saving. Please try again.";
                        throw; // Re-throw for further debugging or global error handling
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "EditGenre POST: An unexpected error occurred while updating genre '{GenreName}' (ID: {GenreID}).", genre.Name, genre.GenreID);
                    TempData["ErrorMessage"] = $"An error occurred while updating the genre: {ex.Message}";
                    ModelState.AddModelError("", "An unexpected error occurred. Please try again.");
                }
            }
            return View(genre);
        }

        // POST: /Admin/DeleteGenre/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteGenre(int id)
        {
            var genre = await _context.Genres.FindAsync(id);
            if (genre == null)
            {
                TempData["ErrorMessage"] = "Genre not found.";
                return RedirectToAction(nameof(Genres));
            }

            try
            {
                // Check for associated books before deleting the genre to prevent FK constraint errors
                var hasAssociatedBooks = await _context.Books.AnyAsync(b => b.GenreID == id);
                if (hasAssociatedBooks)
                {
                    TempData["ErrorMessage"] = $"Cannot delete genre '{genre.Name}' because it has associated books. Please reassign or delete the books first.";
                    _logger.LogWarning("Attempted to delete genre '{GenreName}' (ID: {GenreID}) with associated books.", genre.Name, id);
                    return RedirectToAction(nameof(Genres));
                }

                _context.Genres.Remove(genre);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Genre deleted successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting genre ID {GenreID}.", id);
                TempData["ErrorMessage"] = $"Error deleting genre: {ex.Message}";
            }

            return RedirectToAction(nameof(Genres));
        }


        //--- Other Admin Sections (Placeholder) ---

        // GET: /Admin/TopSelling
        public IActionResult TopSelling()
        {
            ViewData["Title"] = "Top Selling Books";
            // Logic to fetch and display top-selling items
            return View();
        }

        // GET: /Admin/GetOrderDetails
        [HttpGet]
        public async Task<IActionResult> GetOrderDetails(int orderId)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderItems!)
                        .ThenInclude(oi => oi.Book)
                    .FirstOrDefaultAsync(o => o.OrderID == orderId);

                if (order == null)
                {
                    return Json(new { success = false, message = "Order not found" });
                }

                return Json(new
                {
                    success = true,
                    orderId = order.OrderID,
                    orderDate = order.OrderDate,
                    orderStatus = order.OrderStatus,
                    totalAmount = order.TotalAmount,
                    shippingFirstName = order.ShippingFirstName,
                    shippingLastName = order.ShippingLastName,
                    shippingAddressLine1 = order.ShippingAddressLine1,
                    shippingAddressLine2 = order.ShippingAddressLine2,
                    shippingCity = order.ShippingCity,
                    shippingState = order.ShippingState,
                    shippingPinCode = order.ShippingPinCode,
                    shippingPhone = order.ShippingPhone,
                    shippingEmail = order.ShippingEmail,
                    orderItems = order.OrderItems.Select(item => new
                    {
                        quantity = item.Quantity,
                        price = item.Price,
                        book = new
                        {
                            title = item.Book?.Title,
                            author = item.Book?.Author,
                            coverImageURL = item.Book?.CoverImageURL ?? "/images/placeholder.png"
                        }
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching order details for order ID {OrderId}", orderId);
                return Json(new { success = false, message = "Error fetching order details" });
            }
        }
    }

    // Model for order status update
    public class UpdateOrderStatusModel
    {
        public int OrderId { get; set; }
        public string NewStatus { get; set; } = "";
    }
}