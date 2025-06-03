// Controllers/AccountController.cs
using BookMart.Models;
using BookMart.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

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
            var user = await _context.Users.FirstOrDefaultAsync(u => 
                u.Username.ToLower() == model.Username.ToLower());

            if (user != null && BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
            {
                if (user.IsAdmin)
                {
                    return RedirectToAction("Dashboard", "Admin");
                }
                return RedirectToAction("Index", "Home");
            }
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
        }
        return View(model);
    }

    // GET: /Account/Register
    public IActionResult Register()
    {
        var model = new RegisterViewModel();
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
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                FirstName = model.FirstName,
                LastName = model.LastName,
                Phone = model.Phone,
                CreatedAt = DateTime.Now,
                IsAdmin = false
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Account created successfully! Please log in.";
            return RedirectToAction(nameof(Login));
        }

        return View(model);
    }
}