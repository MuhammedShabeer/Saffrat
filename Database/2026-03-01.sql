-- SQL Script for Saffrat RMS (MS SQL Server)
-- Generated on 2026-03-01
-- Targets: Cash Ledger, Stock Adjustment, Partner Equity

-- 1. Create CashLedgers Table
CREATE TABLE [dbo].[CashLedgers] (
    [Id] [int] IDENTITY(1,1) NOT NULL,
    [EntryDate] [datetime] NOT NULL,
    [Description] [nvarchar](500) NOT NULL,
    [Amount] [decimal](18, 2) NOT NULL,
    [Type] [nvarchar](50) NOT NULL, -- 'Income' or 'Expense'
    [GLAccountId] [int] NULL,
    [JournalEntryId] [int] NULL,
    [CreatedBy] [nvarchar](150) NULL,
    [CreatedAt] [datetime] NOT NULL,
    CONSTRAINT [PK_CashLedgers] PRIMARY KEY CLUSTERED ([Id] ASC)
);

-- 2. Create StockAdjustments Table
CREATE TABLE [dbo].[StockAdjustments] (
    [Id] [int] IDENTITY(1,1) NOT NULL,
    [EntryDate] [datetime] NOT NULL,
    [IngredientItemId] [int] NOT NULL,
    [Quantity] [decimal](18, 2) NOT NULL,
    [Type] [nvarchar](50) NOT NULL, -- 'Addition', 'Subtraction', 'Wastage'
    [Reason] [nvarchar](500) NULL,
    [JournalEntryId] [int] NULL,
    [CreatedBy] [nvarchar](150) NULL,
    [CreatedAt] [datetime] NOT NULL,
    CONSTRAINT [PK_StockAdjustments] PRIMARY KEY CLUSTERED ([Id] ASC)
);

-- 3. Create Partners Table
CREATE TABLE [dbo].[Partners] (
    [Id] [int] IDENTITY(1,1) NOT NULL,
    [Name] [nvarchar](150) NOT NULL,
    [ContactInfo] [nvarchar](250) NULL,
    [GLAccountId] [int] NULL,
    [OwnershipPercentage] [decimal](18, 2) NOT NULL,
    [CreatedBy] [nvarchar](150) NULL,
    [CreatedAt] [datetime] NOT NULL,
    CONSTRAINT [PK_Partners] PRIMARY KEY CLUSTERED ([Id] ASC)
);

-- 4. Create PartnerTransactions Table
CREATE TABLE [dbo].[PartnerTransactions] (
    [Id] [int] IDENTITY(1,1) NOT NULL,
    [PartnerId] [int] NOT NULL,
    [EntryDate] [datetime] NOT NULL,
    [Amount] [decimal](18, 2) NOT NULL,
    [Type] [nvarchar](50) NOT NULL, -- 'Investment', 'Withdrawal', 'ProfitDistribution'
    [Note] [nvarchar](500) NULL,
    [JournalEntryId] [int] NULL,
    [CreatedBy] [nvarchar](150) NULL,
    [CreatedAt] [datetime] NOT NULL,
    CONSTRAINT [PK_PartnerTransactions] PRIMARY KEY CLUSTERED ([Id] ASC)
);

-- Foreign Key Constraints
ALTER TABLE [dbo].[CashLedgers] WITH CHECK ADD CONSTRAINT [FK_CashLedgers_GLAccounts] FOREIGN KEY([GLAccountId])
REFERENCES [dbo].[GLAccounts] ([Id]);

ALTER TABLE [dbo].[CashLedgers] WITH CHECK ADD CONSTRAINT [FK_CashLedgers_JournalEntries] FOREIGN KEY([JournalEntryId])
REFERENCES [dbo].[JournalEntries] ([Id]);

ALTER TABLE [dbo].[StockAdjustments] WITH CHECK ADD CONSTRAINT [FK_StockAdjustments_IngredientItems] FOREIGN KEY([IngredientItemId])
REFERENCES [dbo].[IngredientItems] ([Id]);

ALTER TABLE [dbo].[StockAdjustments] WITH CHECK ADD CONSTRAINT [FK_StockAdjustments_JournalEntries] FOREIGN KEY([JournalEntryId])
REFERENCES [dbo].[JournalEntries] ([Id]);

ALTER TABLE [dbo].[Partners] WITH CHECK ADD CONSTRAINT [FK_Partners_GLAccounts] FOREIGN KEY([GLAccountId])
REFERENCES [dbo].[GLAccounts] ([Id]);

ALTER TABLE [dbo].[PartnerTransactions] WITH CHECK ADD CONSTRAINT [FK_PartnerTransactions_Partners] FOREIGN KEY([PartnerId])
REFERENCES [dbo].[Partners] ([Id]) ON DELETE CASCADE;

ALTER TABLE [dbo].[PartnerTransactions] WITH CHECK ADD CONSTRAINT [FK_PartnerTransactions_JournalEntries] FOREIGN KEY([JournalEntryId])
REFERENCES [dbo].[JournalEntries] ([Id]);
