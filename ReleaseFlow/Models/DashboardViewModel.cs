namespace ReleaseFlow.Models;

public class DashboardViewModel
{
    public int TotalSites { get; set; }
    public int RunningSites { get; set; }
    public int StoppedSites { get; set; }
    public int TotalDeployments { get; set; }
    public int SuccessfulDeployments { get; set; }
    public int FailedDeployments { get; set; }
    public List<Deployment> RecentDeployments { get; set; } = new();
    public string ServerHealth { get; set; } = string.Empty;
    public List<DriveInfoModel> Drives { get; set; } = new();
    public double CpuUsage { get; set; }
}

public class DriveInfoModel
{
    public string Name { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public long TotalSizeGB { get; set; }
    public long FreeSpaceGB { get; set; }
    public long UsedSpaceGB { get; set; }
    public int UsedPercentage { get; set; }
    public string DriveType { get; set; } = string.Empty;
}
