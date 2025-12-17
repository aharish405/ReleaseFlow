using Microsoft.EntityFrameworkCore;
using ReleaseFlow.Data;
using ReleaseFlow.Models;

namespace ReleaseFlow.Services;

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AuditService> _logger;

    public AuditService(ApplicationDbContext context, ILogger<AuditService> logger)
    {
        _context = context;
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

            await _context.AuditLogs.AddAsync(auditLog);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Audit log created: {Action} on {EntityType} by user {Username}", 
                action, entityType, username);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create audit log");
            // Don't throw - audit logging should not break the application
        }
    }

    public async Task<IEnumerable<AuditLog>> GetLogsAsync(
        DateTime? fromDate = null, 
        DateTime? toDate = null, 
        string? action = null, 
        string? username = null)
    {
        var query = _context.AuditLogs.AsQueryable();

        if (fromDate.HasValue)
        {
            query = query.Where(a => a.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(a => a.CreatedAt <= toDate.Value);
        }

        if (!string.IsNullOrEmpty(action))
        {
            query = query.Where(a => a.Action == action);
        }

        if (!string.IsNullOrEmpty(username))
        {
            query = query.Where(a => a.Username == username);
        }

        return await query
            .OrderByDescending(a => a.CreatedAt)
            .Take(1000)
            .ToListAsync();
    }
}
