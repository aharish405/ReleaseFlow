# Configurable Deployment Steps

## Overview

Each application can now have customized deployment steps. Not all applications require the same deployment process - some may need to stop IIS, others may not. This feature allows per-application configuration.

## New Configuration Options

### Application Model Properties

Added 7 new properties to the `Application` model:

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `StopSiteBeforeDeployment` | bool | `true` | Stop IIS site before deploying |
| `StopAppPoolBeforeDeployment` | bool | `true` | Stop application pool before deploying |
| `CreateBackup` | bool | `true` | Create backup before deployment |
| `StartAppPoolAfterDeployment` | bool | `true` | Start application pool after deployment |
| `StartSiteAfterDeployment` | bool | `true` | Start IIS site after deployment |
| `RunHealthCheck` | bool | `true` | Run health check after deployment |
| `DeploymentDelaySeconds` | int | `2` | Seconds to wait after stopping services |

## How It Works

### During Deployment

The `DeploymentService` now checks each configuration flag before executing a step:

```csharp
// Example: Stop site only if configured
if (application.StopSiteBeforeDeployment)
{
    await _siteService.StopSiteAsync(application.IISSiteName);
    // Log: "IIS site stopped"
}
else
{
    // Log: "Skipped stopping IIS site (not configured)"
}
```

### Deployment Steps

The deployment process now respects these settings:

1. ✅ **Extract ZIP** (always runs)
2. ✅ **Validate** (always runs)
3. ⚙️ **Stop IIS Site** (if `StopSiteBeforeDeployment = true`)
4. ⚙️ **Stop App Pool** (if `StopAppPoolBeforeDeployment = true`)
5. ⏱️ **Wait** (configurable delay via `DeploymentDelaySeconds`)
6. ⚙️ **Create Backup** (if `CreateBackup = true`)
7. ✅ **Replace Files** (always runs)
8. ⚙️ **Start App Pool** (if `StartAppPoolAfterDeployment = true`)
9. ⚙️ **Start IIS Site** (if `StartSiteAfterDeployment = true`)
10. ⚙️ **Health Check** (if `RunHealthCheck = true` and URL provided)
11. ✅ **Complete** (always runs)

## UI Configuration

### Application Create/Edit Form

Added a new "Deployment Configuration" section with:

- **6 checkboxes** for enabling/disabling steps
- **1 number input** for deployment delay (0-30 seconds)
- All options default to `true` (enabled)
- Clear labels explaining each option

### Example Scenarios

**Scenario 1: Static File Deployment**
```
✅ Stop Site: No
✅ Stop App Pool: No
✅ Create Backup: Yes
✅ Start App Pool: No
✅ Start Site: No
✅ Health Check: No
Delay: 0 seconds
```
*Use case: Deploying static HTML/CSS/JS files that don't require service restart*

**Scenario 2: .NET Application (Full Restart)**
```
✅ Stop Site: Yes
✅ Stop App Pool: Yes
✅ Create Backup: Yes
✅ Start App Pool: Yes
✅ Start Site: Yes
✅ Health Check: Yes
Delay: 2 seconds
```
*Use case: Standard .NET application requiring full restart*

**Scenario 3: Hot Deployment**
```
✅ Stop Site: No
✅ Stop App Pool: No
✅ Create Backup: Yes
✅ Start App Pool: No
✅ Start Site: No
✅ Health Check: Yes
Delay: 0 seconds
```
*Use case: Applications supporting hot reload/shadow copy*

## Benefits

✅ **Flexibility** - Each app can have its own deployment strategy  
✅ **Faster Deployments** - Skip unnecessary steps  
✅ **Reduced Downtime** - Don't stop services if not needed  
✅ **Better Control** - Fine-tune deployment behavior  
✅ **Clear Logging** - Deployment steps show what was skipped and why  

## Database Changes

The new properties are added to the `Applications` table. Since we're using `EnsureCreated()`, the database will be recreated with the new schema on next run.

For existing databases, you would need to:
1. Add the columns manually, OR
2. Drop and recreate the database

## Rollback Behavior

- If `CreateBackup = false`, the deployment is marked as **non-rollbackable**
- Rollback requires a backup to exist
- First deployments are always non-rollbackable (no previous version)

## Default Behavior

All new applications default to **full deployment** with all steps enabled:
- All checkboxes checked
- 2-second delay
- This matches the previous behavior

## Testing

To test different configurations:

1. Create an application with custom settings
2. Deploy a package
3. Check deployment logs to verify which steps ran
4. Adjust settings and deploy again

---

**Ready to use!** Configure each application's deployment steps based on its specific requirements. 🚀
