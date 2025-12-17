# Nested Application Deployment - Implementation Summary

## Overview
ReleaseFlow now supports deploying to nested IIS applications (e.g., `/enlink`, `/nextgen`) in addition to root applications (`/`).

## What Was Implemented

### 1. Data Model
**Added to `Application` model:**
```csharp
public string ApplicationPath { get; set; } = "/";
```
- Default value: `"/"` (root application)
- Supports nested paths: `"/enlink"`, `"/nextgen"`, etc.
- Must start with `/`

### 2. User Interface

**Applications/Create & Edit:**
- New "Application Path" field
- Pattern validation: `^/.*` (must start with /)
- Help text explaining root vs nested apps
- Examples: `/` for root, `/enlink` for nested

**Applications/Index:**
- New "App Path" column
- Displayed in blue for visibility
- Shows full application structure

**Applications/Details:**
- Application Path row added
- Displayed prominently in application info

### 3. Deployment Service

**Enhanced Logging:**
- Deployment steps now show full target path
- Format: `{SiteName}{ApplicationPath}`
- Example: `default/enlink`
- Helps identify which nested app is being deployed

### 4. Sites Details

**Nested Applications Display:**
- Table showing all nested apps under a site
- Columns: Path, Physical Path, App Pool, Protocols
- Empty state message for sites with only root app

## How It Works

### Registering a Nested Application

1. Navigate to Applications → Register Application
2. Fill in basic info (Name, Environment, etc.)
3. Select IIS Site (e.g., `default`)
4. **Enter Application Path:**
   - `/` for root application
   - `/enlink` for nested application
5. Enter physical path for the application
6. Save

### Deploying to Nested Application

1. Select application from list
2. Click Deploy
3. Upload ZIP file
4. Deployment targets specific application path
5. Only that application's app pool is affected

### Deployment Steps

For nested app `default/enlink`:
```
1. Deployment initialized
2. ZIP validated
3. ZIP extracted
4. Target: default/enlink  ← Shows full path
5. App pool stopped (if configured)
6. Backup created
7. Files replaced
8. App pool started
9. Health check (if configured)
10. Deployment completed
```

## Database Schema

**New Column:**
```sql
ALTER TABLE Applications
ADD ApplicationPath NVARCHAR(255) NOT NULL DEFAULT '/';
```

**To Apply:**
Drop and recreate database (see `DATABASE_UPDATE.md`)

## Examples

### Root Application
```
Name: Main Website
Site: default
App Path: /
Physical Path: C:\inetpub\wwwroot
```

### Nested Application
```
Name: Enlink API
Site: default
App Path: /enlink
Physical Path: D:\Sites\enlink
```

### Multiple Nested Apps on Same Site
```
Site: default
├── / (Main Website)
├── /enlink (Enlink API)
├── /nextgen (NextGen Portal)
└── /SMSApp (SMS Service)
```

Each can be deployed independently!

## Benefits

✅ **Independent Deployments** - Deploy one app without affecting others  
✅ **Better Organization** - Multiple apps per site  
✅ **Clear Visibility** - See exactly which app is being deployed  
✅ **Flexible Structure** - Supports complex IIS configurations  
✅ **Multi-Tenant Ready** - Perfect for hosting multiple clients  

## Current Limitations

⚠️ **Database Update Required** - Must drop/recreate database  
⚠️ **App Pool Granularity** - Stops entire app pool (IIS limitation)  
⚠️ **No Auto-Discovery** - Must manually enter application path  

## Future Enhancements

Potential improvements:
1. **Auto-populate** paths from IIS when selecting site
2. **JavaScript dropdown** to select from existing nested apps
3. **Application-level start/stop** if app has dedicated pool
4. **Deployment history** filtered by application path
5. **Health checks** per nested application

## Testing

**Before Testing:**
1. Drop database: `sqlcmd -S HARRISH-PC -U sa -P Admin@12345 -Q "USE master; ALTER DATABASE ReleaseFlow SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE ReleaseFlow;"`
2. Restart application (database recreates automatically)

**Test Scenarios:**
1. Register root application (`/`)
2. Deploy to root application
3. Register nested application (`/enlink`)
4. Deploy to nested application
5. Verify both apps work independently

## Migration Path

**For Existing Applications:**
- All existing apps will default to `/` (root)
- No manual updates needed
- Can edit later to specify nested path if needed

---

**Status:** ✅ Core implementation complete  
**Next:** Database update required to use the feature
