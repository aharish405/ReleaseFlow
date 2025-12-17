using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReleaseFlow.Data.Repositories;
using ReleaseFlow.Services;
using ReleaseFlow.Services.Deployment;

namespace ReleaseFlow.Controllers;

[Authorize]
public class DeploymentsController : Controller
{
    private readonly IApplicationRepository _applicationRepository;
    private readonly IDeploymentRepository _deploymentRepository;
    private readonly IDeploymentService _deploymentService;
    private readonly IRollbackService _rollbackService;
    private readonly IAuditService _auditService;
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DeploymentsController> _logger;

    public DeploymentsController(
        IApplicationRepository applicationRepository,
        IDeploymentRepository deploymentRepository,
        IDeploymentService deploymentService,
        IRollbackService rollbackService,
        IAuditService auditService,
        IWebHostEnvironment environment,
        IConfiguration configuration,
        ILogger<DeploymentsController> logger)
    {
        _applicationRepository = applicationRepository;
        _deploymentRepository = deploymentRepository;
        _deploymentService = deploymentService;
        _rollbackService = rollbackService;
        _auditService = auditService;
        _environment = environment;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<IActionResult> Index(int? applicationId)
    {
        try
        {
            var deployments = await _deploymentService.GetDeploymentHistoryAsync(applicationId);
            ViewBag.Applications = await _applicationRepository.GetActiveAsync();
            ViewBag.SelectedApplicationId = applicationId;
            return View(deployments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading deployment history");
            TempData["Error"] = "Failed to load deployment history";
            ViewBag.Applications = await _applicationRepository.GetActiveAsync();
            ViewBag.SelectedApplicationId = applicationId;
            return View(new List<Models.Deployment>());
        }
    }

    public async Task<IActionResult> Details(int id)
    {
        try
        {
            var deployment = await _deploymentService.GetDeploymentByIdAsync(id);
            if (deployment == null)
            {
                return NotFound();
            }

            return View(deployment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading deployment details for {DeploymentId}", id);
            return NotFound();
        }
    }

    public async Task<IActionResult> Deploy(int? applicationId = null)
    {
        ViewBag.Applications = await _applicationRepository.GetActiveAsync();
        ViewBag.SelectedApplicationId = applicationId;
        return View();
    }

    [HttpPost]
    [RequestSizeLimit(524288000)] // 500 MB
    [RequestFormLimits(MultipartBodyLengthLimit = 524288000)]
    public async Task<IActionResult> Deploy(int applicationId, string version, IFormFile zipFile)
    {
        ViewBag.Applications = await _applicationRepository.GetActiveAsync();

        if (zipFile == null || zipFile.Length == 0)
        {
            ModelState.AddModelError("zipFile", "Please select a ZIP file to upload");
            return View();
        }

        if (!zipFile.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError("zipFile", "Only ZIP files are allowed");
            return View();
        }

        if (string.IsNullOrWhiteSpace(version))
        {
            ModelState.AddModelError("version", "Version is required");
            return View();
        }

        try
        {
            // Save uploaded file to configured upload location
            var uploadsPath = _configuration["UploadBasePath"] ?? Path.Combine(_environment.ContentRootPath, "Uploads");
            if (!Directory.Exists(uploadsPath))
            {
                Directory.CreateDirectory(uploadsPath);
            }

            var fileName = $"{Guid.NewGuid()}_{zipFile.FileName}";
            var filePath = Path.Combine(uploadsPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await zipFile.CopyToAsync(stream);
            }

            // Get current username
            var username = User.Identity?.Name ?? "Unknown";

            // Execute deployment
            var result = await _deploymentService.DeployAsync(applicationId, filePath, version, username);

            if (result.Success)
            {
                await _auditService.LogAsync(
                    Models.AuditActions.Deploy,
                    "Deployment",
                    result.DeploymentId?.ToString(),
                    $"Deployment {version} completed successfully",
                    username,
                    GetClientIpAddress());

                TempData["Success"] = "Deployment completed successfully";
                return RedirectToAction(nameof(Details), new { id = result.DeploymentId });
            }
            else
            {
                TempData["Error"] = $"Deployment failed: {result.Message}";
                if (!string.IsNullOrEmpty(result.ErrorDetails))
                {
                    ViewBag.ErrorDetails = result.ErrorDetails;
                }
                return View();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during deployment");
            TempData["Error"] = $"Deployment error: {ex.Message}";
            return View();
        }
    }

    [HttpPost]
    public async Task<IActionResult> Rollback(int id)
    {
        try
        {
            var canRollback = await _rollbackService.CanRollbackAsync(id);
            if (!canRollback)
            {
                TempData["Error"] = "This deployment cannot be rolled back";
                return RedirectToAction(nameof(Details), new { id });
            }

            var username = User.Identity?.Name ?? "Unknown";
            var result = await _rollbackService.RollbackDeploymentAsync(id, username);

            if (result.Success)
            {
                await _auditService.LogAsync(
                    Models.AuditActions.Rollback,
                    "Deployment",
                    id.ToString(),
                    $"Deployment rolled back successfully",
                    username,
                    GetClientIpAddress());

                TempData["Success"] = "Rollback completed successfully";
            }
            else
            {
                TempData["Error"] = $"Rollback failed: {result.Message}";
            }

            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during rollback for deployment {DeploymentId}", id);
            TempData["Error"] = $"Rollback error: {ex.Message}";
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    private string GetClientIpAddress()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }
}
