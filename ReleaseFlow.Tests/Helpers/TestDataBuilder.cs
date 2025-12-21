using System.IO.Compression;

namespace ReleaseFlow.Tests.Helpers;

/// <summary>
/// Helper class for building test data
/// </summary>
public static class TestDataBuilder
{
    public static Models.Application CreateTestApplication(
        int id = 1,
        string name = "TestApp",
        string environment = "Development",
        bool stopSite = false,
        bool stopPool = false,
        bool startSite = false,
        bool startPool = false,
        bool createBackup = true,
        bool runHealthCheck = false)
    {
        return new Models.Application
        {
            Id = id,
            Name = name,
            Description = $"Test application {name}",
            IISSiteName = $"{name}Site",
            AppPoolName = $"{name}Pool",
            PhysicalPath = Path.Combine(Path.GetTempPath(), "ReleaseFlowTests", name),
            Environment = environment,
            HealthCheckUrl = "http://localhost/health",
            ApplicationPath = "/",
            IsActive = true,
            StopSiteBeforeDeployment = stopSite,
            StopAppPoolBeforeDeployment = stopPool,
            StartSiteAfterDeployment = startSite,
            StartAppPoolAfterDeployment = startPool,
            CreateBackup = createBackup,
            RunHealthCheck = runHealthCheck,
            DeploymentDelaySeconds = 0,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static Models.Deployment CreateTestDeployment(
        int id = 1,
        int applicationId = 1,
        string version = "1.0.0",
        Models.DeploymentStatus status = Models.DeploymentStatus.Succeeded,
        bool canRollback = true,
        string? backupPath = null)
    {
        return new Models.Deployment
        {
            Id = id,
            ApplicationId = applicationId,
            Version = version,
            DeployedByUsername = "testuser",
            ZipFileName = $"test_{version}.zip",
            ZipFileSize = 1024,
            Status = status,
            BackupPath = backupPath,
            CanRollback = canRollback,
            StartedAt = DateTime.UtcNow.AddMinutes(-5),
            CompletedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a test ZIP file with sample content
    /// </summary>
    public static string CreateTestZipFile(string fileName = "test.zip", int fileCount = 5)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), "ReleaseFlowTests", "Uploads");
        Directory.CreateDirectory(tempPath);

        var zipPath = Path.Combine(tempPath, fileName);
        
        // Create a temp directory with test files
        var contentPath = Path.Combine(Path.GetTempPath(), "ReleaseFlowTests", "ZipContent");
        Directory.CreateDirectory(contentPath);

        try
        {
            // Create test files
            for (int i = 0; i < fileCount; i++)
            {
                var filePath = Path.Combine(contentPath, $"file{i}.txt");
                File.WriteAllText(filePath, $"Test content {i}");
            }

            // Create a subdirectory with files
            var subDir = Path.Combine(contentPath, "SubFolder");
            Directory.CreateDirectory(subDir);
            File.WriteAllText(Path.Combine(subDir, "subfile.txt"), "Subfolder content");

            // Create ZIP
            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }
            
            ZipFile.CreateFromDirectory(contentPath, zipPath);
            
            return zipPath;
        }
        finally
        {
            // Cleanup temp content directory
            if (Directory.Exists(contentPath))
            {
                Directory.Delete(contentPath, true);
            }
        }
    }

    /// <summary>
    /// Cleans up test directories
    /// </summary>
    public static void CleanupTestDirectories()
    {
        var testRoot = Path.Combine(Path.GetTempPath(), "ReleaseFlowTests");
        if (Directory.Exists(testRoot))
        {
            try
            {
                Directory.Delete(testRoot, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
