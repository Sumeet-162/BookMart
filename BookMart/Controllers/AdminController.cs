using BookMart.Data; // For ApplicationDbContext
using BookMart.Models; // For Book, Genre, User, AND your new ViewModels
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering; // For SelectList
using Microsoft.EntityFrameworkCore; // For .Include(), .ToListAsync(), etc.
using System.Linq; // For LINQ methods like .OrderBy, .Where
using System.Threading.Tasks; // For async/await operations
using System; // For Exception handling, DateTime
using Microsoft.Extensions.Logging; // For ILogger
using System.Collections.Generic; // Added for List<Book>

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
        public IActionResult Dashboard()
        {
            ViewData["Title"] = "Admin Dashboard";
            return View();
        }

        // GET: /Admin/Orders
        public IActionResult Orders()
        {
            ViewData["Title"] = "Manage Orders";
            // Logic to fetch and display orders
            return View();
        }

        // GET: /Admin/AdminOperations (Placeholder for other admin tasks)
        public IActionResult AdminOperations()
        {
            ViewData["Title"] = "Admin Operations";
            return View();
        }

        
        

        //--- Book Management Actions ---

        // GET: /Admin/Books
        public async Task<IActionResult> Books(string? searchQuery)
        {
            ViewData["Title"] = "Manage Books";
            IQueryable<Book> books = _context.Books.Include(b => b.Genre);

            if (!string.IsNullOrEmpty(searchQuery))
            {
                string lowerSearchQuery = searchQuery.ToLower();
                books = books.Where(b => b.Title.ToLower().Contains(lowerSearchQuery) ||
                                         b.Author.ToLower().Contains(lowerSearchQuery) ||
                                         (b.ISBN != null && b.ISBN.ToLower().Contains(lowerSearchQuery)) ||
                                         (b.Genre != null && b.Genre.Name.ToLower().Contains(lowerSearchQuery)));
            }

            var viewModel = new BookViewModel // <--- CREATE INSTANCE OF BookViewModel
            {
                Books = await books.OrderBy(b => b.Title).ToListAsync(),
                SearchQuery = searchQuery // <--- SET THE SEARCH QUERY PROPERTY
            };

            return View(viewModel); // <--- PASS THE BookViewModel TO THE VIEW
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
    }
}