-- ReleaseFlow Database Schema
-- Generated for SQL Server
-- Date: 2025-12-17

USE master;
GO

-- Create database if not exists
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'ReleaseFlow')
BEGIN
    CREATE DATABASE ReleaseFlow;
END
GO

USE ReleaseFlow;
GO

-- =============================================
-- Table: ReleaseFlow_Applications
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ReleaseFlow_Applications]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[ReleaseFlow_Applications] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [Name] NVARCHAR(256) NOT NULL,
        [Description] NVARCHAR(MAX) NULL,
        [IISSiteName] NVARCHAR(256) NOT NULL,
        [AppPoolName] NVARCHAR(256) NOT NULL,
        [PhysicalPath] NVARCHAR(500) NOT NULL,
        [Environment] NVARCHAR(50) NOT NULL,
        [HealthCheckUrl] NVARCHAR(500) NULL,
        [ApplicationPath] NVARCHAR(256) NOT NULL DEFAULT '/',
        [IsActive] BIT NOT NULL DEFAULT 1,
        [IsDiscovered] BIT NOT NULL DEFAULT 0,
        [LastDiscoveredAt] DATETIME2 NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt] DATETIME2 NULL,
        [StopSiteBeforeDeployment] BIT NOT NULL DEFAULT 1,
        [StopAppPoolBeforeDeployment] BIT NOT NULL DEFAULT 1,
        [StartAppPoolAfterDeployment] BIT NOT NULL DEFAULT 1,
        [StartSiteAfterDeployment] BIT NOT NULL DEFAULT 1,
        [CreateBackup] BIT NOT NULL DEFAULT 1,
        [RunHealthCheck] BIT NOT NULL DEFAULT 1,
        [DeploymentDelaySeconds] INT NOT NULL DEFAULT 2,
        CONSTRAINT [PK_ReleaseFlow_Applications] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [UK_ReleaseFlow_Applications_Name] UNIQUE ([Name])
    );
    
    CREATE INDEX [IX_ReleaseFlow_Applications_IISSiteName] ON [dbo].[ReleaseFlow_Applications]([IISSiteName]);
    CREATE INDEX [IX_ReleaseFlow_Applications_IsActive] ON [dbo].[ReleaseFlow_Applications]([IsActive]);
END
GO

-- =============================================
-- Table: ReleaseFlow_Deployments
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ReleaseFlow_Deployments]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[ReleaseFlow_Deployments] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [ApplicationId] INT NOT NULL,
        [Version] NVARCHAR(100) NOT NULL,
        [DeployedByUsername] NVARCHAR(256) NOT NULL,
        [Status] NVARCHAR(50) NOT NULL,
        [StartedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [CompletedAt] DATETIME2 NULL,
        [ZipFileName] NVARCHAR(500) NULL,
        [ZipFileSize] BIGINT NULL,
        [BackupPath] NVARCHAR(500) NULL,
        [CanRollback] BIT NOT NULL DEFAULT 0,
        [ErrorMessage] NVARCHAR(MAX) NULL,
        CONSTRAINT [PK_ReleaseFlow_Deployments] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_ReleaseFlow_Deployments_Applications] FOREIGN KEY ([ApplicationId]) 
            REFERENCES [dbo].[ReleaseFlow_Applications]([Id]) ON DELETE CASCADE
    );
    
    CREATE INDEX [IX_ReleaseFlow_Deployments_ApplicationId] ON [dbo].[ReleaseFlow_Deployments]([ApplicationId]);
    CREATE INDEX [IX_ReleaseFlow_Deployments_StartedAt] ON [dbo].[ReleaseFlow_Deployments]([StartedAt] DESC);
    CREATE INDEX [IX_ReleaseFlow_Deployments_Status] ON [dbo].[ReleaseFlow_Deployments]([Status]);
END
GO

-- =============================================
-- Table: ReleaseFlow_DeploymentSteps
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ReleaseFlow_DeploymentSteps]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[ReleaseFlow_DeploymentSteps] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [DeploymentId] INT NOT NULL,
        [StepName] NVARCHAR(200) NOT NULL,
        [Status] NVARCHAR(50) NOT NULL,
        [StartedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [CompletedAt] DATETIME2 NULL,
        [ErrorMessage] NVARCHAR(MAX) NULL,
        [Details] NVARCHAR(MAX) NULL,
        CONSTRAINT [PK_ReleaseFlow_DeploymentSteps] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_ReleaseFlow_DeploymentSteps_Deployments] FOREIGN KEY ([DeploymentId]) 
            REFERENCES [dbo].[ReleaseFlow_Deployments]([Id]) ON DELETE CASCADE
    );
    
    CREATE INDEX [IX_ReleaseFlow_DeploymentSteps_DeploymentId] ON [dbo].[ReleaseFlow_DeploymentSteps]([DeploymentId]);
END
GO

-- =============================================
-- Table: ReleaseFlow_AuditLogs
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ReleaseFlow_AuditLogs]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[ReleaseFlow_AuditLogs] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [Username] NVARCHAR(256) NOT NULL,
        [Action] NVARCHAR(100) NOT NULL,
        [EntityType] NVARCHAR(100) NOT NULL,
        [EntityId] NVARCHAR(50) NULL,
        [Details] NVARCHAR(MAX) NULL,
        [IpAddress] NVARCHAR(50) NOT NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_ReleaseFlow_AuditLogs] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    
    CREATE INDEX [IX_ReleaseFlow_AuditLogs_CreatedAt] ON [dbo].[ReleaseFlow_AuditLogs]([CreatedAt] DESC);
    CREATE INDEX [IX_ReleaseFlow_AuditLogs_Username] ON [dbo].[ReleaseFlow_AuditLogs]([Username]);
    CREATE INDEX [IX_ReleaseFlow_AuditLogs_Action] ON [dbo].[ReleaseFlow_AuditLogs]([Action]);
END
GO

-- =============================================
-- Table: ReleaseFlow_AppSettings
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ReleaseFlow_AppSettings]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[ReleaseFlow_AppSettings] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [Key] NVARCHAR(100) NOT NULL,
        [Value] NVARCHAR(MAX) NOT NULL,
        [Description] NVARCHAR(500) NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt] DATETIME2 NULL,
        CONSTRAINT [PK_ReleaseFlow_AppSettings] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [UK_ReleaseFlow_AppSettings_Key] UNIQUE ([Key])
    );
END
GO

PRINT 'Database schema created successfully';
GO
