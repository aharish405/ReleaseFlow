namespace ReleaseFlow.Models;

public class User
{
    public int Id { get; set; }
    public string WindowsIdentity { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    
    // Navigation properties
    public ICollection<Deployment> Deployments { get; set; } = new List<Deployment>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}
