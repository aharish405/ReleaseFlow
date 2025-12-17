using Microsoft.Data.SqlClient;
using ReleaseFlow.Models;

namespace ReleaseFlow.Data.Repositories;

public class ApplicationRepository : IApplicationRepository
{
    private readonly SqlHelper _sqlHelper;

    public ApplicationRepository(SqlHelper sqlHelper)
    {
        _sqlHelper = sqlHelper;
    }

    public async Task<Application?> GetByIdAsync(int id)
    {
        const string sql = @"
            SELECT * FROM ReleaseFlow_Applications 
            WHERE Id = @Id";

        return await _sqlHelper.ExecuteReaderSingleAsync(sql, MapApplication,
            new SqlParameter("@Id", id));
    }

    public async Task<IEnumerable<Application>> GetAllAsync()
    {
        const string sql = "SELECT * FROM ReleaseFlow_Applications ORDER BY Name";
        return await _sqlHelper.ExecuteReaderAsync(sql, MapApplication);
    }

    public async Task<IEnumerable<Application>> GetActiveAsync()
    {
        const string sql = @"
            SELECT * FROM ReleaseFlow_Applications 
            WHERE IsActive = 1 
            ORDER BY Name";

        return await _sqlHelper.ExecuteReaderAsync(sql, MapApplication);
    }

    public async Task<Application?> GetByNameAsync(string name)
    {
        const string sql = @"
            SELECT * FROM ReleaseFlow_Applications 
            WHERE Name = @Name";

        return await _sqlHelper.ExecuteReaderSingleAsync(sql, MapApplication,
            new SqlParameter("@Name", name));
    }

    public async Task<int> CreateAsync(Application application)
    {
        const string sql = @"
            INSERT INTO ReleaseFlow_Applications (
                Name, Description, IISSiteName, AppPoolName, PhysicalPath, 
                Environment, HealthCheckUrl, ApplicationPath, IsActive, IsDiscovered,
                LastDiscoveredAt, CreatedAt, UpdatedAt,
                StopSiteBeforeDeployment, StopAppPoolBeforeDeployment,
                StartAppPoolAfterDeployment, StartSiteAfterDeployment,
                CreateBackup, RunHealthCheck, DeploymentDelaySeconds, ExcludedPaths
            ) VALUES (
                @Name, @Description, @IISSiteName, @AppPoolName, @PhysicalPath,
                @Environment, @HealthCheckUrl, @ApplicationPath, @IsActive, @IsDiscovered,
                @LastDiscoveredAt, @CreatedAt, @UpdatedAt,
                @StopSiteBeforeDeployment, @StopAppPoolBeforeDeployment,
                @StartAppPoolAfterDeployment, @StartSiteAfterDeployment,
                @CreateBackup, @RunHealthCheck, @DeploymentDelaySeconds, @ExcludedPaths
            );
            SELECT CAST(SCOPE_IDENTITY() as int)";

        var id = await _sqlHelper.ExecuteScalarAsync<int>(sql,
            new SqlParameter("@Name", application.Name),
            new SqlParameter("@Description", application.Description ?? (object)DBNull.Value),
            new SqlParameter("@IISSiteName", application.IISSiteName),
            new SqlParameter("@AppPoolName", application.AppPoolName),
            new SqlParameter("@PhysicalPath", application.PhysicalPath),
            new SqlParameter("@Environment", application.Environment),
            new SqlParameter("@HealthCheckUrl", application.HealthCheckUrl ?? (object)DBNull.Value),
            new SqlParameter("@ApplicationPath", application.ApplicationPath),
            new SqlParameter("@IsActive", application.IsActive),
            new SqlParameter("@IsDiscovered", application.IsDiscovered),
            new SqlParameter("@LastDiscoveredAt", application.LastDiscoveredAt ?? (object)DBNull.Value),
            new SqlParameter("@CreatedAt", application.CreatedAt),
            new SqlParameter("@UpdatedAt", application.UpdatedAt ?? (object)DBNull.Value),
            new SqlParameter("@StopSiteBeforeDeployment", application.StopSiteBeforeDeployment),
            new SqlParameter("@StopAppPoolBeforeDeployment", application.StopAppPoolBeforeDeployment),
            new SqlParameter("@StartAppPoolAfterDeployment", application.StartAppPoolAfterDeployment),
            new SqlParameter("@StartSiteAfterDeployment", application.StartSiteAfterDeployment),
            new SqlParameter("@CreateBackup", application.CreateBackup),
            new SqlParameter("@RunHealthCheck", application.RunHealthCheck),
            new SqlParameter("@DeploymentDelaySeconds", application.DeploymentDelaySeconds),
            new SqlParameter("@ExcludedPaths", application.ExcludedPaths ?? (object)DBNull.Value)
        );

        return id;
    }

