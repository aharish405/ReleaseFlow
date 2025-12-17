# ReleaseFlow - Quick Start Guide

## Database Setup Complete ✅

The application has been configured to use **SQL Server** with the following connection:
- **Server**: HARRISH-PC
- **Database**: DataSyncDb
- **Authentication**: SQL Server (sa/Admin@12345)

## Next Steps

### 1. Run the Application

```powershell
cd c:\Users\ahari\source\ReleaseFlow
dotnet run --project ReleaseFlow/ReleaseFlow.csproj
```

The application will:
- Automatically create all database tables on first run
- Seed default roles (SuperAdmin, Deployer, ReadOnly)
- Create default settings
- Create an admin user with your Windows identity

### 2. Access the Application

Open your browser and navigate to:
- **HTTPS**: `https://localhost:5001`
- **HTTP**: `http://localhost:5000`

### 3. First-Time Setup

#### Register Your First Application

1. Navigate to **Applications** → **Register Application**
2. Fill in the details:
   - **Name**: Your application name
   - **Environment**: Development/Staging/Production
   - **IIS Site Name**: The IIS site name (e.g., "Default Web Site")
   - **App Pool Name**: The application pool name (e.g., "DefaultAppPool")
   - **Physical Path**: Where files will be deployed (e.g., `C:\inetpub\wwwroot\MyApp`)
   - **Health Check URL** (optional): URL to verify deployment (e.g., `http://localhost/health`)

#### Prepare a Deployment Package

Create a ZIP file with your application files:

```
MyApp_v1.0.0.zip
├── bin/
├── wwwroot/
├── appsettings.json
└── web.config
```

#### Deploy Your Application

1. Navigate to **Deployments** → **New Deployment**
2. Select your application
3. Enter version number (e.g., "1.0.0")
4. Upload your ZIP file
5. Click **Deploy Now**

The system will:
- ✅ Stop the IIS site and app pool
- ✅ Create a backup of current files
- ✅ Deploy new files
- ✅ Restart services
- ✅ Run health checks
- ✅ Log all steps

### 4. Monitor Deployments

- View deployment history in **Deployments** → **History**
- Click on any deployment to see detailed step-by-step logs
- Rollback if needed (one-click rollback available for successful deployments)

### 5. Manage IIS

- **Sites**: View and control IIS sites (start/stop/restart)
- **App Pools**: Manage application pools (recycle/start/stop)

### 6. View Audit Logs

Navigate to **Audit Logs** to see all system operations including:
- Deployments
- Rollbacks
- IIS site/app pool operations
- User actions

## Important Notes

### Windows Authentication

The application uses Windows Authentication. Make sure:
- You're running on a Windows machine
- IIS is installed (for production deployment)
- You have administrator privileges for IIS management

### Permissions Required

The application needs:
- **Administrator rights** to manage IIS
- **Read/Write access** to deployment and backup directories
- **Database access** to SQL Server

### Default Directories

The application will use these directories (created automatically):
- **Deployments**: `C:\ReleaseFlow\Deployments`
- **Backups**: `C:\ReleaseFlow\Backups`
- **Logs**: `logs/` folder in application directory

### Backup Retention

- Backups are kept for **30 days** by default
- Automatic cleanup runs periodically
- Configure in **Settings** if needed

## Troubleshooting

### Cannot Connect to Database

**Error**: "Cannot open database"

**Solution**: Ensure SQL Server is running and the connection string is correct in `appsettings.json`

### Access Denied when Managing IIS

**Error**: "Access denied" when starting/stopping sites

**Solution**: Run the application with administrator privileges:
```powershell
# Run as administrator
Start-Process powershell -Verb RunAs
cd c:\Users\ahari\source\ReleaseFlow
dotnet run --project ReleaseFlow/ReleaseFlow.csproj
```

### Deployment Fails

**Error**: Deployment fails during file copy

**Solution**: 
1. Ensure the IIS site and app pool are fully stopped
2. Check that no processes are locking files
3. Verify the physical path exists and is writable

### Health Check Fails

**Error**: Health check times out

**Solution**:
1. Verify the health check URL is correct
2. Ensure the site is accessible
3. Increase timeout in settings if needed

## Production Deployment

### Option 1: IIS Hosting

1. Publish the application:
```powershell
dotnet publish -c Release -o C:\inetpub\ReleaseFlow
```

2. Create IIS site:
   - App Pool: .NET CLR Version = "No Managed Code"
   - Identity: ApplicationPoolIdentity with admin rights
   - Enable Windows Authentication
   - Disable Anonymous Authentication

3. Configure permissions:
   - Grant app pool identity admin rights
   - Grant read/write to deployment directories

### Option 2: Windows Service

1. Install as Windows Service
2. Configure to run with elevated privileges
3. Set startup type to Automatic

## Features Summary

✅ **Deployment Management**
- ZIP-based deployments
- Automatic backups
- One-click rollback
- Health check validation
- Detailed step logging

✅ **IIS Management**
- Site lifecycle control
- App pool management
- Real-time status monitoring

✅ **Security**
- Windows Authentication
- Role-based access control
- Comprehensive audit logging

✅ **User Interface**
- Clean Bootstrap 5 admin interface
- Real-time dashboard
- Alert notifications

## Support

For issues or questions:
1. Check the logs in `logs/` directory
2. Review audit logs in the application
3. Check deployment step details for errors

---

**Version**: 1.0.0  
**Database**: SQL Server (DataSyncDb)  
**Ready to Deploy**: ✅
