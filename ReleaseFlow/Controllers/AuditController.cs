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

    public async Task<IActionResult> Index(DateTime? fromDate, DateTime? toDate, string? action)
    {
        try
        {
            var logs = await _auditService.GetLogsAsync(fromDate, toDate, action, null);
            
            ViewBag.FromDate = fromDate;
            ViewBag.ToDate = toDate;
            ViewBag.Action = action;
            
            return View(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading audit logs");
            TempData["Error"] = "Failed to load audit logs";
            return View(new List<Models.AuditLog>());
        }
    }
}
