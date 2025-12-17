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
                    var hasApplications = await context.Applications.AnyAsync();
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

        // No seeding required - authentication is handled via appsettings.json
    }
}
