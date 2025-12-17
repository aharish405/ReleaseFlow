# Authentication Setup - Development Mode

## Changes Made

### 1. Switched from Windows Authentication to Cookie Authentication

**Why?** Windows Authentication only works when hosted in IIS. For development with Kestrel (dotnet run), we need cookie-based authentication.

### 2. Created Login System

**New Files:**
- `Controllers/AccountController.cs` - Handles login/logout
- `Views/Account/Login.cshtml` - Login page

### 3. Auto-User Creation

For development convenience, any username entered will automatically create a SuperAdmin user.

## How to Use

### 1. Start the Application

```powershell
cd c:\Users\ahari\source\ReleaseFlow
dotnet run --project ReleaseFlow/ReleaseFlow.csproj
```

### 2. Access the Login Page

Navigate to: `https://localhost:5001`

You'll be automatically redirected to the login page.

### 3. Login

- Enter any username (e.g., "admin", "testuser", etc.)
- Click "Sign In"
- The system will automatically create a SuperAdmin account for that username

### 4. You're In!

You'll be redirected to the dashboard with full SuperAdmin access.

### 5. Logout

Click on your username in the top-right corner and select "Logout"

## Features

✅ **Auto-Create Users** - Any username creates a SuperAdmin account  
✅ **Persistent Login** - Stay logged in for 8 hours  
✅ **Secure Cookies** - Cookie-based authentication  
✅ **Role-Based Access** - Full RBAC support  
✅ **Easy Logout** - Dropdown menu in navigation  

## Production Deployment

For production deployment with IIS and Windows Authentication:

1. Update `Program.cs` to use Windows Authentication
2. Configure IIS with Windows Authentication enabled
3. Map AD users to roles in the database

## Database Initialization

On first run, the application will:
1. Create all database tables in SQL Server
2. Seed default roles (SuperAdmin, Deployer, ReadOnly)
3. Create default settings
4. Ready to accept logins

## Troubleshooting

### "Database not initialized"

**Solution:** Restart the application. The database will be created automatically.

### Cannot login

**Solution:** Check that SQL Server is running and the connection string is correct.

### Redirected to login repeatedly

**Solution:** Clear browser cookies and try again.

---

**Ready to test!** Just run the application and login with any username.
