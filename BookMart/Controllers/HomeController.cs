using System.Diagnostics;
using BookMart.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BookMart.Data;

namespace BookMart.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var viewModel = new HomeViewModel();
            
            // Get all active books for explore section
            viewModel.Books = await _context.Books
                .Where(b => b.IsActive)
                .Include(b => b.Genre)
                .ToListAsync();

            // Get books with discounts for offers section
            viewModel.Offers = await _context.Books
                .Where(b => b.IsActive && b.DiscountedPrice.HasValue)
                .Include(b => b.Genre)
                .OrderByDescending(b => (b.Price - b.DiscountedPrice) / b.Price)
                .Take(4)
                .ToListAsync();

            return View(viewModel);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
