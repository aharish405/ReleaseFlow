namespace ReleaseFlow.Models;

public class Role
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    // Navigation properties
    public ICollection<User> Users { get; set; } = new List<User>();
}

public static class RoleNames
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Deployer = "Deployer";
    public const string ReadOnly = "ReadOnly";
}
