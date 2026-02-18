
-- 1. Create Tenants Table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Tenants' and xtype='U')
BEGIN
    CREATE TABLE Tenants (
        Id NVARCHAR(150) NOT NULL PRIMARY KEY, -- Subdomain as ID
        Name NVARCHAR(150) NOT NULL,
        Host NVARCHAR(MAX) NULL,
        ConnectionString NVARCHAR(MAX) NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAt DATETIME NOT NULL DEFAULT GETUTCDATE()
    );
END

-- 2. Update Departments Table (Example)
IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'TenantId' AND Object_ID = Object_ID(N'Departments'))
BEGIN
    ALTER TABLE Departments ADD TenantId NVARCHAR(450) NULL; 
    CREATE INDEX IX_Departments_TenantId ON Departments(TenantId);
END

-- 3. Update Users Table
IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'TenantId' AND Object_ID = Object_ID(N'Users'))
BEGIN
    ALTER TABLE Users ADD TenantId NVARCHAR(450) NULL; 
    -- Note: If you have unique constraints on UserName/Email, you should update them to be composed with TenantId
    -- CREATE UNIQUE INDEX IX_Users_TenantId_UserName ON Users(TenantId, UserName) WHERE TenantId IS NOT NULL;
END

-- Repeat for all other tables implementing IMustHaveTenant
