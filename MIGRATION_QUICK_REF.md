# EF to ADO.NET Migration - Quick Reference

## ✅ Completed (60%)
- All repository infrastructure
- Program.cs DI registration
- AuditService
- ApplicationsController (partial)

## 🔧 Remaining Work - Find/Replace Patterns

### ApplicationsController - Remaining Methods

**Details method (line ~44):**
```csharp
// FIND:
var application = await _context.Applications
    .Include(a => a.Deployments.OrderByDescending(d => d.StartedAt).Take(10))
    .FirstOrDefaultAsync(a => a.Id == id);

// REPLACE WITH:
var application = await _applicationRepository.GetByIdAsync(id);
// Note: Deployments will need separate query if needed
```

**Create POST (line ~90):**
```csharp
// FIND:
_context.Add(application);
await _context.SaveChangesAsync();

// REPLACE WITH:
application.Id = await _applicationRepository.CreateAsync(application);
```

**Edit POST (line ~140):**
```csharp
// FIND:
_context.Update(application);
await _context.SaveChangesAsync();

// REPLACE WITH:
await _applicationRepository.UpdateAsync(application);
```

**Delete (line ~180):**
```csharp
// FIND:
var application = await _context.Applications.FindAsync(id);
_context.Applications.Remove(application);
await _context.SaveChangesAsync();

// REPLACE WITH:
await _applicationRepository.DeleteAsync(id);
```

**ApplicationExists (line ~210):**
```csharp
// FIND:
return _context.Applications.Any(e => e.Id == id);

// REPLACE WITH:
return await _applicationRepository.ExistsAsync(id);
```

### DeploymentsController

**Constructor:**
```csharp
private readonly IApplicationRepository _applicationRepository;
private readonly IDeploymentRepository _deploymentRepository;
```

**Index:**
```csharp
var deployments = await _deploymentRepository.GetByApplicationIdAsync(applicationId.Value);
// OR
var deployments = await _deploymentRepository.GetRecentAsync(50);
```

**Details:**
```csharp
var deployment = await _deploymentRepository.GetByIdAsync(id);
```

**Deploy GET:**
```csharp
var applications = await _applicationRepository.GetActiveAsync();
```

### DashboardController

```csharp
private readonly IApplicationRepository _applicationRepository;
private readonly IDeploymentRepository _deploymentRepository;

var totalApps = (await _applicationRepository.GetAllAsync()).Count();
var recentDeployments = await _deploymentRepository.GetRecentAsync(5);
```

## 🗑️ Files to Delete

After all updates work:
```powershell
Remove-Item "ReleaseFlow\Data\ApplicationDbContext.cs"
Remove-Item "ReleaseFlow\Data\DbInitializer.cs"
Remove-Item "ReleaseFlow\Migrations" -Recurse
```

## 🗄️ Database Setup

```powershell
# Run schema
sqlcmd -S HARRISH-PC -U sa -P Admin@12345 -i Database\Schema.sql

# Verify tables
sqlcmd -S HARRISH-PC -U sa -P Admin@12345 -d ReleaseFlow -Q "SELECT name FROM sys.tables WHERE name LIKE 'ReleaseFlow_%'"
```

## 🧪 Build & Test

```powershell
# Clean
dotnet clean

# Restore
dotnet restore

# Build
dotnet build

# Run
dotnet run
```

## 📝 Testing Checklist

- [ ] App starts
- [ ] Login works
- [ ] Applications list loads
- [ ] Can create application
- [ ] Can edit application
- [ ] Can delete application
- [ ] Auto-discover works
- [ ] Can deploy
- [ ] Audit logs work
- [ ] Dashboard loads

## ⚠️ If Build Fails

Common issues:
1. **"ApplicationDbContext not found"** - Remove `using ReleaseFlow.Data;` if only used for DbContext
2. **"SaveChangesAsync not found"** - Replace with repository Update/Create methods
3. **"Include not found"** - Remove `.Include()` calls, use separate repository queries
4. **"FirstOrDefaultAsync not found"** - Use repository Get methods instead

## 🎯 Estimated Time Remaining

- Controller updates: 2 hours
- Service updates: 2 hours
- Testing: 1 hour
- **Total: ~5 hours**
