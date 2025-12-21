using Xunit;
using Moq;
using FluentAssertions;
using ReleaseFlow.Services.Deployment;
using ReleaseFlow.Services.IIS;
using ReleaseFlow.Data.Repositories;
using ReleaseFlow.Tests.Fixtures;
using ReleaseFlow.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Web.Administration;

namespace ReleaseFlow.Tests.Integration;

/// <summary>
/// Integration tests for rollback workflow
/// </summary>
[Collection("FileSystem")]
public class RollbackWorkflowTests : IDisposable
{
    private readonly FileSystemFixture _fixture;
    private readonly Mock<IApplicationRepository> _appRepoMock;
    private readonly Mock<IDeploymentRepository> _deploymentRepoMock;
    private readonly Mock<IIISSiteService> _siteServiceMock;
    private readonly Mock<IIISAppPoolService> _poolServiceMock;
    private readonly BackupService _backupService;
    private readonly RollbackService _rollbackService;

    public RollbackWorkflowTests(FileSystemFixture fixture)
    {
        _fixture = fixture;

        _appRepoMock = new Mock<IApplicationRepository>();
        _deploymentRepoMock = new Mock<IDeploymentRepository>();
        _siteServiceMock = new Mock<IIISSiteService>();
        _poolServiceMock = new Mock<IIISAppPoolService>();

        // Setup IIS service mocks
        _siteServiceMock.Setup(x => x.StopSiteAsync(It.IsAny<string>())).ReturnsAsync(true);
        _siteServiceMock.Setup(x => x.StartSiteAsync(It.IsAny<string>())).ReturnsAsync(true);
        _siteServiceMock.Setup(x => x.GetSiteStateAsync(It.IsAny<string>()))
            .ReturnsAsync(ObjectState.Started);

        _poolServiceMock.Setup(x => x.StopAppPoolAsync(It.IsAny<string>())).ReturnsAsync(true);
        _poolServiceMock.Setup(x => x.StartAppPoolAsync(It.IsAny<string>())).ReturnsAsync(true);
        _poolServiceMock.Setup(x => x.GetAppPoolStateAsync(It.IsAny<string>()))
            .ReturnsAsync(ObjectState.Started);

        var configMock = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
        configMock.Setup(x => x["BackupBasePath"]).Returns(_fixture.BackupPath);
        _backupService = new BackupService(Mock.Of<ILogger<BackupService>>(), configMock.Object);

        _rollbackService = new RollbackService(
            _appRepoMock.Object,
            _deploymentRepoMock.Object,
            _backupService,
            _siteServiceMock.Object,
            _poolServiceMock.Object,
            Mock.Of<ILogger<RollbackService>>()
        );
    }

    [Fact]
    public async Task RollbackDeploymentAsync_ShouldRestoreFiles_WhenBackupExists()
    {
        // Arrange
        var application = TestDataBuilder.CreateTestApplication();
        var appPath = _fixture.CreateApplicationDirectory(application.Name);
        application.PhysicalPath = appPath;

        // Create original files and backup
        _fixture.CreateTestFiles(appPath, 5);
        var backupPath = await _backupService.CreateBackupAsync(appPath, application.Name, "1.0.0");

        // Modify files to simulate new deployment
        Directory.Delete(appPath, true);
        Directory.CreateDirectory(appPath);
        File.WriteAllText(Path.Combine(appPath, "newfile.txt"), "new content");

        var deployment = TestDataBuilder.CreateTestDeployment(
            applicationId: application.Id,
            backupPath: backupPath
        );

        _appRepoMock.Setup(x => x.GetByIdAsync(application.Id)).ReturnsAsync(application);
        _deploymentRepoMock.Setup(x => x.GetByIdAsync(deployment.Id)).ReturnsAsync(deployment);
        _deploymentRepoMock.Setup(x => x.UpdateAsync(It.IsAny<Models.Deployment>()))
            .Returns(Task.CompletedTask);
        _deploymentRepoMock.Setup(x => x.CreateAsync(It.IsAny<Models.Deployment>()))
            .ReturnsAsync(2);

        // Act
        var result = await _rollbackService.RollbackDeploymentAsync(deployment.Id, "testuser");

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("successfully");

        // Verify original files were restored
        Directory.GetFiles(appPath).Should().HaveCount(5);
        File.Exists(Path.Combine(appPath, "newfile.txt")).Should().BeFalse();
        File.Exists(Path.Combine(appPath, "file0.txt")).Should().BeTrue();
    }

