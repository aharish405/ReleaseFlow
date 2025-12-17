namespace ReleaseFlow.Models;

public class AuditLog
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? Details { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public static class AuditActions
{
    public const string Login = "Login";
    public const string Logout = "Logout";
    public const string Deploy = "Deploy";
    public const string Rollback = "Rollback";
    public const string SiteStart = "SiteStart";
    public const string SiteStop = "SiteStop";
    public const string SiteRestart = "SiteRestart";
    public const string SiteCreate = "SiteCreate";
    public const string SiteDelete = "SiteDelete";
    public const string AppPoolCreate = "AppPoolCreate";
    public const string AppPoolRecycle = "AppPoolRecycle";
    public const string SettingsUpdate = "SettingsUpdate";
}
