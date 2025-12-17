-- ReleaseFlow Database Schema Update
-- Add Deployment Exclusion Support
-- Run this script to add ExcludedPaths column to Applications table

USE ReleaseFlow;
GO

-- Add ExcludedPaths column to Applications table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ReleaseFlow_Applications]') AND name = 'ExcludedPaths')
BEGIN
    ALTER TABLE ReleaseFlow_Applications
    ADD ExcludedPaths NVARCHAR(MAX) NULL;
    
    PRINT 'ExcludedPaths column added successfully';
END
ELSE
BEGIN
    PRINT 'ExcludedPaths column already exists';
END
GO

-- Update existing applications with common exclusions (optional)
UPDATE ReleaseFlow_Applications
SET ExcludedPaths = 'web.config,appsettings.json,appsettings.*.json'
WHERE ExcludedPaths IS NULL;
GO

PRINT 'Schema update completed successfully';
GO
