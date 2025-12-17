using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReleaseFlow.Services;
using ReleaseFlow.Services.IIS;

namespace ReleaseFlow.Controllers;

[Authorize(Policy = "DeployerOrAbove")]
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
    public async Task<IActionResult> Start(string name)
    {
        try
        {
            var result = await _siteService.StartSiteAsync(name);
            if (result)
            {
                await _auditService.LogAsync(
                    Models.AuditActions.SiteStart,
                    "Site",
                    name,
                    $"Site '{name}' started",
                    GetCurrentUserId(),
                    GetClientIpAddress());

                TempData["Success"] = $"Site '{name}' started successfully";
            }
            else
            {
                TempData["Error"] = $"Failed to start site '{name}'";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting site {SiteName}", name);
            TempData["Error"] = $"Error starting site: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Stop(string name)
    {
        try
        {
            var result = await _siteService.StopSiteAsync(name);
            if (result)
            {
                await _auditService.LogAsync(
                    Models.AuditActions.SiteStop,
                    "Site",
                    name,
                    $"Site '{name}' stopped",
                    GetCurrentUserId(),
                    GetClientIpAddress());

                TempData["Success"] = $"Site '{name}' stopped successfully";
            }
            else
            {
                TempData["Error"] = $"Failed to stop site '{name}'";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping site {SiteName}", name);
            TempData["Error"] = $"Error stopping site: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Restart(string name)
    {
        try
        {
            var result = await _siteService.RestartSiteAsync(name);
            if (result)
            {
                await _auditService.LogAsync(
                    Models.AuditActions.SiteRestart,
                    "Site",
                    name,
                    $"Site '{name}' restarted",
                    GetCurrentUserId(),
                    GetClientIpAddress());

                TempData["Success"] = $"Site '{name}' restarted successfully";
            }
            else
            {
                TempData["Error"] = $"Failed to restart site '{name}'";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restarting site {SiteName}", name);
            TempData["Error"] = $"Error restarting site: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    private int? GetCurrentUserId()
    {
        // This would be implemented to get the actual user ID from the database
        // based on the Windows identity
        return null;
    }

    private string GetClientIpAddress()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }
}
