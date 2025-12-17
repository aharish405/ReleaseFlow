using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReleaseFlow.Services;
using ReleaseFlow.Services.IIS;

namespace ReleaseFlow.Controllers;

[Authorize]
public class AppPoolsController : Controller
{
    private readonly IIISAppPoolService _appPoolService;
    private readonly IAuditService _auditService;
    private readonly ILogger<AppPoolsController> _logger;

    public AppPoolsController(
        IIISAppPoolService appPoolService,
        IAuditService auditService,
        ILogger<AppPoolsController> logger)
    {
        _appPoolService = appPoolService;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var appPools = await _appPoolService.GetAllAppPoolsAsync();
            return View(appPools);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading application pools");
            TempData["Error"] = "Failed to load application pools";
            return View(new List<AppPoolInfo>());
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
            var appPool = await _appPoolService.GetAppPoolByNameAsync(name);
            if (appPool == null)
            {
                return NotFound();
            }

            return View(appPool);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading app pool details for {AppPoolName}", name);
            return NotFound();
        }
    }

    [HttpPost]
    public async Task<IActionResult> Recycle(string name)
    {
        try
        {
            var result = await _appPoolService.RecycleAppPoolAsync(name);
            if (result)
            {
                await _auditService.LogAsync(
                    Models.AuditActions.AppPoolRecycle,
                    "AppPool",
                    name,
                    $"App pool '{name}' recycled",
                    User.Identity?.Name ?? "Unknown",
                    GetClientIpAddress());

                TempData["Success"] = $"App pool '{name}' recycled successfully";
            }
            else
            {
                TempData["Error"] = $"Failed to recycle app pool '{name}'";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recycling app pool {AppPoolName}", name);
            TempData["Error"] = $"Error recycling app pool: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Start(string name)
    {
        try
        {
            var result = await _appPoolService.StartAppPoolAsync(name);
            if (result)
            {
                TempData["Success"] = $"App pool '{name}' started successfully";
            }
            else
            {
                TempData["Error"] = $"Failed to start app pool '{name}'";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting app pool {AppPoolName}", name);
            TempData["Error"] = $"Error starting app pool: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Stop(string name)
    {
        try
        {
            var result = await _appPoolService.StopAppPoolAsync(name);
            if (result)
            {
                TempData["Success"] = $"App pool '{name}' stopped successfully";
            }
            else
            {
                TempData["Error"] = $"Failed to stop app pool '{name}'";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping app pool {AppPoolName}", name);
            TempData["Error"] = $"Error stopping app pool: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    private string GetClientIpAddress()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }
}
