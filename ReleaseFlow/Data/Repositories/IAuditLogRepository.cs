using ReleaseFlow.Models;

namespace ReleaseFlow.Data.Repositories;

public interface IAuditLogRepository
{
    Task CreateAsync(AuditLog auditLog);
    Task<IEnumerable<AuditLog>> GetLogsAsync(DateTime? fromDate, DateTime? toDate, string? action, string? username);
    Task<IEnumerable<AuditLog>> GetRecentLogsAsync(int count = 100);
}
