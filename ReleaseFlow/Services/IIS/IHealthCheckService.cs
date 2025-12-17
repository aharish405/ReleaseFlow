namespace ReleaseFlow.Services.IIS;

public interface IHealthCheckService
{
    Task<HealthCheckResult> CheckHealthAsync(string url, int timeoutSeconds = 30);
    Task<HealthCheckResult> CheckSiteHealthAsync(string siteName, string? healthCheckPath = null);
}

public class HealthCheckResult
{
    public bool IsHealthy { get; set; }
    public int StatusCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public TimeSpan ResponseTime { get; set; }
}
