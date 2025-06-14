// Program.cs
using BookMart.Services;


using BookMart.Data;
using Microsoft.EntityFrameworkCore;
using System; // Required for TimeSpan
using Microsoft.AspNetCore.Authentication.Cookies; // Required for CookieAuthenticationDefaults

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHostedService<WebsitePinger>();


// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options => {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve;
        options.JsonSerializerOptions.MaxDepth = 64; // Increase max depth if needed
    });

// Configure DbContext to use SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// --- START AUTHENTICATION CONFIGURATION (NEW ADDITION) ---
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login"; // Path to your login page
        options.LogoutPath = "/Account/Logout"; // Path to your logout action
        options.AccessDeniedPath = "/Account/AccessDenied"; // Path for access denied
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60); // Cookie expiration
        options.SlidingExpiration = true; // Renew cookie if half its lifetime has passed
    });
// --- END AUTHENTICATION CONFIGURATION ---

// --- START SESSION CONFIGURATION ---
// Required to store session data in memory. For production, consider a distributed cache like Redis.
builder.Services.AddDistributedMemoryCache();

// Configure session options
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Set a timeout for the session (e.g., 30 minutes)
    options.Cookie.HttpOnly = true; // Make the session cookie inaccessible to client-side script
    options.Cookie.IsEssential = true; // Make the session cookie essential for the app to function
});
// --- END SESSION CONFIGURATION ---

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// --- ORDER MATTERS: Session before Authentication, Authentication before Authorization ---
// Ensure UseSession() is before UseAuthentication()
app.UseSession();
app.UseAuthentication(); // <--- ADD THIS LINE
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=User}/{action=UserHome}/{id?}"); // Default route is now user home

app.Run();
