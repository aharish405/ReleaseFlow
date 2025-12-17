using ReleaseFlow.Models;

namespace ReleaseFlow.Services;

public interface IAuditService
{
    Task LogAsync(string action, string entityType, string? entityId, string? details, string username, string ipAddress);
    Task<IEnumerable<AuditLog>> GetLogsAsync(DateTime? fromDate = null, DateTime? toDate = null, string? action = null, string? username = null);
}
