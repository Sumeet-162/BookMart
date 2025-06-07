// Controllers/AccountController.cs
using BookMart.Models;
using BookMart.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using System.Security.Claims; // Required for Claims
using Microsoft.AspNetCore.Authentication; // Required for SignInAsync
using System.Collections.Generic; // Required for List<Claim>
using System; // Required for DateTime

// Required for Data Annotations attributes (these are for the ViewModels, but good to have if any other attributes are used directly in controller)
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookMart.Controllers;

public class AccountController(ApplicationDbContext context) : Controller
{
    private readonly ApplicationDbContext _context = context;

    // GET: /Account/Login
    public IActionResult Login() => View(new LoginViewModel());

    // POST: /Account/Login
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (ModelState.IsValid)
        {
            // Find the user by username (case-insensitive)
            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.Username.ToLower() == model.Username.ToLower());

            if (user != null && BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
            {
                // --- START: Crucial Authentication Logic ---
                var claims = new List<Claim>
                {
                    // This is the most important claim for identifying the user across requests
                    // It stores the actual UserID (int) as a string
                    new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.GivenName, user.FirstName),
                    new Claim(ClaimTypes.Surname, user.LastName),
                    new Claim("IsAdmin", user.IsAdmin ? "1" : "0"), // Add IsAdmin claim
                    new Claim(ClaimTypes.Role, user.IsAdmin ? "Admin" : "User") // Add Role claim for standard role checks
                };

                var claimsIdentity = new ClaimsIdentity(
                    claims,
                    Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);

                // Sign in the user, creating the authentication cookie
                await HttpContext.SignInAsync(
                    Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity)
                    // Removed AuthenticationProperties for RememberMe since it's not in your LoginViewModel
                    );

                // --- END: Crucial Authentication Logic ---

                TempData["SuccessMessage"] = $"Welcome back, {user.Username}!";

                if (user.IsAdmin)
                {
                    return RedirectToAction("dashboard", "Admin");
                }
                return RedirectToAction("UserHome", "User");
            }
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
        }
        return View(model);
    }

    // GET: /Account/Register
    public IActionResult Register()
    {
        var model = new RegisterViewModel();
        // Populate TempData if it exists (for returning to form after validation failure)
        if (TempData.ContainsKey("RegisterUsername")) model.Username = TempData["RegisterUsername"]?.ToString() ?? string.Empty;
        if (TempData.ContainsKey("RegisterEmail")) model.Email = TempData["RegisterEmail"]?.ToString() ?? string.Empty;
        if (TempData.ContainsKey("RegisterFirstName")) model.FirstName = TempData["RegisterFirstName"]?.ToString() ?? string.Empty;
        if (TempData.ContainsKey("RegisterLastName")) model.LastName = TempData["RegisterLastName"]?.ToString() ?? string.Empty;
        if (TempData.ContainsKey("RegisterPhone")) model.Phone = TempData["RegisterPhone"]?.ToString() ?? string.Empty;

        return View(model);
    }

    // POST: /Account/Register
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (ModelState.IsValid)
        {
            // Check if username already exists (case-insensitive)
            if (await _context.Users.AnyAsync(u =>
                u.Username.ToLower() == model.Username.ToLower()))
            {
                ModelState.AddModelError("Username", "Username is already taken.");
                return View(model);
            }

            // Check if email already exists (case-insensitive)
            if (await _context.Users.AnyAsync(u =>
                u.Email.ToLower() == model.Email.ToLower()))
            {
                ModelState.AddModelError("Email", "Email address is already registered.");
                return View(model);
            }

            var newUser = new User
            {
                Username = model.Username,
                Email = model.Email,
                // Hash the password using BCrypt before storing
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                FirstName = model.FirstName,
                LastName = model.LastName,
                Phone = model.Phone,
                CreatedAt = DateTime.Now, // Set creation timestamp
                IsAdmin = false // New users are not admins by default
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Account created successfully! Please log in.";
            return RedirectToAction(nameof(Login));
        }

        // If ModelState is not valid, return to the view with validation errors
        return View(model);
    }

    // POST: /Account/Logout
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        // Sign out the user from the authentication scheme
        await HttpContext.SignOutAsync(
            Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);

        // Clear the guest user ID from session upon logout.
        // This ensures that if the user browses unauthenticated after logout,
        // a new guest session (or existing one if they return later) is used.
        HttpContext.Session.Remove("CurrentUserId");

        TempData["InfoMessage"] = "You have been logged out.";
        return RedirectToAction("Login", "Account");
    }
}