    [Fact]
    public async Task RollbackDeploymentAsync_ShouldRestoreServiceStates_WhenServicesWereRunning()
    {
        // Arrange
        var application = TestDataBuilder.CreateTestApplication();
        var appPath = _fixture.CreateApplicationDirectory(application.Name);
        application.PhysicalPath = appPath;
        _fixture.CreateTestFiles(appPath, 3);

        var backupPath = await _backupService.CreateBackupAsync(appPath, application.Name, "1.0.0");
        var deployment = TestDataBuilder.CreateTestDeployment(backupPath: backupPath);

        _appRepoMock.Setup(x => x.GetByIdAsync(application.Id)).ReturnsAsync(application);
        _deploymentRepoMock.Setup(x => x.GetByIdAsync(deployment.Id)).ReturnsAsync(deployment);
        _deploymentRepoMock.Setup(x => x.UpdateAsync(It.IsAny<Models.Deployment>()))
            .Returns(Task.CompletedTask);
        _deploymentRepoMock.Setup(x => x.CreateAsync(It.IsAny<Models.Deployment>()))
            .ReturnsAsync(2);

        // Services are running (default mock setup)
        _siteServiceMock.Setup(x => x.GetSiteStateAsync(It.IsAny<string>()))
            .ReturnsAsync(ObjectState.Started);
        _poolServiceMock.Setup(x => x.GetAppPoolStateAsync(It.IsAny<string>()))
            .ReturnsAsync(ObjectState.Started);

        // Act
        var result = await _rollbackService.RollbackDeploymentAsync(deployment.Id, "testuser");

        // Assert
        result.Success.Should().BeTrue();

        // Verify services were stopped and restarted
        _siteServiceMock.Verify(x => x.StopSiteAsync(application.IISSiteName), Times.Once);
        _poolServiceMock.Verify(x => x.StopAppPoolAsync(application.AppPoolName), Times.Once);
        _siteServiceMock.Verify(x => x.StartSiteAsync(application.IISSiteName), Times.Once);
        _poolServiceMock.Verify(x => x.StartAppPoolAsync(application.AppPoolName), Times.Once);
    }

