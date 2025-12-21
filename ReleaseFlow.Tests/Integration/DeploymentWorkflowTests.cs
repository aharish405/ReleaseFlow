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
/// Integration tests for full deployment workflow
/// </summary>
[Collection("FileSystem")]
public class DeploymentWorkflowTests : IDisposable
{
    private readonly FileSystemFixture _fixture;
    private readonly Mock<IApplicationRepository> _appRepoMock;
    private readonly Mock<IDeploymentRepository> _deploymentRepoMock;
    private readonly Mock<IDeploymentStepRepository> _stepRepoMock;
    private readonly Mock<IIISSiteService> _siteServiceMock;
    private readonly Mock<IIISAppPoolService> _poolServiceMock;
    private readonly Mock<IHealthCheckService> _healthCheckMock;
    private readonly BackupService _backupService;
    private readonly DeploymentService _deploymentService;

    public DeploymentWorkflowTests(FileSystemFixture fixture)
    {
        _fixture = fixture;

        // Setup mocks
        _appRepoMock = new Mock<IApplicationRepository>();
        _deploymentRepoMock = new Mock<IDeploymentRepository>();
        _stepRepoMock = new Mock<IDeploymentStepRepository>();
        _siteServiceMock = new Mock<IIISSiteService>();
        _poolServiceMock = new Mock<IIISAppPoolService>();
        _healthCheckMock = new Mock<IHealthCheckService>();

        // Setup IIS service mocks to return success
        _siteServiceMock.Setup(x => x.StopSiteAsync(It.IsAny<string>())).ReturnsAsync(true);
        _siteServiceMock.Setup(x => x.StartSiteAsync(It.IsAny<string>())).ReturnsAsync(true);
        _siteServiceMock.Setup(x => x.GetSiteStateAsync(It.IsAny<string>()))
            .ReturnsAsync(ObjectState.Started);

        _poolServiceMock.Setup(x => x.StopAppPoolAsync(It.IsAny<string>())).ReturnsAsync(true);
        _poolServiceMock.Setup(x => x.StartAppPoolAsync(It.IsAny<string>())).ReturnsAsync(true);
        _poolServiceMock.Setup(x => x.GetAppPoolStateAsync(It.IsAny<string>()))
            .ReturnsAsync(ObjectState.Started);

        _healthCheckMock.Setup(x => x.CheckHealthAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(new HealthCheckResult { IsHealthy = true, Message = "Healthy" });

        // Create real backup service
        var configMock = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
        configMock.Setup(x => x["BackupBasePath"]).Returns(_fixture.BackupPath);
        _backupService = new BackupService(Mock.Of<ILogger<BackupService>>(), configMock.Object);

        // Create deployment service
        _deploymentService = new DeploymentService(
            _appRepoMock.Object,
            _deploymentRepoMock.Object,
            _stepRepoMock.Object,
            _siteServiceMock.Object,
            _poolServiceMock.Object,
            _healthCheckMock.Object,
            _backupService,
            Mock.Of<ILogger<DeploymentService>>()
        );
    }

    [Fact]
    public async Task DeployAsync_ShouldCompleteSuccessfully_WithAllOptionsEnabled()
    {
        // Arrange
        var application = TestDataBuilder.CreateTestApplication(
            stopSite: true,
            stopPool: true,
            startSite: true,
            startPool: true,
            createBackup: true,
            runHealthCheck: true
        );

        var appPath = _fixture.CreateApplicationDirectory(application.Name);
        application.PhysicalPath = appPath;
        _fixture.CreateTestFiles(appPath, 3); // Create existing files for backup

        var zipPath = TestDataBuilder.CreateTestZipFile("deploy.zip", 5);

        _appRepoMock.Setup(x => x.GetByIdAsync(application.Id)).ReturnsAsync(application);
        _deploymentRepoMock.Setup(x => x.CreateAsync(It.IsAny<Models.Deployment>()))
            .ReturnsAsync(1);
        _deploymentRepoMock.Setup(x => x.UpdateAsync(It.IsAny<Models.Deployment>()))
            .Returns(Task.CompletedTask);
        _stepRepoMock.Setup(x => x.CreateAsync(It.IsAny<Models.DeploymentStep>()))
            .ReturnsAsync(1);

        // Act
        var result = await _deploymentService.DeployAsync(application.Id, zipPath, "1.0.0", "testuser");

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("successfully");

        // Verify files were deployed
        Directory.GetFiles(appPath).Length.Should().BeGreaterThan(0);

        // Verify backup was created
        var backups = await _backupService.GetBackupsAsync(application.Name);
        backups.Should().HaveCountGreaterThan(0);

        // Verify IIS operations were called
        _siteServiceMock.Verify(x => x.StopSiteAsync(application.IISSiteName), Times.Once);
        _poolServiceMock.Verify(x => x.StopAppPoolAsync(application.AppPoolName), Times.Once);
        _siteServiceMock.Verify(x => x.StartSiteAsync(application.IISSiteName), Times.Once);
        _poolServiceMock.Verify(x => x.StartAppPoolAsync(application.AppPoolName), Times.Once);
        _healthCheckMock.Verify(x => x.CheckHealthAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public async Task DeployAsync_ShouldCompleteSuccessfully_WithMinimalOptions()
    {
        // Arrange
        var application = TestDataBuilder.CreateTestApplication(
            stopSite: false,
            stopPool: false,
            startSite: false,
            startPool: false,
            createBackup: true,
            runHealthCheck: false
        );

        var appPath = _fixture.CreateApplicationDirectory(application.Name);
        application.PhysicalPath = appPath;

        var zipPath = TestDataBuilder.CreateTestZipFile("deploy.zip", 5);

        _appRepoMock.Setup(x => x.GetByIdAsync(application.Id)).ReturnsAsync(application);
        _deploymentRepoMock.Setup(x => x.CreateAsync(It.IsAny<Models.Deployment>()))
            .ReturnsAsync(1);
        _deploymentRepoMock.Setup(x => x.UpdateAsync(It.IsAny<Models.Deployment>()))
            .Returns(Task.CompletedTask);
        _stepRepoMock.Setup(x => x.CreateAsync(It.IsAny<Models.DeploymentStep>()))
            .ReturnsAsync(1);

        // Act
        var result = await _deploymentService.DeployAsync(application.Id, zipPath, "1.0.0", "testuser");

        // Assert
        result.Success.Should().BeTrue();

        // Verify IIS operations were NOT called
        _siteServiceMock.Verify(x => x.StopSiteAsync(It.IsAny<string>()), Times.Never);
        _poolServiceMock.Verify(x => x.StopAppPoolAsync(It.IsAny<string>()), Times.Never);
        _siteServiceMock.Verify(x => x.StartSiteAsync(It.IsAny<string>()), Times.Never);
        _poolServiceMock.Verify(x => x.StartAppPoolAsync(It.IsAny<string>()), Times.Never);
        _healthCheckMock.Verify(x => x.CheckHealthAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task DeployAsync_ShouldRespectExclusionPatterns()
    {
        // Arrange
        var application = TestDataBuilder.CreateTestApplication();
        application.ExcludedPaths = "*.log,temp/*";

        var appPath = _fixture.CreateApplicationDirectory(application.Name);
        application.PhysicalPath = appPath;

        // Create existing files that should be excluded
        File.WriteAllText(Path.Combine(appPath, "app.log"), "log content");
        var tempDir = Path.Combine(appPath, "temp");
        Directory.CreateDirectory(tempDir);
        File.WriteAllText(Path.Combine(tempDir, "temp.txt"), "temp content");

        var zipPath = TestDataBuilder.CreateTestZipFile("deploy.zip", 5);

        _appRepoMock.Setup(x => x.GetByIdAsync(application.Id)).ReturnsAsync(application);
        _deploymentRepoMock.Setup(x => x.CreateAsync(It.IsAny<Models.Deployment>()))
            .ReturnsAsync(1);
        _deploymentRepoMock.Setup(x => x.UpdateAsync(It.IsAny<Models.Deployment>()))
            .Returns(Task.CompletedTask);
        _stepRepoMock.Setup(x => x.CreateAsync(It.IsAny<Models.DeploymentStep>()))
            .ReturnsAsync(1);

        // Act
        var result = await _deploymentService.DeployAsync(application.Id, zipPath, "1.0.0", "testuser");

        // Assert
        result.Success.Should().BeTrue();

        // Verify excluded files still exist
        File.Exists(Path.Combine(appPath, "app.log")).Should().BeTrue();
        File.Exists(Path.Combine(tempDir, "temp.txt")).Should().BeTrue();
    }

    [Fact]
    public async Task DeployAsync_ShouldLogWarning_WhenHealthCheckFails()
    {
        // Arrange
        var application = TestDataBuilder.CreateTestApplication(
            createBackup: true,
            runHealthCheck: true
        );
        var appPath = _fixture.CreateApplicationDirectory(application.Name);
        application.PhysicalPath = appPath;
        application.HealthCheckUrl = "http://localhost/health";
        _fixture.CreateTestFiles(appPath, 3);

        var zipPath = TestDataBuilder.CreateTestZipFile("deploy.zip", 5);

        _appRepoMock.Setup(x => x.GetByIdAsync(application.Id)).ReturnsAsync(application);
        _deploymentRepoMock.Setup(x => x.CreateAsync(It.IsAny<Models.Deployment>()))
            .ReturnsAsync(1);
        _deploymentRepoMock.Setup(x => x.UpdateAsync(It.IsAny<Models.Deployment>()))
            .Returns(Task.CompletedTask);
        _stepRepoMock.Setup(x => x.CreateAsync(It.IsAny<Models.DeploymentStep>()))
            .ReturnsAsync(1);

        // Simulate health check failure
        _healthCheckMock.Setup(x => x.CheckHealthAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(new HealthCheckResult { IsHealthy = false, Message = "Health check failed" });

        // Act
        var result = await _deploymentService.DeployAsync(application.Id, zipPath, "1.0.0", "testuser");

        // Assert - Current behavior: deployment succeeds with warning
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("successfully");

        // Verify health check was called
        _healthCheckMock.Verify(x => x.CheckHealthAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Once);

        // Verify deployment completed (not rolled back)
        _deploymentRepoMock.Verify(x => x.UpdateAsync(
            It.Is<Models.Deployment>(d => d.Status == Models.DeploymentStatus.Succeeded)),
            Times.AtLeastOnce);
    }

    public void Dispose()
    {
        TestDataBuilder.CleanupTestDirectories();
    }
}
