using System.Diagnostics;

namespace ReleaseFlow.Services.IIS;

public class HealthCheckService : IHealthCheckService
{
    private readonly ILogger<HealthCheckService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public HealthCheckService(ILogger<HealthCheckService> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(string url, int timeoutSeconds = 30)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new HealthCheckResult();

        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(timeoutSeconds);

            var response = await httpClient.GetAsync(url);
            stopwatch.Stop();

            result.StatusCode = (int)response.StatusCode;
            result.ResponseTime = stopwatch.Elapsed;
            result.IsHealthy = response.IsSuccessStatusCode;
            result.Message = response.IsSuccessStatusCode 
                ? "Health check passed" 
                : $"Health check failed with status code {response.StatusCode}";

            _logger.LogInformation("Health check for {Url}: {Status} ({ResponseTime}ms)", 
                url, response.StatusCode, stopwatch.ElapsedMilliseconds);
        }
        catch (TaskCanceledException)
        {
            stopwatch.Stop();
            result.IsHealthy = false;
            result.ResponseTime = stopwatch.Elapsed;
            result.Message = $"Health check timed out after {timeoutSeconds} seconds";
            _logger.LogWarning("Health check for {Url} timed out", url);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.IsHealthy = false;
            result.ResponseTime = stopwatch.Elapsed;
            result.Message = $"Health check failed: {ex.Message}";
            _logger.LogError(ex, "Health check for {Url} failed", url);
        }

        return result;
    }

    public async Task<HealthCheckResult> CheckSiteHealthAsync(string siteName, string? healthCheckPath = null)
    {
        // This is a simplified implementation
        // In a real scenario, you would get the site's bindings and construct the URL
        var url = $"http://localhost/{healthCheckPath ?? ""}";
        return await CheckHealthAsync(url);
    }
}
