# Deployment Error - IIS Access Issue

## Error
```
Failed to start app pool: default
```

## Root Cause

The application cannot manage IIS because one or more of the following:

1. **IIS is not installed** on this machine
2. **Application is not running with Administrator privileges**
3. **The app pool "default" doesn't exist** in IIS

## Solutions

### Option 1: Run with Administrator Privileges (Recommended for Testing)

```powershell
# Close current instance (Ctrl+C)

# Open PowerShell as Administrator
Start-Process powershell -Verb RunAs

# Navigate to project
cd c:\Users\ahari\source\ReleaseFlow

# Run the application
dotnet run --project ReleaseFlow/ReleaseFlow.csproj
```

### Option 2: Install IIS (If Not Installed)

```powershell
# Run as Administrator
Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServerRole
Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServer
Enable-WindowsOptionalFeature -Online -FeatureName IIS-ManagementConsole
```

After installation, restart your computer.

### Option 3: Create Test IIS Site and App Pool

1. **Open IIS Manager** (Run as Administrator)
   - Press `Win + R`
   - Type `inetmgr`
   - Click OK

2. **Create Application Pool**
   - Right-click "Application Pools"
   - Select "Add Application Pool"
   - Name: `TestAppPool`
   - .NET CLR Version: `No Managed Code` (for .NET Core/8)
   - Click OK

3. **Create Website**
   - Right-click "Sites"
   - Select "Add Website"
   - Site name: `TestSite`
   - Application pool: `TestAppPool`
   - Physical path: `C:\inetpub\wwwroot\TestSite` (create this folder first)
   - Binding: HTTP, Port 8080
   - Click OK

4. **Register in ReleaseFlow**
   - Login to ReleaseFlow
   - Go to Applications → Register Application
   - Fill in:
     - Name: `Test Application`
     - Environment: `Development`
     - IIS Site Name: `TestSite`
     - App Pool Name: `TestAppPool`
     - Physical Path: `C:\inetpub\wwwroot\TestSite`
   - Save

### Option 4: Test Without IIS (Development Mode)

For development/testing without IIS, you can:

1. Browse the application features (Dashboard, Applications list, etc.)
2. Register applications (but don't deploy yet)
3. View audit logs
4. Test the UI

**Note:** Actual deployments require IIS to be installed and the application running with admin privileges.

## Verify IIS Installation

```powershell
# Check if IIS is installed
Get-WindowsFeature -Name Web-Server

# Check IIS service status
Get-Service W3SVC
```

## Verify Administrator Privileges

```powershell
# Check if running as admin
([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
```

Should return `True` if running as admin.

## Next Steps

1. **For Production Use:**
   - Install IIS
   - Run application as Windows Service with elevated privileges
   - Configure proper app pools and sites

2. **For Development/Testing:**
   - Run PowerShell as Administrator
   - Create test IIS site and app pool
   - Test deployment with a simple ZIP file

## Creating a Test ZIP File

```powershell
# Create test content
New-Item -Path "C:\temp\testapp" -ItemType Directory -Force
Set-Content -Path "C:\temp\testapp\index.html" -Value "<h1>Test Deployment v1.0</h1>"

# Create ZIP
Compress-Archive -Path "C:\temp\testapp\*" -DestinationPath "C:\temp\testapp_v1.0.zip" -Force
```

Then upload this ZIP through ReleaseFlow's deployment wizard.

---

**Remember:** ReleaseFlow is an IIS management tool and requires:
- ✅ Windows OS
- ✅ IIS installed
- ✅ Administrator privileges
- ✅ Valid IIS sites and app pools

Once these are in place, deployments will work smoothly!
