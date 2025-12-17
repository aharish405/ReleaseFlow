# ReleaseFlow - IIS Deployment Management System

## Overview

ReleaseFlow is an enterprise-grade, on-premise IIS deployment management application built with ASP.NET Core (.NET 8). It provides secure, automated ZIP-based deployments with full IIS lifecycle management, rollback capabilities, and comprehensive audit logging.

## Features

### Core Capabilities
- **ZIP-Based Deployments**: Upload and deploy applications via ZIP archives
- **Transactional Deployment Engine**: Atomic deployments with automatic rollback on failure
- **IIS Management**: Full control over IIS sites and application pools
- **Rollback Support**: One-click rollback to previous deployment versions
- **Health Checks**: Post-deployment validation with configurable endpoints
- **Audit Logging**: Comprehensive logging of all privileged operations
- **Windows Authentication**: Integrated Windows Authentication with role-based access control

### Security
- **Role-Based Access Control**: Three-tier permission model (SuperAdmin, Deployer, ReadOnly)
- **Windows Authentication**: Seamless integration with Active Directory
- **Audit Trail**: Complete audit log of all system operations
- **Secure File Handling**: Path traversal protection and file validation

### Deployment Features
- Backup creation before each deployment
- Versioned backup retention
- Detailed step-by-step deployment logging
- Automatic site and app pool lifecycle management
- Health check validation
- Rollback on deployment failure

## Prerequisites

- Windows Server 2016 or later
- .NET 8.0 SDK or Runtime
- IIS 10.0 or later
- Administrator privileges (for IIS management)

## Installation

### 1. Clone or Download the Repository

```powershell
git clone <repository-url>
cd ReleaseFlow
```

### 2. Restore Dependencies

```powershell
dotnet restore
```

### 3. Build the Application

```powershell
dotnet build
```

### 4. Configure Application Settings

Edit `appsettings.json` to configure:
- Database connection string
- Deployment and backup paths
- Retention policies
- Upload size limits

```json
{
  "DeploymentBasePath": "C:\\ReleaseFlow\\Deployments",
  "BackupBasePath": "C:\\ReleaseFlow\\Backups",
  "BackupRetentionDays": 30,
  "MaxUploadSizeMB": 500
}
```

### 5. Initialize Database

The database will be automatically created and seeded on first run. The default admin user will be created with the current Windows user identity.

### 6. Run the Application

#### Option A: Development (Kestrel)

```powershell
dotnet run --project ReleaseFlow/ReleaseFlow.csproj
```

Access the application at `https://localhost:5001`

#### Option B: Production (IIS)

1. Publish the application:
```powershell
dotnet publish -c Release -o C:\\inetpub\\ReleaseFlow
```

2. Create IIS Application Pool:
   - Open IIS Manager
   - Create new Application Pool named "ReleaseFlow"
   - Set .NET CLR Version to "No Managed Code"
   - Set Identity to "ApplicationPoolIdentity" or a custom account with admin privileges

3. Create IIS Site:
   - Create new site named "ReleaseFlow"
   - Point to `C:\\inetpub\\ReleaseFlow`
   - Bind to desired port (e.g., 8080)
   - Select "ReleaseFlow" application pool

4. Configure Windows Authentication:
   - Select the ReleaseFlow site
   - Open "Authentication"
   - Disable "Anonymous Authentication"
   - Enable "Windows Authentication"

5. Grant Permissions:
   - Ensure the application pool identity has:
     - Read/Write access to deployment and backup directories
     - Administrator privileges for IIS management

#### Option C: Windows Service

1. Install as Windows Service using `sc.exe` or a service installer
2. Configure service to run with elevated privileges
3. Set startup type to "Automatic"

## Initial Configuration

### 1. User Setup

The first time you run the application, a default admin user will be created using your current Windows identity. To add more users:

1. Log in as SuperAdmin
2. Manually add users to the database with their Windows identities
3. Assign appropriate roles

