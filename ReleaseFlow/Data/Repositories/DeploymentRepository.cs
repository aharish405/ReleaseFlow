using Microsoft.Data.SqlClient;
using ReleaseFlow.Models;

namespace ReleaseFlow.Data.Repositories;

public class DeploymentRepository : IDeploymentRepository
{
    private readonly SqlHelper _sqlHelper;

    public DeploymentRepository(SqlHelper sqlHelper)
    {
        _sqlHelper = sqlHelper;
    }

    public async Task<Deployment?> GetByIdAsync(int id)
    {
        const string sql = @"
            SELECT 
                d.*,
                a.Id as App_Id,
                a.Name as App_Name,
                a.IISSiteName as App_IISSiteName,
                a.Environment as App_Environment
            FROM ReleaseFlow_Deployments d
            INNER JOIN ReleaseFlow_Applications a ON d.ApplicationId = a.Id
            WHERE d.Id = @Id";
            
        return await _sqlHelper.ExecuteReaderSingleAsync(sql, MapDeploymentWithApplication,
            new SqlParameter("@Id", id));
    }

    public async Task<IEnumerable<Deployment>> GetByApplicationIdAsync(int applicationId)
    {
        const string sql = @"
            SELECT 
                d.*,
                a.Id as App_Id,
                a.Name as App_Name,
                a.IISSiteName as App_IISSiteName,
                a.Environment as App_Environment
            FROM ReleaseFlow_Deployments d
            INNER JOIN ReleaseFlow_Applications a ON d.ApplicationId = a.Id
            WHERE d.ApplicationId = @ApplicationId 
            ORDER BY d.StartedAt DESC";

        return await _sqlHelper.ExecuteReaderAsync(sql, MapDeploymentWithApplication,
            new SqlParameter("@ApplicationId", applicationId));
    }

    public async Task<IEnumerable<Deployment>> GetRecentAsync(int count = 10)
    {
        var sql = $@"
            SELECT TOP({count}) 
                d.*,
                a.Id as App_Id,
                a.Name as App_Name,
                a.IISSiteName as App_IISSiteName,
                a.Environment as App_Environment
            FROM ReleaseFlow_Deployments d
            INNER JOIN ReleaseFlow_Applications a ON d.ApplicationId = a.Id
            ORDER BY d.StartedAt DESC";

        return await _sqlHelper.ExecuteReaderAsync(sql, MapDeploymentWithApplication);
    }

    public async Task<int> CreateAsync(Deployment deployment)
    {
        const string sql = @"
            INSERT INTO ReleaseFlow_Deployments (
                ApplicationId, Version, DeployedByUsername, Status, StartedAt,
                CompletedAt, ZipFileName, ZipFileSize, BackupPath, CanRollback, ErrorMessage
            ) VALUES (
                @ApplicationId, @Version, @DeployedByUsername, @Status, @StartedAt,
                @CompletedAt, @ZipFileName, @ZipFileSize, @BackupPath, @CanRollback, @ErrorMessage
            );
            SELECT CAST(SCOPE_IDENTITY() as int)";

        var id = await _sqlHelper.ExecuteScalarAsync<int>(sql,
            new SqlParameter("@ApplicationId", deployment.ApplicationId),
            new SqlParameter("@Version", deployment.Version),
            new SqlParameter("@DeployedByUsername", deployment.DeployedByUsername),
            new SqlParameter("@Status", deployment.Status.ToString()),
            new SqlParameter("@StartedAt", deployment.StartedAt),
            new SqlParameter("@CompletedAt", deployment.CompletedAt ?? (object)DBNull.Value),
            new SqlParameter("@ZipFileName", deployment.ZipFileName ?? (object)DBNull.Value),
            new SqlParameter("@ZipFileSize", deployment.ZipFileSize),
            new SqlParameter("@BackupPath", deployment.BackupPath ?? (object)DBNull.Value),
            new SqlParameter("@CanRollback", deployment.CanRollback),
            new SqlParameter("@ErrorMessage", deployment.ErrorMessage ?? (object)DBNull.Value)
        );

        return id;
    }

    public async Task UpdateAsync(Deployment deployment)
    {
        const string sql = @"
            UPDATE ReleaseFlow_Deployments SET
                Status = @Status,
                CompletedAt = @CompletedAt,
                BackupPath = @BackupPath,
                CanRollback = @CanRollback,
                ErrorMessage = @ErrorMessage
            WHERE Id = @Id";

        await _sqlHelper.ExecuteNonQueryAsync(sql,
            new SqlParameter("@Id", deployment.Id),
            new SqlParameter("@Status", deployment.Status.ToString()),
            new SqlParameter("@CompletedAt", deployment.CompletedAt ?? (object)DBNull.Value),
            new SqlParameter("@BackupPath", deployment.BackupPath ?? (object)DBNull.Value),
            new SqlParameter("@CanRollback", deployment.CanRollback),
            new SqlParameter("@ErrorMessage", deployment.ErrorMessage ?? (object)DBNull.Value)
        );
    }

    public async Task<Deployment?> GetLatestSuccessfulAsync(int applicationId)
    {
        const string sql = @"
            SELECT TOP(1) * FROM ReleaseFlow_Deployments 
            WHERE ApplicationId = @ApplicationId 
              AND Status = 'Completed' 
              AND CanRollback = 1
            ORDER BY CompletedAt DESC";

        return await _sqlHelper.ExecuteReaderSingleAsync(sql, MapDeployment,
            new SqlParameter("@ApplicationId", applicationId));
    }

    private static Deployment MapDeployment(SqlDataReader reader)
    {
        return new Deployment
        {
            Id = SqlHelper.GetInt32(reader, "Id"),
            ApplicationId = SqlHelper.GetInt32(reader, "ApplicationId"),
            Version = SqlHelper.GetString(reader, "Version"),
            DeployedByUsername = SqlHelper.GetString(reader, "DeployedByUsername"),
            Status = Enum.Parse<DeploymentStatus>(SqlHelper.GetString(reader, "Status")),
            StartedAt = SqlHelper.GetDateTime(reader, "StartedAt") ?? DateTime.UtcNow,
            CompletedAt = SqlHelper.GetDateTime(reader, "CompletedAt"),
            ZipFileSize = reader.IsDBNull(reader.GetOrdinal("ZipFileSize")) ? 0L : reader.GetInt64(reader.GetOrdinal("ZipFileSize")),
            BackupPath = SqlHelper.GetValue<string>(reader, "BackupPath"),
            CanRollback = SqlHelper.GetBoolean(reader, "CanRollback"),
            ErrorMessage = SqlHelper.GetValue<string>(reader, "ErrorMessage")
        };
    }

    private static Deployment MapDeploymentWithApplication(SqlDataReader reader)
    {
        var deployment = MapDeployment(reader);
        
        // Map Application if columns exist
        if (reader.GetOrdinal("App_Id") >= 0)
        {
            deployment.Application = new Application
            {
                Id = SqlHelper.GetInt32(reader, "App_Id"),
                Name = SqlHelper.GetString(reader, "App_Name"),
                IISSiteName = SqlHelper.GetString(reader, "App_IISSiteName"),
                Environment = SqlHelper.GetString(reader, "App_Environment")
            };
        }
        
        return deployment;
    }
}
