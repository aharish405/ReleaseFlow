using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReleaseFlow.Data.Repositories;
using ReleaseFlow.Models;

namespace ReleaseFlow.Controllers;

[Authorize]
public class ApplicationsController : Controller
{
    private readonly IApplicationRepository _applicationRepository;
    private readonly IDeploymentRepository _deploymentRepository;
    private readonly Services.IIS.IIISDiscoveryService _discoveryService;
    private readonly ILogger<ApplicationsController> _logger;

    public ApplicationsController(
        IApplicationRepository applicationRepository,
        IDeploymentRepository deploymentRepository,
        Services.IIS.IIISDiscoveryService discoveryService,
        ILogger<ApplicationsController> logger)
    {
        _applicationRepository = applicationRepository;
        _deploymentRepository = deploymentRepository;
        _discoveryService = discoveryService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var applications = await _applicationRepository.GetActiveAsync();

            // Load latest deployment for each application
            foreach (var app in applications)
            {
                var latestDeployment = (await _deploymentRepository.GetByApplicationIdAsync(app.Id))
                    .Where(d => d.Status == DeploymentStatus.Succeeded)
                    .OrderByDescending(d => d.CompletedAt)
                    .FirstOrDefault();

                if (latestDeployment != null)
                {
                    app.Deployments.Add(latestDeployment);
                }
            }

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
            var application = await _applicationRepository.GetByIdAsync(id);
            if (application == null)
            {
                return NotFound();
            }

            // Load recent deployments
            var deployments = await _deploymentRepository.GetByApplicationIdAsync(id);
            application.Deployments = deployments.OrderByDescending(d => d.StartedAt).Take(10).ToList();

            return View(application);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading application details for {ApplicationId}", id);
            return NotFound();
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetFileStructure(int id)
    {
        try
        {
            var application = await _applicationRepository.GetByIdAsync(id);
            if (application == null || !Directory.Exists(application.PhysicalPath))
            {
                return Json(new { error = "Application path not found" });
            }

            var fileTree = BuildFileTree(application.PhysicalPath, application.PhysicalPath);
            return Json(fileTree);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file structure for application {ApplicationId}", id);
            return Json(new { error = ex.Message });
        }
    }

    private object BuildFileTree(string path, string rootPath, int maxDepth = 5, int currentDepth = 0)
    {
        if (currentDepth >= maxDepth)
        {
            return new { name = "...", type = "limit", children = new object[0] };
        }

        var dirInfo = new DirectoryInfo(path);
        var relativePath = path == rootPath ? "/" : path.Substring(rootPath.Length).TrimStart('\\', '/');

        var node = new
        {
            name = dirInfo.Name == "" ? "/" : dirInfo.Name,
            path = relativePath,
            type = "directory",
            children = new List<object>()
        };

        try
        {
            // Get directories
            var directories = dirInfo.GetDirectories()
                .Where(d => !d.Attributes.HasFlag(FileAttributes.Hidden) && !d.Attributes.HasFlag(FileAttributes.System))
                .OrderBy(d => d.Name)
                .Take(100); // Limit to prevent too many items

            foreach (var dir in directories)
            {
                ((List<object>)node.children).Add(BuildFileTree(dir.FullName, rootPath, maxDepth, currentDepth + 1));
            }

            // Get files
            var files = dirInfo.GetFiles()
                .Where(f => !f.Attributes.HasFlag(FileAttributes.Hidden) && !f.Attributes.HasFlag(FileAttributes.System))
                .OrderBy(f => f.Name)
                .Take(100); // Limit to prevent too many items

            foreach (var file in files)
            {
                ((List<object>)node.children).Add(new
                {
                    name = file.Name,
                    path = file.FullName.Substring(rootPath.Length).TrimStart('\\', '/'),
                    type = "file",
                    size = FormatFileSize(file.Length),
                    modified = file.LastWriteTime.ToString("yyyy-MM-dd HH:mm")
                });
            }
        }
        catch (UnauthorizedAccessException)
        {
            ((List<object>)node.children).Add(new { name = "Access Denied", type = "error" });
        }

        return node;
    }

    private string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
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

                application.Id = await _applicationRepository.CreateAsync(application);

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
            var application = await _applicationRepository.GetByIdAsync(id);
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
                await _applicationRepository.UpdateAsync(application);

                TempData["Success"] = $"Application '{application.Name}' updated successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                if (!await ApplicationExists(application.Id))
                {
                    return NotFound();
                }
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
            var application = await _applicationRepository.GetByIdAsync(id);
            if (application == null)
            {
                return NotFound();
            }

            // Soft delete
            application.IsActive = false;
            application.UpdatedAt = DateTime.UtcNow;
            await _applicationRepository.UpdateAsync(application);

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
        return await _applicationRepository.ExistsAsync(id);
    }
}