### 2. Register Applications

Before deploying, register your applications:

1. Navigate to **Applications** → **Create**
2. Fill in:
   - Application Name
   - IIS Site Name
   - App Pool Name
   - Physical Path
   - Environment (Dev/Staging/Production)
   - Health Check URL (optional)

### 3. Prepare Deployment Packages

Create ZIP files containing your application files:

```
MyApp.zip
├── bin/
├── wwwroot/
├── appsettings.json
└── web.config
```

## Usage

### Deploying an Application

1. Navigate to **Deployments** → **Deploy**
2. Select the application
3. Enter version number
4. Upload ZIP file
5. Click **Deploy**

The system will:
- Validate the ZIP file
- Stop the IIS site and app pool
- Create a backup of the current deployment
- Extract and copy new files
- Restart the site and app pool
- Perform health check
- Log all steps

### Rolling Back a Deployment

1. Navigate to **Deployments** → **History**
2. Find the deployment to rollback
3. Click **Rollback**

The system will restore the backup and restart services.

### Managing IIS Sites

Navigate to **IIS Management** → **Sites** to:
- View all IIS sites
- Start/Stop/Restart sites
- View bindings and configuration

### Managing Application Pools

Navigate to **IIS Management** → **App Pools** to:
- View all application pools
- Start/Stop/Recycle pools
- View runtime configuration

### Viewing Audit Logs

Navigate to **Audit Logs** to:
- View all system operations
- Filter by date, action, or user
- Export logs for compliance

## Security Considerations

### Running with Elevated Privileges

ReleaseFlow requires administrator privileges to manage IIS. Ensure:

1. The application pool identity has admin rights
2. Windows Authentication is properly configured
3. Only authorized users have access to the application

### Role-Based Access Control

Three roles are available:

- **SuperAdmin**: Full system access including user management and settings
- **Deployer**: Can deploy applications and manage IIS sites
- **ReadOnly**: View-only access to deployments and IIS status

### Best Practices

1. **Use HTTPS**: Always run over HTTPS in production
2. **Limit Access**: Restrict network access to authorized users only
3. **Regular Backups**: Monitor backup retention and storage
4. **Audit Regularly**: Review audit logs for suspicious activity
5. **Update Regularly**: Keep the application and dependencies up to date

## Troubleshooting

### Common Issues

#### "Access Denied" when managing IIS

**Solution**: Ensure the application is running with administrator privileges.

#### Deployment fails with "Cannot access file"

**Solution**: Check that the IIS site and app pool are fully stopped before file operations.

#### Health check fails

**Solution**: Verify the health check URL is correct and the site is accessible.

#### Database errors

**Solution**: Check that the application has write access to the database file location.

### Logs

Application logs are stored in:
- File: `logs/releaseflow-{date}.txt`
- Database: `Logs` table in SQLite database

## Architecture

### Technology Stack

- **Backend**: ASP.NET Core MVC (.NET 8)
- **Frontend**: Razor Pages + Bootstrap 5
- **IIS Control**: Microsoft.Web.Administration
- **Database**: SQLite (replaceable with SQL Server)
- **Logging**: Serilog
- **Authentication**: Windows Authentication

### Project Structure

```
ReleaseFlow/
├── Controllers/          # MVC Controllers
├── Models/              # Domain entities
├── Views/               # Razor views
├── Services/            # Business logic
│   ├── IIS/            # IIS management services
│   └── Deployment/     # Deployment engine
├── Data/                # Database context
├── Repositories/        # Data access layer
├── Authorization/       # Custom authorization
└── wwwroot/            # Static files
```

## Contributing

This is an enterprise application. For contributions or modifications, please follow standard .NET coding conventions and ensure all changes are thoroughly tested.

## License

Copyright © 2025 ReleaseFlow. All rights reserved.

## Support

For issues or questions, please contact your system administrator.

---

**Version**: 1.0.0  
**Last Updated**: December 2025
