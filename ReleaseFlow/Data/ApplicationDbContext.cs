using Microsoft.EntityFrameworkCore;
using ReleaseFlow.Models;

namespace ReleaseFlow.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Application> Applications { get; set; }
    public DbSet<Deployment> Deployments { get; set; }
    public DbSet<DeploymentStep> DeploymentSteps { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<AppSetting> AppSettings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Application configuration
        modelBuilder.Entity<Application>(entity =>
        {
            entity.ToTable("ReleaseFlow_Applications");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(256);
            entity.Property(e => e.IISSiteName).IsRequired().HasMaxLength(256);
            entity.Property(e => e.AppPoolName).IsRequired().HasMaxLength(256);
            entity.Property(e => e.PhysicalPath).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Environment).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ApplicationPath).IsRequired().HasMaxLength(255).HasDefaultValue("/");
        });

        // Deployment configuration
        modelBuilder.Entity<Deployment>(entity =>
        {
            entity.ToTable("ReleaseFlow_Deployments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Version).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ZipFileName).IsRequired().HasMaxLength(256);
            entity.Property(e => e.DeployedByUsername).IsRequired().HasMaxLength(256);
            
            entity.HasOne(e => e.Application)
                .WithMany(a => a.Deployments)
                .HasForeignKey(e => e.ApplicationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // DeploymentStep configuration
        modelBuilder.Entity<DeploymentStep>(entity =>
        {
            entity.ToTable("ReleaseFlow_DeploymentSteps");
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
            entity.ToTable("ReleaseFlow_AuditLogs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(100);
            entity.Property(e => e.EntityType).HasMaxLength(100);
            entity.Property(e => e.IpAddress).HasMaxLength(50);
            entity.HasIndex(e => e.CreatedAt);
        });

        // AppSetting configuration
        modelBuilder.Entity<AppSetting>(entity =>
        {
            entity.ToTable("ReleaseFlow_AppSettings");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Key).IsUnique();
            entity.Property(e => e.Key).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Value).IsRequired();
        });
    }
}
