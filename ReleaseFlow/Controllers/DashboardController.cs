using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReleaseFlow.Data;
using ReleaseFlow.Models;
using ReleaseFlow.Services.IIS;

namespace ReleaseFlow.Controllers;

[Authorize(Policy = "Authenticated")]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IIISSiteService _siteService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        ApplicationDbContext context,
        IIISSiteService siteService,
        ILogger<DashboardController> logger)
    {
        _context = context;
        _siteService = siteService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var model = new DashboardViewModel();

            // Get IIS statistics
            var sites = await _siteService.GetAllSitesAsync();
            model.TotalSites = sites.Count();
            model.RunningSites = sites.Count(s => s.State == "Started");
            model.StoppedSites = sites.Count(s => s.State == "Stopped");

            // Get deployment statistics
            model.TotalDeployments = await _context.Deployments.CountAsync();
            model.SuccessfulDeployments = await _context.Deployments
                .CountAsync(d => d.Status == DeploymentStatus.Succeeded);
            model.FailedDeployments = await _context.Deployments
                .CountAsync(d => d.Status == DeploymentStatus.Failed);

            // Get recent deployments
            model.RecentDeployments = await _context.Deployments
                .Include(d => d.Application)
                .Include(d => d.DeployedBy)
                .OrderByDescending(d => d.StartedAt)
                .Take(10)
                .ToListAsync();

            // Server health (simplified)
            model.ServerHealth = "Healthy";
            model.Drives = GetAllDrives();
            model.CpuUsage = 0; // Placeholder

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading dashboard");
            return View("Error");
        }
    }

    private List<DriveInfoModel> GetAllDrives()
    {
        var drives = new List<DriveInfoModel>();
        
        try
        {
            foreach (var drive in DriveInfo.GetDrives())
            {
                // Skip network drives and non-ready drives
                if (!drive.IsReady || drive.DriveType == DriveType.Network)
                    continue;

                var totalGB = drive.TotalSize / (1024 * 1024 * 1024);
                var freeGB = drive.AvailableFreeSpace / (1024 * 1024 * 1024);
                var usedGB = totalGB - freeGB;
                var usedPercentage = totalGB > 0 ? (int)((usedGB * 100) / totalGB) : 0;

                drives.Add(new DriveInfoModel
                {
                    Name = drive.Name,
                    Label = string.IsNullOrEmpty(drive.VolumeLabel) ? drive.Name : drive.VolumeLabel,
                    TotalSizeGB = totalGB,
                    FreeSpaceGB = freeGB,
                    UsedSpaceGB = usedGB,
                    UsedPercentage = usedPercentage,
                    DriveType = drive.DriveType.ToString()
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting drive information");
        }

        return drives;
    }
}

