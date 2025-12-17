using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReleaseFlow.Data;
using ReleaseFlow.Models;

namespace ReleaseFlow.Controllers;

[Authorize]
public class ApplicationsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly Services.IIS.IIISDiscoveryService _discoveryService;
    private readonly ILogger<ApplicationsController> _logger;

    public ApplicationsController(
        ApplicationDbContext context,
        Services.IIS.IIISDiscoveryService discoveryService,
        ILogger<ApplicationsController> logger)
    {
        _context = context;
        _discoveryService = discoveryService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var applications = await _context.Applications
                .Where(a => a.IsActive)
                .OrderBy(a => a.Name)
                .ToListAsync();

            return View(applications);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading applications");
            TempData["Error"] = "Failed to load applications";
            return View(new List<Application>());
        }
    }

    public async Task<IActionResult> Details(int id)
    {
        try
        {
            var application = await _context.Applications
                .Include(a => a.Deployments.OrderByDescending(d => d.StartedAt).Take(10))
                .FirstOrDefaultAsync(a => a.Id == id);

            if (application == null)
            {
                return NotFound();
            }

            return View(application);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading application details for {ApplicationId}", id);
            return NotFound();
        }
    }

    public IActionResult Create()
    {
        return View(new Application());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Application application)
    {
        if (ModelState.IsValid)
        {
            try
            {
                application.CreatedAt = DateTime.UtcNow;
                application.IsActive = true;

                _context.Applications.Add(application);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Application '{application.Name}' created successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating application");
                ModelState.AddModelError("", "Failed to create application");
            }
        }

        return View(application);
    }

    public async Task<IActionResult> Edit(int id)
    {
        try
        {
            var application = await _context.Applications.FindAsync(id);
            if (application == null)
            {
                return NotFound();
            }

            return View(application);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading application for edit {ApplicationId}", id);
            return NotFound();
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Application application)
    {
        if (id != application.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                application.UpdatedAt = DateTime.UtcNow;
                _context.Update(application);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Application '{application.Name}' updated successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await ApplicationExists(application.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating application {ApplicationId}", id);
                ModelState.AddModelError("", "Failed to update application");
            }
        }

        return View(application);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var application = await _context.Applications.FindAsync(id);
            if (application == null)
            {
                return NotFound();
            }

            // Soft delete
            application.IsActive = false;
            application.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Application '{application.Name}' deleted successfully";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting application {ApplicationId}", id);
            TempData["Error"] = "Failed to delete application";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    public async Task<IActionResult> DiscoverFromIIS()
    {
        try
        {
            _logger.LogInformation("Starting IIS application discovery...");
            var result = await _discoveryService.DiscoverAndRegisterApplicationsAsync();

            if (result.Success)
            {
                TempData["Success"] = result.Message;
                if (result.Errors.Any())
                {
                    TempData["Warning"] = $"Some errors occurred: {string.Join(", ", result.Errors.Take(3))}";
                }
            }
            else
            {
                TempData["Error"] = result.Message;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during IIS discovery");
            TempData["Error"] = $"Discovery failed: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> ApplicationExists(int id)
    {
        return await _context.Applications.AnyAsync(e => e.Id == id);
    }
}
