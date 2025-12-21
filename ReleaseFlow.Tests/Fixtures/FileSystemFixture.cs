using Xunit;

namespace ReleaseFlow.Tests.Fixtures;

/// <summary>
/// Fixture for managing file system resources during tests
/// </summary>
public class FileSystemFixture : IDisposable
{
    public string TestRootPath { get; }
    public string BackupPath { get; }
    public string UploadPath { get; }

    public FileSystemFixture()
    {
        TestRootPath = Path.Combine(Path.GetTempPath(), "ReleaseFlowTests", Guid.NewGuid().ToString());
        BackupPath = Path.Combine(TestRootPath, "Backups");
        UploadPath = Path.Combine(TestRootPath, "Uploads");

        Directory.CreateDirectory(TestRootPath);
        Directory.CreateDirectory(BackupPath);
        Directory.CreateDirectory(UploadPath);
    }

    public string CreateApplicationDirectory(string appName)
    {
        var appPath = Path.Combine(TestRootPath, "Apps", appName);
        Directory.CreateDirectory(appPath);
        return appPath;
    }

    public void CreateTestFiles(string directory, int count = 5)
    {
        Directory.CreateDirectory(directory);
        
        for (int i = 0; i < count; i++)
        {
            var filePath = Path.Combine(directory, $"file{i}.txt");
            File.WriteAllText(filePath, $"Content {i}");
        }
    }

    public void Dispose()
    {
        if (Directory.Exists(TestRootPath))
        {
            try
            {
                Directory.Delete(TestRootPath, true);
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }
    }
}

[CollectionDefinition("FileSystem")]
public class FileSystemCollection : ICollectionFixture<FileSystemFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
