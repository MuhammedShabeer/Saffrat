-- Accounting Engine Middleware Schema Setup
-- Run this script to generate the underlying tables for the AccountingEngine.
-- After running, use Scaffold-DbContext or dotnet ef dbcontext scaffold to update your models (Database First approach).

BEGIN TRANSACTION;

-- 1. AppSettings Extensions (Single-Tenant Settings)
-- Adding FinancialYearStart to your existing AppSettings table if not already present
IF NOT EXISTS(SELECT 1 FROM sys.columns WHERE Name = N'FinancialYearStart' AND Object_ID = Object_ID(N'dbo.AppSettings'))
BEGIN
    ALTER TABLE [dbo].[AppSettings] ADD [FinancialYearStart] DATETIME NULL;
END

-- 2. GLAccounts (Chart of Accounts) table
CREATE TABLE [dbo].[GLAccounts] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [AccountCode] NVARCHAR(50) NOT NULL,
    [AccountName] NVARCHAR(255) NOT NULL,
    [Description] NVARCHAR(MAX) NULL,
    [Category] INT NOT NULL,  -- (0: Asset, 1: Liability, 2: Equity, 3: Revenue, 4: Expense)
    [Type] INT NOT NULL,      -- Mapped to the AccountType Enum
    [CurrentBalance] DECIMAL(18, 2) NOT NULL DEFAULT 0.00,
    [IsActive] BIT NOT NULL DEFAULT 1
);

-- 3. JournalEntries table
CREATE TABLE [dbo].[JournalEntries] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [ReferenceNumber] NVARCHAR(100) NOT NULL,
    [Description] NVARCHAR(MAX) NULL,
    [EntryDate] DATETIME NOT NULL,
    [IsPosted] BIT NOT NULL DEFAULT 0,
    [SourceDocumentType] NVARCHAR(100) NULL,
    [SourceDocumentId] INT NULL,
    [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE()
);

-- 4. LedgerEntries (The Debits and Credits) table
CREATE TABLE [dbo].[LedgerEntries] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [JournalEntryId] INT NOT NULL FOREIGN KEY REFERENCES [dbo].[JournalEntries]([Id]) ON DELETE CASCADE,
    [GLAccountId] INT NOT NULL FOREIGN KEY REFERENCES [dbo].[GLAccounts]([Id]),
    [Description] NVARCHAR(MAX) NULL,
    [Debit] DECIMAL(18, 2) NOT NULL DEFAULT 0.00,
    [Credit] DECIMAL(18, 2) NOT NULL DEFAULT 0.00
);

-- 5. Invoices (AR) table
CREATE TABLE [dbo].[Invoices] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [CustomerId] INT NOT NULL FOREIGN KEY REFERENCES [dbo].[Customers]([Id]),
    [InvoiceNumber] NVARCHAR(100) NOT NULL,
    [IssueDate] DATETIME NOT NULL,
    [DueDate] DATETIME NOT NULL,
    [TotalAmount] DECIMAL(18, 2) NOT NULL DEFAULT 0.00,
    [AmountPaid] DECIMAL(18, 2) NOT NULL DEFAULT 0.00,
    [Status] NVARCHAR(50) NOT NULL DEFAULT 'Draft',
    [JournalEntryId] INT NULL FOREIGN KEY REFERENCES [dbo].[JournalEntries]([Id])
);

-- 6. Bills (AP) table
CREATE TABLE [dbo].[Bills] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [SupplierId] INT NOT NULL FOREIGN KEY REFERENCES [dbo].[Suppliers]([Id]),
    [BillNumber] NVARCHAR(100) NOT NULL,
    [Date] DATETIME NOT NULL,
    [DueDate] DATETIME NOT NULL,
    [TotalAmount] DECIMAL(18, 2) NOT NULL DEFAULT 0.00,
    [AmountPaid] DECIMAL(18, 2) NOT NULL DEFAULT 0.00,
    [Status] NVARCHAR(50) NOT NULL DEFAULT 'Draft',
    [JournalEntryId] INT NULL FOREIGN KEY REFERENCES [dbo].[JournalEntries]([Id])
);

COMMIT TRANSACTION;
GO