    public async Task UpdateAsync(Application application)
    {
        const string sql = @"
            UPDATE ReleaseFlow_Applications SET
                Name = @Name,
                Description = @Description,
                IISSiteName = @IISSiteName,
                AppPoolName = @AppPoolName,
                PhysicalPath = @PhysicalPath,
                Environment = @Environment,
                HealthCheckUrl = @HealthCheckUrl,
                ApplicationPath = @ApplicationPath,
                IsActive = @IsActive,
                IsDiscovered = @IsDiscovered,
                LastDiscoveredAt = @LastDiscoveredAt,
                UpdatedAt = @UpdatedAt,
                StopSiteBeforeDeployment = @StopSiteBeforeDeployment,
                StopAppPoolBeforeDeployment = @StopAppPoolBeforeDeployment,
                StartAppPoolAfterDeployment = @StartAppPoolAfterDeployment,
                StartSiteAfterDeployment = @StartSiteAfterDeployment,
                CreateBackup = @CreateBackup,
                RunHealthCheck = @RunHealthCheck,
                DeploymentDelaySeconds = @DeploymentDelaySeconds,
                ExcludedPaths = @ExcludedPaths
            WHERE Id = @Id";

        await _sqlHelper.ExecuteNonQueryAsync(sql,
            new SqlParameter("@Id", application.Id),
            new SqlParameter("@Name", application.Name),
            new SqlParameter("@Description", application.Description ?? (object)DBNull.Value),
            new SqlParameter("@IISSiteName", application.IISSiteName),
            new SqlParameter("@AppPoolName", application.AppPoolName),
            new SqlParameter("@PhysicalPath", application.PhysicalPath),
            new SqlParameter("@Environment", application.Environment),
            new SqlParameter("@HealthCheckUrl", application.HealthCheckUrl ?? (object)DBNull.Value),
            new SqlParameter("@ApplicationPath", application.ApplicationPath),
            new SqlParameter("@IsActive", application.IsActive),
            new SqlParameter("@IsDiscovered", application.IsDiscovered),
            new SqlParameter("@LastDiscoveredAt", application.LastDiscoveredAt ?? (object)DBNull.Value),
            new SqlParameter("@UpdatedAt", DateTime.UtcNow),
            new SqlParameter("@StopSiteBeforeDeployment", application.StopSiteBeforeDeployment),
            new SqlParameter("@StopAppPoolBeforeDeployment", application.StopAppPoolBeforeDeployment),
            new SqlParameter("@StartAppPoolAfterDeployment", application.StartAppPoolAfterDeployment),
            new SqlParameter("@StartSiteAfterDeployment", application.StartSiteAfterDeployment),
            new SqlParameter("@CreateBackup", application.CreateBackup),
            new SqlParameter("@RunHealthCheck", application.RunHealthCheck),
            new SqlParameter("@DeploymentDelaySeconds", application.DeploymentDelaySeconds),
            new SqlParameter("@ExcludedPaths", application.ExcludedPaths ?? (object)DBNull.Value)
        );
    }

    public async Task DeleteAsync(int id)
    {
        const string sql = "DELETE FROM ReleaseFlow_Applications WHERE Id = @Id";
        await _sqlHelper.ExecuteNonQueryAsync(sql, new SqlParameter("@Id", id));
    }

    public async Task<bool> ExistsAsync(int id)
    {
        const string sql = "SELECT COUNT(1) FROM ReleaseFlow_Applications WHERE Id = @Id";
        var count = await _sqlHelper.ExecuteScalarAsync<int>(sql, new SqlParameter("@Id", id));
        return count > 0;
    }

    private static Application MapApplication(SqlDataReader reader)
    {
        return new Application
        {
            Id = SqlHelper.GetInt32(reader, "Id"),
            Name = SqlHelper.GetString(reader, "Name"),
            Description = SqlHelper.GetString(reader, "Description"),
            IISSiteName = SqlHelper.GetString(reader, "IISSiteName"),
            AppPoolName = SqlHelper.GetString(reader, "AppPoolName"),
            PhysicalPath = SqlHelper.GetString(reader, "PhysicalPath"),
            Environment = SqlHelper.GetString(reader, "Environment"),
            HealthCheckUrl = SqlHelper.GetValue<string>(reader, "HealthCheckUrl"),
            ApplicationPath = SqlHelper.GetString(reader, "ApplicationPath", "/"),
            IsActive = SqlHelper.GetBoolean(reader, "IsActive", true),
            IsDiscovered = SqlHelper.GetBoolean(reader, "IsDiscovered", false),
            LastDiscoveredAt = SqlHelper.GetDateTime(reader, "LastDiscoveredAt"),
            CreatedAt = SqlHelper.GetDateTime(reader, "CreatedAt") ?? DateTime.UtcNow,
            UpdatedAt = SqlHelper.GetDateTime(reader, "UpdatedAt"),
            StopSiteBeforeDeployment = SqlHelper.GetBoolean(reader, "StopSiteBeforeDeployment", true),
            StopAppPoolBeforeDeployment = SqlHelper.GetBoolean(reader, "StopAppPoolBeforeDeployment", true),
            StartAppPoolAfterDeployment = SqlHelper.GetBoolean(reader, "StartAppPoolAfterDeployment", true),
            StartSiteAfterDeployment = SqlHelper.GetBoolean(reader, "StartSiteAfterDeployment", true),
            CreateBackup = SqlHelper.GetBoolean(reader, "CreateBackup", true),
            RunHealthCheck = SqlHelper.GetBoolean(reader, "RunHealthCheck", true),
            DeploymentDelaySeconds = SqlHelper.GetInt32(reader, "DeploymentDelaySeconds", 2),
            ExcludedPaths = SqlHelper.GetValue<string>(reader, "ExcludedPaths")
        };
    }
}
