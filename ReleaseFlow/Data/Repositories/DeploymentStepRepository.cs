using Microsoft.Data.SqlClient;
using ReleaseFlow.Models;

namespace ReleaseFlow.Data.Repositories;

public class DeploymentStepRepository : IDeploymentStepRepository
{
    private readonly SqlHelper _sqlHelper;

    public DeploymentStepRepository(SqlHelper sqlHelper)
    {
        _sqlHelper = sqlHelper;
    }

    public async Task<int> CreateAsync(DeploymentStep step)
    {
        const string sql = @"
            INSERT INTO ReleaseFlow_DeploymentSteps (
                DeploymentId, StepName, Status, StartedAt, CompletedAt, ErrorMessage, Details
            ) VALUES (
                @DeploymentId, @StepName, @Status, @StartedAt, @CompletedAt, @ErrorMessage, @Details
            );
            SELECT CAST(SCOPE_IDENTITY() as int)";

        var id = await _sqlHelper.ExecuteScalarAsync<int>(sql,
            new SqlParameter("@DeploymentId", step.DeploymentId),
            new SqlParameter("@StepName", step.StepName),
            new SqlParameter("@Status", step.Status),
            new SqlParameter("@StartedAt", step.StartedAt),
            new SqlParameter("@CompletedAt", step.CompletedAt ?? (object)DBNull.Value),
            new SqlParameter("@ErrorMessage", step.ErrorDetails ?? (object)DBNull.Value),
            new SqlParameter("@Details", step.Message ?? (object)DBNull.Value)
        );

        return id;
    }

    public async Task UpdateAsync(DeploymentStep step)
    {
        const string sql = @"
            UPDATE ReleaseFlow_DeploymentSteps SET
                Status = @Status,
                CompletedAt = @CompletedAt,
                ErrorMessage = @ErrorMessage,
                Details = @Details
            WHERE Id = @Id";

        await _sqlHelper.ExecuteNonQueryAsync(sql,
            new SqlParameter("@Id", step.Id),
            new SqlParameter("@Status", step.Status),
            new SqlParameter("@CompletedAt", step.CompletedAt ?? (object)DBNull.Value),
            new SqlParameter("@ErrorMessage", step.ErrorDetails ?? (object)DBNull.Value),
            new SqlParameter("@Details", step.Message ?? (object)DBNull.Value)
        );
    }

    public async Task<IEnumerable<DeploymentStep>> GetByDeploymentIdAsync(int deploymentId)
    {
        const string sql = @"
            SELECT * FROM ReleaseFlow_DeploymentSteps 
            WHERE DeploymentId = @DeploymentId 
            ORDER BY StartedAt";

        return await _sqlHelper.ExecuteReaderAsync(sql, MapDeploymentStep,
            new SqlParameter("@DeploymentId", deploymentId));
    }

    private static DeploymentStep MapDeploymentStep(SqlDataReader reader)
    {
        return new DeploymentStep
        {
            Id = SqlHelper.GetInt32(reader, "Id"),
            DeploymentId = SqlHelper.GetInt32(reader, "DeploymentId"),
            StepName = SqlHelper.GetString(reader, "StepName"),
            Status = Enum.Parse<StepStatus>(SqlHelper.GetString(reader, "Status")),
            StartedAt = SqlHelper.GetDateTime(reader, "StartedAt") ?? DateTime.UtcNow,
            CompletedAt = SqlHelper.GetDateTime(reader, "CompletedAt"),
            ErrorDetails = SqlHelper.GetValue<string>(reader, "ErrorMessage"),
            Message = SqlHelper.GetValue<string>(reader, "Details")
        };
    }
}
