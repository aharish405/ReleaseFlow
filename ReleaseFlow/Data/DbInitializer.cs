using Microsoft.EntityFrameworkCore;
using ReleaseFlow.Models;

namespace ReleaseFlow.Data;

public static class DbInitializer
{
    public static async Task Initialize(ApplicationDbContext context)
    {
        try
        {
            // Check if database exists and has tables
            var canConnect = await context.Database.CanConnectAsync();
            
            if (canConnect)
            {
                // Try to check if tables exist
                try
                {
                    var hasRoles = await context.Roles.AnyAsync();
                }
                catch (Microsoft.Data.SqlClient.SqlException)
                {
                    // Tables don't exist, recreate database
                    await context.Database.EnsureDeletedAsync();
                    await context.Database.EnsureCreatedAsync();
                }
            }
            else
            {
                // Database doesn't exist, create it
                await context.Database.EnsureCreatedAsync();
            }
        }
        catch
        {
            // If any error, try to create database
            await context.Database.EnsureCreatedAsync();
        }

        // Check if already seeded
        if (await context.Roles.AnyAsync())
        {
            return; // Database has been seeded
        }

        // Seed Roles
        var roles = new[]
        {
            new Role
            {
                Name = RoleNames.SuperAdmin,
                Description = "Full system access including user management and settings"
            },
            new Role
            {
                Name = RoleNames.Deployer,
                Description = "Can deploy applications and manage IIS sites"
            },
            new Role
            {
                Name = RoleNames.ReadOnly,
                Description = "Read-only access to view deployments and IIS status"
            }
        };

        await context.Roles.AddRangeAsync(roles);
        await context.SaveChangesAsync();

        // Seed default settings
        var settings = new[]
        {
            new AppSetting
            {
                Key = SettingKeys.DeploymentBasePath,
                Value = @"C:\ReleaseFlow\Deployments",
                Description = "Base path for deployment files"
            },
            new AppSetting
            {
                Key = SettingKeys.BackupBasePath,
                Value = @"C:\ReleaseFlow\Backups",
                Description = "Base path for backup files"
            },
            new AppSetting
            {
                Key = SettingKeys.BackupRetentionDays,
                Value = "30",
                Description = "Number of days to retain backups"
            },
            new AppSetting
            {
                Key = SettingKeys.MaxUploadSizeMB,
                Value = "500",
                Description = "Maximum upload file size in MB"
            },
            new AppSetting
            {
                Key = SettingKeys.HealthCheckTimeoutSeconds,
                Value = "30",
                Description = "Health check timeout in seconds"
            }
        };

        await context.AppSettings.AddRangeAsync(settings);
        await context.SaveChangesAsync();

        // Create default admin user (will need to be mapped to actual Windows identity)
        var adminRole = await context.Roles.FirstAsync(r => r.Name == RoleNames.SuperAdmin);
        var adminUser = new User
        {
            WindowsIdentity = Environment.UserDomainName + "\\" + Environment.UserName,
            DisplayName = "System Administrator",
            Email = "admin@releaseflow.local",
            RoleId = adminRole.Id,
            IsActive = true
        };

        await context.Users.AddAsync(adminUser);
        await context.SaveChangesAsync();
    }
}
