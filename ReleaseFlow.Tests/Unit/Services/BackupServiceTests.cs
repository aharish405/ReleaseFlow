using Xunit;
using Moq;
using FluentAssertions;
using ReleaseFlow.Services.Deployment;
using ReleaseFlow.Tests.Fixtures;
using ReleaseFlow.Tests.Helpers;
using Microsoft.Extensions.Logging;

namespace ReleaseFlow.Tests.Unit.Services;

[Collection("FileSystem")]
public class BackupServiceTests
{
    private readonly FileSystemFixture _fixture;
    private readonly Mock<ILogger<BackupService>> _loggerMock;
    private readonly Mock<Microsoft.Extensions.Configuration.IConfiguration> _configMock;
    private readonly BackupService _service;

    public BackupServiceTests(FileSystemFixture fixture)
    {
        _fixture = fixture;
        _loggerMock = new Mock<ILogger<BackupService>>();
        _configMock = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
        _configMock.Setup(x => x["BackupBasePath"]).Returns(_fixture.BackupPath);
        _service = new BackupService(_loggerMock.Object, _configMock.Object);
    }

    [Fact]
    public async Task CreateBackupAsync_ShouldCreateZipFile_WhenSourceDirectoryExists()
    {
        // Arrange
        var appName = "TestApp" + Guid.NewGuid().ToString("N").Substring(0, 8);
        var version = "1.0.0";
        var sourcePath = _fixture.CreateApplicationDirectory(appName);
        _fixture.CreateTestFiles(sourcePath, 5);

        // Act
        var backupPath = await _service.CreateBackupAsync(sourcePath, appName, version);

        // Assert
        backupPath.Should().NotBeNullOrEmpty();
        File.Exists(backupPath).Should().BeTrue();
        backupPath.Should().EndWith(".zip");
        backupPath.Should().Contain(appName);
        backupPath.Should().Contain(version);
    }

    [Fact]
    public async Task CreateBackupAsync_ShouldReturnEmptyString_WhenSourceDirectoryDoesNotExist()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_fixture.TestRootPath, "NonExistent");

        // Act
        var result = await _service.CreateBackupAsync(nonExistentPath, "TestApp", "1.0.0");

        // Assert - BackupService returns empty string when source doesn't exist
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task RestoreBackupAsync_ShouldExtractFiles_WhenBackupExists()
    {
        // Arrange
        var appName = "TestApp" + Guid.NewGuid().ToString("N").Substring(0, 8);
        var sourcePath = _fixture.CreateApplicationDirectory(appName);
        _fixture.CreateTestFiles(sourcePath, 5);

        var backupPath = await _service.CreateBackupAsync(sourcePath, appName, "1.0.0");

        // Clear the source directory
        Directory.Delete(sourcePath, true);
        Directory.CreateDirectory(sourcePath);

        // Act
        var result = await _service.RestoreBackupAsync(backupPath, sourcePath);

        // Assert
        result.Should().BeTrue();
        Directory.GetFiles(sourcePath).Should().HaveCount(5);
    }

    [Fact]
    public async Task RestoreBackupAsync_ShouldReturnFalse_WhenBackupDoesNotExist()
    {
        // Arrange
        var nonExistentBackup = Path.Combine(_fixture.BackupPath, "nonexistent.zip");
        var destinationPath = _fixture.CreateApplicationDirectory("TestApp");

        // Act
        var result = await _service.RestoreBackupAsync(nonExistentBackup, destinationPath);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RestoreBackupAsync_ShouldOverwriteExistingFiles()
    {
        // Arrange
        var appName = "TestApp" + Guid.NewGuid().ToString("N").Substring(0, 8);
        var sourcePath = _fixture.CreateApplicationDirectory(appName);
        File.WriteAllText(Path.Combine(sourcePath, "file0.txt"), "Original content");

        var backupPath = await _service.CreateBackupAsync(sourcePath, appName, "1.0.0");

        // Modify the file
        File.WriteAllText(Path.Combine(sourcePath, "file0.txt"), "Modified content");

        // Act
        var result = await _service.RestoreBackupAsync(backupPath, sourcePath);

        // Assert
        result.Should().BeTrue();
        var restoredContent = File.ReadAllText(Path.Combine(sourcePath, "file0.txt"));
        restoredContent.Should().Be("Original content");
    }

    [Fact]
    public async Task GetBackupsAsync_ShouldReturnListOfBackups()
    {
        // Arrange
        var appName = "TestApp" + Guid.NewGuid().ToString("N").Substring(0, 8);
        var sourcePath = _fixture.CreateApplicationDirectory(appName);
        _fixture.CreateTestFiles(sourcePath, 3);

        await _service.CreateBackupAsync(sourcePath, appName, "1.0.0");
        await Task.Delay(100); // Ensure different timestamps
        await _service.CreateBackupAsync(sourcePath, appName, "1.0.1");

        // Act
        var backups = await _service.GetBackupsAsync(appName);

        // Assert
        backups.Should().HaveCount(2);
        backups.Should().OnlyContain(b => b.ApplicationName == appName);
    }

    [Fact]
    public async Task GetBackupsAsync_ShouldReturnEmptyList_WhenNoBackupsExist()
    {
        // Arrange
        var appName = "NonExistentApp";

        // Act
        var backups = await _service.GetBackupsAsync(appName);

        // Assert
        backups.Should().BeEmpty();
    }
}
