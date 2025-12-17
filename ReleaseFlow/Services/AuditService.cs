using ReleaseFlow.Data.Repositories;
using ReleaseFlow.Models;

namespace ReleaseFlow.Services;

public class AuditService : IAuditService
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ILogger<AuditService> _logger;

    public AuditService(IAuditLogRepository auditLogRepository, ILogger<AuditService> logger)
    {
        _auditLogRepository = auditLogRepository;
        _logger = logger;
    }

    public async Task LogAsync(string action, string entityType, string? entityId, string? details, string username, string ipAddress)
    {
        try
        {
            var auditLog = new AuditLog
            {
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                Details = details,
                Username = username,
                IpAddress = ipAddress,
                CreatedAt = DateTime.UtcNow
            };

            await _auditLogRepository.CreateAsync(auditLog);
            
            _logger.LogInformation("Audit log created: {Action} on {EntityType} by user {Username}", 
                action, entityType, username);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create audit log for action {Action}", action);
        }
    }

    public async Task<IEnumerable<AuditLog>> GetLogsAsync(
        DateTime? fromDate = null, 
        DateTime? toDate = null, 
        string? action = null, 
        string? username = null)
    {
        _logger.LogInformation("GetLogsAsync called with fromDate={FromDate}, toDate={ToDate}, action={Action}, username={Username}", 
            fromDate, toDate, action, username);
        
        var logs = await _auditLogRepository.GetLogsAsync(fromDate, toDate, action, username);
        
        _logger.LogInformation("Returning {Count} audit logs", logs.Count());
        
        return logs;
    }
}
