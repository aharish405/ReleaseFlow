# Nested Applications Support

## Overview

IIS sites often have nested applications (virtual directories) under them. For example:
- `default` (site)
  - `/enlink` (nested app)
  - `/nextgen` (nested app)
  - `/SMSApp` (nested app)
  - `/sventbyte` (nested app)

This feature adds support to view and manage these nested applications.

## Changes Made

### 1. Data Models

**Added `ApplicationInfo` class:**
```csharp
public class ApplicationInfo
{
    public string Path { get; set; }              // e.g., "/enlink"
    public string PhysicalPath { get; set; }      // e.g., "D:\Sites\enlink"
    public string AppPoolName { get; set; }       // e.g., "DefaultAppPool"
    public bool EnabledProtocols { get; set; }    // HTTP/HTTPS enabled
}
```

**Updated `SiteInfo` class:**
```csharp
public class SiteInfo
{
    // ... existing properties ...
    public List<ApplicationInfo> Applications { get; set; } = new();
}
```

### 2. IIS Service

**Updated `IISSiteService.MapToSiteInfo()`:**
- Now iterates through `site.Applications`
- Skips root application (`/`) as it's already in site info
- Maps each nested application to `ApplicationInfo`
- Populates path, physical path, app pool, and protocol info

```csharp
foreach (var app in site.Applications)
{
    if (app.Path == "/") continue; // Skip root
    
    siteInfo.Applications.Add(new ApplicationInfo
    {
        Path = app.Path,
        PhysicalPath = virtualDir?.PhysicalPath ?? string.Empty,
        AppPoolName = app.ApplicationPoolName ?? string.Empty,
        EnabledProtocols = !string.IsNullOrEmpty(app.EnabledProtocols)
    });
}
```

### 3. Sites Details View

**Added "Nested Applications" section:**
- New card displaying all nested applications
- Table with columns:
  - Application Path (e.g., `/enlink`)
  - Physical Path
  - Application Pool
  - Protocols (Enabled/Disabled)
- Folder icon for visual clarity
- Info message when no nested apps exist

## UI Features

### Display

**Nested Applications Table:**
| Application Path | Physical Path | Application Pool | Protocols |
|-----------------|---------------|------------------|-----------|
| 🗂️ /enlink | D:\Sites\enlink | DefaultAppPool | ✅ Enabled |
| 🗂️ /nextgen | D:\Sites\nextgen | NextGenPool | ✅ Enabled |
| 🗂️ /SMSApp | D:\Sites\SMSApp | SMSAppPool | ✅ Enabled |

**Empty State:**
```
ℹ️ No nested applications found. This site only has the root application.
```

## Benefits

✅ **Complete Visibility** - See all applications under a site  
✅ **Better Management** - Understand site structure  
✅ **App Pool Tracking** - Know which pool each app uses  
✅ **Path Information** - Both virtual and physical paths shown  
✅ **Protocol Status** - See if protocols are enabled  

## Use Cases

### Scenario 1: Multi-Tenant Site
```
default (site)
├── / (root)
├── /client1
├── /client2
└── /client3
```
Each client has their own nested application with separate app pools.

### Scenario 2: Microservices
```
api.company.com (site)
├── / (root)
├── /auth
├── /users
├── /orders
└── /payments
```
Each microservice is a nested application.

### Scenario 3: Versioned APIs
```
api.company.com (site)
├── / (root)
├── /v1
├── /v2
└── /v3
```
Different API versions as nested applications.

## Future Enhancements

Potential additions:
1. **Start/Stop** nested applications individually
2. **Deploy** to specific nested applications
3. **Recycle** app pools for nested apps
4. **Add/Remove** nested applications via UI
5. **Edit** nested application settings
6. **Health checks** for each nested app

## Example from Screenshot

Based on the IIS Manager screenshot:

**Site: default**
- Root: `/`
- Nested Apps:
  - `/enlink`
  - `/nextgen`
  - `/SMSApp`
  - `/sventbyte`

All of these will now be visible in the Sites Details page!

---

**Result:** Complete visibility into IIS site structure with all nested applications displayed! 🎯