    [Fact]
    public async Task RollbackDeploymentAsync_ShouldNotStopServices_WhenServicesWereStopped()
    {
        // Arrange
        var application = TestDataBuilder.CreateTestApplication();
        var appPath = _fixture.CreateApplicationDirectory(application.Name);
        application.PhysicalPath = appPath;
        _fixture.CreateTestFiles(appPath, 3);

        var backupPath = await _backupService.CreateBackupAsync(appPath, application.Name, "1.0.0");
        var deployment = TestDataBuilder.CreateTestDeployment(backupPath: backupPath);

        _appRepoMock.Setup(x => x.GetByIdAsync(application.Id)).ReturnsAsync(application);
        _deploymentRepoMock.Setup(x => x.GetByIdAsync(deployment.Id)).ReturnsAsync(deployment);
        _deploymentRepoMock.Setup(x => x.UpdateAsync(It.IsAny<Models.Deployment>()))
            .Returns(Task.CompletedTask);
        _deploymentRepoMock.Setup(x => x.CreateAsync(It.IsAny<Models.Deployment>()))
            .ReturnsAsync(2);

        // Services are stopped
        _siteServiceMock.Setup(x => x.GetSiteStateAsync(It.IsAny<string>()))
            .ReturnsAsync(ObjectState.Stopped);
        _poolServiceMock.Setup(x => x.GetAppPoolStateAsync(It.IsAny<string>()))
            .ReturnsAsync(ObjectState.Stopped);

        // Act
        var result = await _rollbackService.RollbackDeploymentAsync(deployment.Id, "testuser");

        // Assert
        result.Success.Should().BeTrue();

        // Verify services were NOT stopped or started (they were already stopped)
        _siteServiceMock.Verify(x => x.StopSiteAsync(It.IsAny<string>()), Times.Never);
        _poolServiceMock.Verify(x => x.StopAppPoolAsync(It.IsAny<string>()), Times.Never);
        _siteServiceMock.Verify(x => x.StartSiteAsync(It.IsAny<string>()), Times.Never);
        _poolServiceMock.Verify(x => x.StartAppPoolAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RollbackDeploymentAsync_ShouldFail_WhenBackupDoesNotExist()
    {
        // Arrange
        var application = TestDataBuilder.CreateTestApplication();
        var deployment = TestDataBuilder.CreateTestDeployment(
            backupPath: "C:\\NonExistent\\backup.zip"
        );

        _deploymentRepoMock.Setup(x => x.GetByIdAsync(deployment.Id)).ReturnsAsync(deployment);

        // Act
        var result = await _rollbackService.RollbackDeploymentAsync(deployment.Id, "testuser");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Backup file not found");
    }

    [Fact]
    public async Task RollbackDeploymentAsync_ShouldFail_WhenCannotRollback()
    {
        // Arrange
        var deployment = TestDataBuilder.CreateTestDeployment(canRollback: false);
        _deploymentRepoMock.Setup(x => x.GetByIdAsync(deployment.Id)).ReturnsAsync(deployment);

        // Act
        var result = await _rollbackService.RollbackDeploymentAsync(deployment.Id, "testuser");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("cannot be rolled back");
    }

    [Fact]
    public async Task RollbackDeploymentAsync_ShouldFail_WhenPhysicalPathIsEmpty()
    {
        // Arrange
        var application = TestDataBuilder.CreateTestApplication();
        application.PhysicalPath = ""; // Empty path

        var appPath = _fixture.CreateApplicationDirectory("temp");
        _fixture.CreateTestFiles(appPath, 3);
        var backupPath = await _backupService.CreateBackupAsync(appPath, "temp", "1.0.0");

        var deployment = TestDataBuilder.CreateTestDeployment(backupPath: backupPath);

        _appRepoMock.Setup(x => x.GetByIdAsync(application.Id)).ReturnsAsync(application);
        _deploymentRepoMock.Setup(x => x.GetByIdAsync(deployment.Id)).ReturnsAsync(deployment);

        // Act
        var result = await _rollbackService.RollbackDeploymentAsync(deployment.Id, "testuser");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("physical path is not configured");
    }

    [Fact]
    public async Task RollbackDeploymentAsync_ShouldUpdateDeploymentStatus_ToRolledBack()
    {
        // Arrange
        var application = TestDataBuilder.CreateTestApplication();
        var appPath = _fixture.CreateApplicationDirectory(application.Name);
        application.PhysicalPath = appPath;
        _fixture.CreateTestFiles(appPath, 3);

        var backupPath = await _backupService.CreateBackupAsync(appPath, application.Name, "1.0.0");
        var deployment = TestDataBuilder.CreateTestDeployment(backupPath: backupPath);

        _appRepoMock.Setup(x => x.GetByIdAsync(application.Id)).ReturnsAsync(application);
        _deploymentRepoMock.Setup(x => x.GetByIdAsync(deployment.Id)).ReturnsAsync(deployment);
        _deploymentRepoMock.Setup(x => x.UpdateAsync(It.IsAny<Models.Deployment>()))
            .Returns(Task.CompletedTask);
        _deploymentRepoMock.Setup(x => x.CreateAsync(It.IsAny<Models.Deployment>()))
            .ReturnsAsync(2);

        // Act
        var result = await _rollbackService.RollbackDeploymentAsync(deployment.Id, "testuser");

        // Assert
        result.Success.Should().BeTrue();

        // Verify deployment status was updated to RolledBack
        _deploymentRepoMock.Verify(x => x.UpdateAsync(
            It.Is<Models.Deployment>(d => d.Status == Models.DeploymentStatus.RolledBack)),
            Times.Once);
    }

    public void Dispose()
    {
        TestDataBuilder.CleanupTestDirectories();
    }
}
