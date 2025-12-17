namespace ReleaseFlow.Models;

public class DeploymentStep
{
    public int Id { get; set; }
    public int DeploymentId { get; set; }
    public Deployment Deployment { get; set; } = null!;
    public int StepNumber { get; set; }
    public string StepName { get; set; } = string.Empty;
    public StepStatus Status { get; set; } = StepStatus.Pending;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public string? Message { get; set; }
    public string? ErrorDetails { get; set; }
}

public enum StepStatus
{
    Pending,
    InProgress,
    Succeeded,
    Failed,
    Skipped
}
