# Database Schema Update

## Problem

The `Applications` table is missing the new deployment configuration columns:
- `CreateBackup`
- `DeploymentDelaySeconds`
- `RunHealthCheck`
- `StartAppPoolAfterDeployment`
- `StartSiteAfterDeployment`
- `StopAppPoolBeforeDeployment`
- `StopSiteBeforeDeployment`

## Solution

Since we're using `EnsureCreated()` instead of migrations, we need to drop and recreate the database.

## Option 1: Drop Database via SQL Server Management Studio

1. Open SQL Server Management Studio
2. Connect to `HARRISH-PC`
3. Right-click on `ReleaseFlow` database
4. Select "Delete"
5. Check "Close existing connections"
6. Click OK
7. Restart the ReleaseFlow application - database will be recreated automatically

## Option 2: Drop Database via SQL Command

Run this in SQL Server Management Studio or Azure Data Studio:

```sql
USE master;
GO

-- Close all connections
ALTER DATABASE ReleaseFlow SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
GO

-- Drop database
DROP DATABASE ReleaseFlow;
GO
```

Then restart the ReleaseFlow application.

## Option 3: PowerShell Script

```powershell
# Run this from PowerShell
$server = "HARRISH-PC"
$database = "ReleaseFlow"
$username = "sa"
$password = "Admin@12345"

# Drop database
$query = @"
USE master;
ALTER DATABASE [$database] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
DROP DATABASE [$database];
"@

Invoke-Sqlcmd -ServerInstance $server -Username $username -Password $password -Query $query

Write-Host "Database dropped successfully. Restart the application to recreate it."
```

## What Happens After Dropping

When you restart the application:

1. ✅ `DbInitializer` runs
2. ✅ `EnsureCreatedAsync()` creates new database
3. ✅ All tables created with new schema
4. ✅ Default roles seeded
5. ✅ Default settings seeded
6. ✅ Ready to use!

## New Schema

The `Applications` table will now include:

| Column | Type | Default | Description |
|--------|------|---------|-------------|
| StopSiteBeforeDeployment | bit | 1 | Stop IIS site before deploy |
| StopAppPoolBeforeDeployment | bit | 1 | Stop app pool before deploy |
| CreateBackup | bit | 1 | Create backup before deploy |
| StartAppPoolAfterDeployment | bit | 1 | Start app pool after deploy |
| StartSiteAfterDeployment | bit | 1 | Start IIS site after deploy |
| RunHealthCheck | bit | 1 | Run health check after deploy |
| DeploymentDelaySeconds | int | 2 | Delay after stopping services |

## Note

⚠️ **This will delete all existing data!**

If you have important data:
1. Backup the database first
2. Export application configurations
3. Re-register applications after recreation

For production, you would use EF Core migrations instead of `EnsureCreated()`.

---

**Quick Start:** Just drop the database and restart the app!
