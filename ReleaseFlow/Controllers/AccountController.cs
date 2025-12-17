using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReleaseFlow.Data;
using System.Security.Claims;

namespace ReleaseFlow.Controllers;

public class AccountController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AccountController> _logger;

    public AccountController(ApplicationDbContext context, ILogger<AccountController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string username, string? returnUrl = null)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            ModelState.AddModelError("", "Please enter a username");
            return View();
        }

        // For development: auto-create user if doesn't exist
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.WindowsIdentity == username && u.IsActive);

        if (user == null)
        {
            // Create default admin user for development
            var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == Models.RoleNames.SuperAdmin);
            if (adminRole == null)
            {
                ModelState.AddModelError("", "Database not initialized. Please restart the application.");
                return View();
            }

            user = new Models.User
            {
                WindowsIdentity = username,
                DisplayName = username,
                Email = $"{username}@localhost",
                RoleId = adminRole.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Reload with role
            user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.WindowsIdentity == username);
        }

        if (user != null)
        {
            // Create claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.WindowsIdentity),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.Name),
                new Claim("DisplayName", user.DisplayName),
                new Claim("UserId", user.Id.ToString())
            };

            var claimsIdentity = new ClaimsIdentity(claims, "Cookies");
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            };

            await HttpContext.SignInAsync("Cookies", new ClaimsPrincipal(claimsIdentity), authProperties);

            // Update last login
            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("User {Username} logged in", username);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Dashboard");
        }

        ModelState.AddModelError("", "Login failed");
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("Cookies");
        return RedirectToAction("Login");
    }
}
