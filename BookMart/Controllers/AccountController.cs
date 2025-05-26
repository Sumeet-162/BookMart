// Controllers/AccountController.cs
using BookMart.Models;
using BookMart.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace BookMart.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Account/Login
        public IActionResult Login()
        {
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == model.Username);

                if (user != null && BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
                {
                    if (user.IsAdmin)
                    {
                        return RedirectToAction("Dashboard", "Admin");
                    }
                    else
                    {
                        return RedirectToAction("Index", "Home");
                    }
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid username or password.");
                    return View(model);
                }
            }
            return View(model);
        }

        // GET: /Account/Register (for the signup form)
        public IActionResult Register()
        {
            // When the Register GET action is called, we pass an empty RegisterViewModel
            // to ensure the form fields can be rendered correctly with initial values.
            return View("Login", new RegisterViewModel());
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check if username already exists
                if (await _context.Users.AnyAsync(u => u.Username == model.Username))
                {
                    ModelState.AddModelError("Username", "Username is already taken.");
                    // Store data in TempData to repopulate the register form
                    TempData["RegisterUsername"] = model.Username;
                    TempData["RegisterEmail"] = model.Email;
                    TempData["RegisterFirstName"] = model.FirstName;
                    TempData["RegisterLastName"] = model.LastName;
                    TempData["RegisterPhone"] = model.Phone;
                    return View("Login", new LoginViewModel()); // Return Login view with empty LoginViewModel
                }

                // Check if email already exists
                if (await _context.Users.AnyAsync(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "Email address is already registered.");
                    // Store data in TempData to repopulate the register form
                    TempData["RegisterUsername"] = model.Username;
                    TempData["RegisterEmail"] = model.Email;
                    TempData["RegisterFirstName"] = model.FirstName;
                    TempData["RegisterLastName"] = model.LastName;
                    TempData["RegisterPhone"] = model.Phone;
                    return View("Login", new LoginViewModel()); // Return Login view with empty LoginViewModel
                }

                string passwordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);

                var newUser = new User
                {
                    Username = model.Username,
                    Email = model.Email,
                    PasswordHash = passwordHash,
                    FirstName = model.FirstName, // Map the new mandatory fields
                    LastName = model.LastName,   // Map the new mandatory fields
                    Phone = model.Phone,         // Map the new mandatory fields
                    CreatedAt = DateTime.Now,
                    IsAdmin = false
                };

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Account created successfully! Please log in.";
                return RedirectToAction("Login");
            }

            // If ModelState is NOT valid (e.g., due to [Required] validation on new fields)
            // Store data in TempData to repopulate the register form
            TempData["RegisterUsername"] = model.Username;
            TempData["RegisterEmail"] = model.Email;
            TempData["RegisterFirstName"] = model.FirstName;
            TempData["RegisterLastName"] = model.LastName;
            TempData["RegisterPhone"] = model.Phone;
            return View("Login", new LoginViewModel()); // Return Login view with empty LoginViewModel
        }
    }
}