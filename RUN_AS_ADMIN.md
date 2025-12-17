# Quick Fix: Administrator Privileges Required

## Error
```
System.UnauthorizedAccessException
Cannot read configuration file due to insufficient permissions
```

## Cause
ReleaseFlow needs Administrator privileges to access IIS configuration via `Microsoft.Web.Administration`.

## Solution

### Option 1: Run Visual Studio as Administrator (Recommended for Development)

1. **Close Visual Studio** completely
2. **Right-click** Visual Studio icon
3. Select **"Run as administrator"**
4. Open ReleaseFlow solution
5. Run the application (F5)

### Option 2: Run Application as Administrator

1. Build the application
2. Navigate to: `c:\Users\ahari\source\ReleaseFlow\ReleaseFlow\bin\Debug\net8.0\`
3. **Right-click** `ReleaseFlow.exe`
4. Select **"Run as administrator"**

### Option 3: Configure Visual Studio to Always Run as Admin

1. Navigate to Visual Studio installation folder:
   - VS 2022: `C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\`
2. Right-click `devenv.exe`
3. Properties → Compatibility tab
4. Check **"Run this program as an administrator"**
5. Click OK

### Option 4: Run via Command Line

```powershell
# Navigate to project directory
cd c:\Users\ahari\source\ReleaseFlow\ReleaseFlow

# Run with admin (will prompt for elevation)
Start-Process dotnet -ArgumentList "run" -Verb RunAs
```

## Verification

After running as administrator, you should see:
- ✅ Dashboard loads successfully
- ✅ Sites page shows IIS sites
- ✅ App Pools page shows application pools
- ✅ No permission errors

## Why This is Required

ReleaseFlow needs admin privileges to:
- Read IIS configuration (`applicationHost.config`)
- Start/Stop IIS sites and app pools
- Deploy files to IIS directories
- Create/modify IIS applications
- Access system-level IIS management APIs

## Production Deployment

For production IIS hosting:
1. Application pool identity needs IIS management permissions
2. Or run under an account with IIS admin rights
3. Configure Windows Authentication
4. See `AUTHENTICATION.md` for production setup

---

**Quick Start:** Just run Visual Studio as Administrator! 🚀
