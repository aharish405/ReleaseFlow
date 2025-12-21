using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReleaseFlow.Services;
using ReleaseFlow.Services.IIS;

namespace ReleaseFlow.Controllers;

[Authorize]
public class SitesController : Controller
{
    private readonly IIISSiteService _siteService;
    private readonly IIISAppPoolService _appPoolService;
    private readonly IAuditService _auditService;
    private readonly ILogger<SitesController> _logger;

    public SitesController(
        IIISSiteService siteService,
        IIISAppPoolService appPoolService,
        IAuditService auditService,
        ILogger<SitesController> logger)
    {
        _siteService = siteService;
        _appPoolService = appPoolService;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var sites = await _siteService.GetAllSitesAsync();
            return View(sites);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading sites");
            TempData["Error"] = "Failed to load IIS sites";
            return View(new List<SiteInfo>());
        }
    }

    public async Task<IActionResult> Details(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return NotFound();
        }

        try
        {
            var site = await _siteService.GetSiteByNameAsync(name);
            if (site == null)
            {
                return NotFound();
            }

            return View(site);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading site details for {SiteName}", name);
            return NotFound();
        }
    }

    [HttpPost]
    public async Task<IActionResult> Restart(string name)
    {
        try
        {
            // Stop the site first
            var stopResult = await _siteService.StopSiteAsync(name);
            if (!stopResult)
            {
                TempData["Error"] = $"Failed to stop site '{name}' for restart";
                return RedirectToAction(nameof(Index));
            }

            // Wait a moment for the site to fully stop
            await Task.Delay(2000);

            // Start the site
            var startResult = await _siteService.StartSiteAsync(name);
            if (startResult)
            {
                await _auditService.LogAsync(
                    Models.AuditActions.SiteRestart,
                    "Site",
                    name,
                    $"Site '{name}' restarted",
                    GetCurrentUsername(),
                    GetClientIpAddress());

                TempData["Success"] = $"Site '{name}' restarted successfully";
            }
            else
            {
                TempData["Error"] = $"Failed to start site '{name}' after stopping";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restarting site {SiteName}", name);
            TempData["Error"] = $"Error restarting site: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    private string GetCurrentUsername()
    {
        return User.Identity?.Name ?? "Unknown";
    }

    private string GetClientIpAddress()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }
}
