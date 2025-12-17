# ReleaseFlow

**Enterprise IIS Deployment Management System**

ReleaseFlow is a modern ASP.NET Core MVC application designed to streamline and automate IIS application deployments with comprehensive monitoring, rollback capabilities, and audit trails.

![Version](https://img.shields.io/badge/version-1.0.0-blue)
![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![License](https://img.shields.io/badge/license-MIT-green)

---

## 🎯 Key Features

- **🚀 Automated Deployments** - One-click deployment with configurable pre/post actions
- **🔄 Smart Rollback** - Instant rollback to previous versions with automatic backup restoration
- **📊 Real-time Monitoring** - Live deployment progress with Azure DevOps-style console output
- **🔍 IIS Auto-Discovery** - Automatically detect and register IIS applications
- **📝 Audit Trail** - Complete deployment history with detailed step-by-step logs
- **⚙️ Flexible Configuration** - Per-application deployment settings and exclusions
- **🎨 Modern UI** - Beautiful, responsive interface with Bootstrap 5 and custom gradients

---

## 🏗️ Architecture

### High-Level Architecture

```mermaid
graph TB
    subgraph "Presentation Layer"
        UI[ASP.NET MVC Views]
        Controllers[MVC Controllers]
    end
    
    subgraph "Business Logic Layer"
        DS[Deployment Service]
        RS[Rollback Service]
        BS[Backup Service]
        IISS[IIS Services]
        AS[Audit Service]
    end
    
    subgraph "Data Access Layer"
        Repos[Repositories<br/>ADO.NET]
        DB[(SQL Server<br/>Database)]
    end
    
    subgraph "External Systems"
        IIS[IIS Manager]
        FS[File System]
    end
    
    UI --> Controllers
    Controllers --> DS
    Controllers --> RS
    Controllers --> AS
    DS --> BS
    DS --> IISS
    RS --> BS
    RS --> IISS
    IISS --> IIS
    DS --> FS
    BS --> FS
    Controllers --> Repos
    DS --> Repos
    RS --> Repos
    AS --> Repos
    Repos --> DB
    
    style UI fill:#667eea
    style Controllers fill:#764ba2
    style DS fill:#f093fb
    style DB fill:#4facfe
```

### Technology Stack

| Layer | Technology |
|-------|-----------|
| **Frontend** | ASP.NET Core MVC, Bootstrap 5, Bootstrap Icons |
| **Backend** | .NET 8, C# 12 |
| **Data Access** | ADO.NET (Raw SQL) |
| **Database** | SQL Server 2019+ |
| **IIS Management** | Microsoft.Web.Administration |
| **Logging** | Serilog (File + Database) |
| **Grid** | NonFactors MVC Grid |

---

## 📊 Database Schema

```mermaid
erDiagram
    Applications ||--o{ Deployments : has
    Deployments ||--o{ DeploymentSteps : contains
    
    Applications {
        int Id PK
        string Name
        string Environment
        string IISSiteName
        string AppPoolName
        string PhysicalPath
        string ExcludedPaths
        bool StopSiteBeforeDeployment
        bool CreateBackup
        datetime CreatedAt
    }
    
    Deployments {
        int Id PK
        int ApplicationId FK
        string Version
        string Status
        string BackupPath
        bool CanRollback
        datetime StartedAt
        datetime CompletedAt
    }
    
    DeploymentSteps {
        int Id PK
        int DeploymentId FK
        int StepNumber
        string StepName
        string Status
        string Message
        datetime StartedAt
        datetime CompletedAt
    }
    
    AuditLogs {
        int Id PK
        string Username
        string Action
        string EntityType
        string Details
        datetime CreatedAt
    }
```

---

## 🔄 Deployment Flow

### Standard Deployment Process

```mermaid
sequenceDiagram
    participant User
    participant UI
    participant Controller
    participant DeploymentService
    participant BackupService
    participant IIS
    participant FileSystem
    participant DB
    
    User->>UI: Upload ZIP & Start Deployment
    UI->>Controller: POST /Deployments/Deploy
    Controller->>DeploymentService: DeployAsync()
    
    DeploymentService->>DB: Create Deployment Record
    DeploymentService->>DB: Log Step: Initialized
    
    DeploymentService->>IIS: Stop IIS Site
    DeploymentService->>DB: Log Step: Site Stopped
    
    DeploymentService->>IIS: Stop App Pool
    DeploymentService->>DB: Log Step: App Pool Stopped
    
    DeploymentService->>BackupService: CreateBackupAsync()
    BackupService->>FileSystem: ZIP Current Files
    BackupService-->>DeploymentService: Backup Path
    DeploymentService->>DB: Log Step: Backup Created
    
    DeploymentService->>FileSystem: Extract ZIP to Temp
    DeploymentService->>FileSystem: Copy Files (Skip Exclusions)
    DeploymentService->>DB: Log Step: Files Replaced
    
    DeploymentService->>IIS: Start App Pool
    DeploymentService->>DB: Log Step: App Pool Started
    
    DeploymentService->>IIS: Start IIS Site
    DeploymentService->>DB: Log Step: Site Started
    
    opt Health Check Enabled
        DeploymentService->>IIS: HTTP GET Health Check URL
        DeploymentService->>DB: Log Step: Health Check Passed
    end
    
    DeploymentService->>DB: Update Deployment Status: Succeeded
    DeploymentService-->>Controller: Deployment Result
    Controller-->>UI: Redirect to Details
    UI-->>User: Show Success
```

### Rollback Flow

```mermaid
sequenceDiagram
    participant User
    participant Controller
    participant RollbackService
    participant BackupService
    participant IIS
    participant FileSystem
    participant DB
    
    User->>Controller: Click Rollback
    Controller->>RollbackService: RollbackAsync()
    
    RollbackService->>DB: Get Deployment & Backup Path
    
    RollbackService->>IIS: Stop Site & App Pool
    RollbackService->>DB: Log: Services Stopped
    
    RollbackService->>BackupService: RestoreBackupAsync()
    BackupService->>FileSystem: Clear Current Files
    BackupService->>FileSystem: Extract Backup ZIP
    BackupService-->>RollbackService: Success
    
    RollbackService->>IIS: Start App Pool & Site
    RollbackService->>DB: Log: Services Started
    
    RollbackService->>DB: Create Rollback Deployment Record
    RollbackService-->>Controller: Success
    Controller-->>User: Rollback Complete
```

---

## 🎨 Application Flow

### User Journey: Deploy Application

```mermaid
graph LR
    A[Login] --> B[Dashboard]
    B --> C{Action?}
    C -->|New App| D[Register Application]
    C -->|Deploy| E[Select Application]
    
    D --> F[Configure Settings]
    F --> G[Set Exclusions]
    G --> H[Save]
    
    E --> I[Upload ZIP]
    I --> J[Enter Version]
    J --> K[Start Deployment]
    K --> L[Monitor Progress]
    L --> M{Success?}
    M -->|Yes| N[View Details]
    M -->|No| O[View Errors]
    O --> P{Rollback?}
    P -->|Yes| Q[Confirm Rollback]
    Q --> R[Restore Previous]
    
    style A fill:#667eea
    style K fill:#f093fb
    style M fill:#ffeaa7
    style Q fill:#ff6b6b
```

---

## 📁 Project Structure

```
ReleaseFlow/
├── Controllers/           # MVC Controllers
│   ├── ApplicationsController.cs
│   ├── DeploymentsController.cs
│   ├── AppPoolsController.cs
│   └── AuditController.cs
├── Services/             # Business Logic
│   ├── Deployment/
│   │   ├── DeploymentService.cs
│   │   ├── RollbackService.cs
│   │   └── BackupService.cs
│   ├── IIS/
│   │   ├── SiteService.cs
│   │   ├── AppPoolService.cs
│   │   └── IISDiscoveryService.cs
│   └── AuditService.cs
├── Data/                 # Data Access Layer
│   ├── Repositories/
│   │   ├── ApplicationRepository.cs
│   │   ├── DeploymentRepository.cs
│   │   └── AuditLogRepository.cs
│   └── SqlHelper.cs
├── Models/               # Domain Models
│   ├── Application.cs
│   ├── Deployment.cs
│   ├── DeploymentStep.cs
│   └── AuditLog.cs
├── Views/                # Razor Views
│   ├── Applications/
│   ├── Deployments/
│   ├── AppPools/
│   └── Shared/
└── wwwroot/             # Static Files
    ├── css/
    └── js/
```

---

## 🚀 Getting Started

### Prerequisites

- .NET 8 SDK
- SQL Server 2019+
- IIS 10+ (Windows Server 2016+)
- Administrator privileges (for IIS management)

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/yourusername/ReleaseFlow.git
   cd ReleaseFlow
   ```

2. **Configure database connection**
   ```json
   // appsettings.json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost;Database=ReleaseFlowDB;Trusted_Connection=True;TrustServerCertificate=True;"
     }
   }
   ```

3. **Create database**
   ```bash
   # Run database creation script
   sqlcmd -S localhost -i Database/CreateDatabase.sql
   ```

4. **Build and run**
   ```bash
   dotnet build
   dotnet run
   ```

5. **Access application**
   ```
   https://localhost:5001
   ```

### First-Time Setup

1. **Auto-discover IIS applications**
   - Navigate to Applications → Auto-Discover
   - System will scan IIS and register applications

2. **Configure application**
   - Edit discovered application
   - Set deployment options
   - Configure exclusions (e.g., `web.config,StaticContent`)

3. **Deploy**
   - Go to Deployments → New Deployment
   - Select application
   - Upload ZIP package
   - Monitor real-time progress

---

## ⚙️ Configuration

### Application Settings

Each application can be configured with:

| Setting | Description | Example |
|---------|-------------|---------|
| **Stop Site Before Deployment** | Stop IIS site before copying files | ✅ Enabled |
| **Stop App Pool Before Deployment** | Stop application pool | ✅ Enabled |
| **Create Backup** | Backup current files before deployment | ✅ Enabled |
| **Start Services After** | Auto-start site and pool after deployment | ✅ Enabled |
| **Health Check** | Verify application after deployment | URL: `/health` |
| **Deployment Delay** | Wait time after stopping services | 2 seconds |
| **Excluded Paths** | Files/folders to preserve | `web.config,StaticContent` |

### Exclusion Patterns

Supports flexible exclusion patterns:

```
# Exact file names
web.config,appsettings.json

# Exact folder names
StaticContent,Documents,Uploads

# Wildcards
*.config,appsettings.*.json,Uploads/*.pdf

# Combined
web.config,StaticContent,*.config,Uploads
```

---

## 📸 Screenshots

### Dashboard
Modern overview with deployment statistics and recent activity.

### Deployment Console
Real-time deployment progress with Azure DevOps-style step visualization.

### Application Management
Grid-based application listing with filtering, sorting, and quick actions.

---

## 🔐 Security

- **Windows Authentication** - Integrated with Active Directory
- **Audit Logging** - All actions tracked with user, timestamp, and IP
- **Administrator Privileges** - Required for IIS management operations
- **Backup Encryption** - Optional encryption for backup files

---

## 📝 Deployment Best Practices

1. **Always enable backups** - Allows instant rollback
2. **Use exclusions wisely** - Preserve config files and user content
3. **Test health checks** - Ensure application is accessible post-deployment
4. **Monitor logs** - Review deployment steps for issues
5. **Rollback quickly** - Don't hesitate to rollback on errors

---

## 🤝 Contributing

Contributions are welcome! Please follow these guidelines:

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request

---

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

---

## 🆘 Support

For issues and questions:
- **GitHub Issues**: [Create an issue](https://github.com/yourusername/ReleaseFlow/issues)
- **Documentation**: See `/docs` folder
- **Email**: support@releaseflow.com

---

## 🎯 Roadmap

- [ ] Multi-server deployment support
- [ ] Deployment scheduling
- [ ] Email notifications
- [ ] API for CI/CD integration
- [ ] Docker container support
- [ ] Kubernetes deployment

---

**Built with ❤️ using ASP.NET Core**
