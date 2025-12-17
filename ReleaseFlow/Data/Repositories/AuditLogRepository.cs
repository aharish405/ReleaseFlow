using Microsoft.Data.SqlClient;
using ReleaseFlow.Models;

namespace ReleaseFlow.Data.Repositories;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly SqlHelper _sqlHelper;

    public AuditLogRepository(SqlHelper sqlHelper)
    {
        _sqlHelper = sqlHelper;
    }

    public async Task CreateAsync(AuditLog auditLog)
    {
        const string sql = @"
            INSERT INTO ReleaseFlow_AuditLogs (
                Username, Action, EntityType, EntityId, Details, IpAddress, CreatedAt
            ) VALUES (
                @Username, @Action, @EntityType, @EntityId, @Details, @IpAddress, @CreatedAt
            )";

        await _sqlHelper.ExecuteNonQueryAsync(sql,
            new SqlParameter("@Username", auditLog.Username),
            new SqlParameter("@Action", auditLog.Action),
            new SqlParameter("@EntityType", auditLog.EntityType),
            new SqlParameter("@EntityId", auditLog.EntityId ?? (object)DBNull.Value),
            new SqlParameter("@Details", auditLog.Details ?? (object)DBNull.Value),
            new SqlParameter("@IpAddress", auditLog.IpAddress),
            new SqlParameter("@CreatedAt", auditLog.CreatedAt)
        );
    }

    public async Task<IEnumerable<AuditLog>> GetLogsAsync(DateTime? fromDate, DateTime? toDate, string? action, string? username)
    {
        var sql = "SELECT * FROM ReleaseFlow_AuditLogs WHERE 1=1";
        var parameters = new List<SqlParameter>();

        if (fromDate.HasValue)
        {
            sql += " AND CreatedAt >= @FromDate";
            parameters.Add(new SqlParameter("@FromDate", fromDate.Value));
        }

        if (toDate.HasValue)
        {
            sql += " AND CreatedAt <= @ToDate";
            parameters.Add(new SqlParameter("@ToDate", toDate.Value));
        }

        if (!string.IsNullOrEmpty(action))
        {
            sql += " AND Action = @Action";
            parameters.Add(new SqlParameter("@Action", action));
        }

        if (!string.IsNullOrEmpty(username))
        {
            sql += " AND Username = @Username";
            parameters.Add(new SqlParameter("@Username", username));
        }

        sql += " ORDER BY CreatedAt DESC";

        return await _sqlHelper.ExecuteReaderAsync(sql, MapAuditLog, parameters.ToArray());
    }

    public async Task<IEnumerable<AuditLog>> GetRecentLogsAsync(int count = 100)
    {
        var sql = $"SELECT TOP({count}) * FROM ReleaseFlow_AuditLogs ORDER BY CreatedAt DESC";
        return await _sqlHelper.ExecuteReaderAsync(sql, MapAuditLog);
    }

    private static AuditLog MapAuditLog(SqlDataReader reader)
    {
        return new AuditLog
        {
            Id = SqlHelper.GetInt32(reader, "Id"),
            Username = SqlHelper.GetString(reader, "Username"),
            Action = SqlHelper.GetString(reader, "Action"),
            EntityType = SqlHelper.GetString(reader, "EntityType"),
            EntityId = SqlHelper.GetValue<string>(reader, "EntityId"),
            Details = SqlHelper.GetValue<string>(reader, "Details"),
            IpAddress = SqlHelper.GetString(reader, "IpAddress"),
            CreatedAt = SqlHelper.GetDateTime(reader, "CreatedAt") ?? DateTime.UtcNow
        };
    }
}
