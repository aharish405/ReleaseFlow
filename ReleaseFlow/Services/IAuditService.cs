using ReleaseFlow.Models;

namespace ReleaseFlow.Services;

public interface IAuditService
{
    Task LogAsync(string action, string entityType, string? entityId, string? details, int? userId, string ipAddress);
    Task<IEnumerable<AuditLog>> GetLogsAsync(DateTime? fromDate = null, DateTime? toDate = null, string? action = null, int? userId = null);
}
