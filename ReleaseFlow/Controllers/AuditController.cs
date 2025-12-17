using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReleaseFlow.Services;

namespace ReleaseFlow.Controllers;

[Authorize]
public class AuditController : Controller
{
    private readonly IAuditService _auditService;
    private readonly ILogger<AuditController> _logger;

    public AuditController(IAuditService auditService, ILogger<AuditController> logger)
    {
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<IActionResult> Index(DateTime? fromDate, DateTime? toDate, string? actionType)
    {
        try
        {
            var logs = await _auditService.GetLogsAsync(fromDate, toDate, actionType, null);
            
            _logger.LogInformation("Retrieved {Count} audit logs", logs.Count());
            
            ViewBag.FromDate = fromDate;
            ViewBag.ToDate = toDate;
            ViewBag.Action = actionType;
            
            return View(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading audit logs");
            TempData["Error"] = $"Failed to load audit logs: {ex.Message}";
            return View(new List<Models.AuditLog>());
        }
    }
}
