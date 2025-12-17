using Microsoft.EntityFrameworkCore;
using ReleaseFlow.Models;

namespace ReleaseFlow.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<Application> Applications { get; set; }
    public DbSet<Deployment> Deployments { get; set; }
    public DbSet<DeploymentStep> DeploymentSteps { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<AppSetting> AppSettings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.WindowsIdentity).IsUnique();
            entity.Property(e => e.WindowsIdentity).IsRequired().HasMaxLength(256);
            entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Email).HasMaxLength(256);
            
            entity.HasOne(e => e.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Role configuration
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(500);
        });

        // Application configuration
        modelBuilder.Entity<Application>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(256);
            entity.Property(e => e.IISSiteName).IsRequired().HasMaxLength(256);
            entity.Property(e => e.AppPoolName).IsRequired().HasMaxLength(256);
            entity.Property(e => e.PhysicalPath).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Environment).IsRequired().HasMaxLength(50);
        });

        // Deployment configuration
        modelBuilder.Entity<Deployment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Version).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ZipFileName).IsRequired().HasMaxLength(256);
            
            entity.HasOne(e => e.Application)
                .WithMany(a => a.Deployments)
                .HasForeignKey(e => e.ApplicationId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(e => e.DeployedBy)
                .WithMany(u => u.Deployments)
                .HasForeignKey(e => e.DeployedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // DeploymentStep configuration
        modelBuilder.Entity<DeploymentStep>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.StepName).IsRequired().HasMaxLength(256);
            
            entity.HasOne(e => e.Deployment)
                .WithMany(d => d.Steps)
                .HasForeignKey(e => e.DeploymentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // AuditLog configuration
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(100);
            entity.Property(e => e.EntityType).HasMaxLength(100);
            entity.Property(e => e.IpAddress).HasMaxLength(50);
            entity.HasIndex(e => e.CreatedAt);
            
            entity.HasOne(e => e.User)
                .WithMany(u => u.AuditLogs)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // AppSetting configuration
        modelBuilder.Entity<AppSetting>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Key).IsUnique();
            entity.Property(e => e.Key).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Value).IsRequired();
        });
    }
}
