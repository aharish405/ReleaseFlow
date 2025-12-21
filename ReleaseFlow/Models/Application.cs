namespace ReleaseFlow.Models;

public class Application
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IISSiteName { get; set; } = string.Empty;
    public string AppPoolName { get; set; } = string.Empty;
    public string PhysicalPath { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty; // Dev, Staging, Production
    public string? HealthCheckUrl { get; set; }
    public string ApplicationPath { get; set; } = "/"; // IIS Application Path (e.g., "/" or "/enlink")
    public bool IsActive { get; set; } = true;
    public bool IsDiscovered { get; set; } = false; // Auto-discovered from IIS
    public DateTime? LastDiscoveredAt { get; set; } // Last IIS discovery timestamp
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Deployment Configuration
    public bool StopSiteBeforeDeployment { get; set; } = false;
    public bool StopAppPoolBeforeDeployment { get; set; } = false;
    public bool StartAppPoolAfterDeployment { get; set; } = false;
    public bool StartSiteAfterDeployment { get; set; } = false;
    public bool CreateBackup { get; set; } = true;
    public bool RunHealthCheck { get; set; } = false;
    public int DeploymentDelaySeconds { get; set; } = 2; // Delay after stopping services
    public string? ExcludedPaths { get; set; } // Comma-separated list of paths to exclude from deployment

    // Navigation properties
    public ICollection<Deployment> Deployments { get; set; } = new List<Deployment>();
}
