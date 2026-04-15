USE [db43829-12-04-2026-aftercleanup];
GO

-- 1. FOC Tracking
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'OrderDetails' AND COLUMN_NAME = 'FocQuantity')
    ALTER TABLE OrderDetails ADD FocQuantity DECIMAL(18, 2) NOT NULL DEFAULT 0;

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'RunningOrderDetails' AND COLUMN_NAME = 'FocQuantity')
    ALTER TABLE RunningOrderDetails ADD FocQuantity DECIMAL(18, 2) NOT NULL DEFAULT 0;

-- 2. FoodItemStocks (Van Inventory)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[FoodItemStocks]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[FoodItemStocks] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [FoodItemId] INT NOT NULL,
        [UserId] NVARCHAR(150) NOT NULL,
        [Quantity] DECIMAL(18, 2) NOT NULL DEFAULT 0,
        [UpdatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [FK_FoodItemStocks_FoodItems] FOREIGN KEY ([FoodItemId]) REFERENCES [FoodItems]([Id])
    );
END

-- 3. InventoryTransactions
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[InventoryTransactions]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[InventoryTransactions] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [FoodItemId] INT NOT NULL,
        [UserId] NVARCHAR(150) NOT NULL,
        [QuantityChange] DECIMAL(18, 2) NOT NULL,
        [Type] NVARCHAR(100) NOT NULL,
        [EntryDate] DATETIME NOT NULL DEFAULT GETDATE(),
        [ReferenceId] INT NULL,
        [CreatedBy] NVARCHAR(150) NULL,
        CONSTRAINT [FK_InventoryTransactions_FoodItems] FOREIGN KEY ([FoodItemId]) REFERENCES [FoodItems]([Id])
    );
END

-- 4. GL Account
IF NOT EXISTS (SELECT * FROM GLAccounts WHERE AccountName = 'Van Sale Expenses')
BEGIN
    INSERT INTO GLAccounts (AccountCode, AccountName, Description, Category, Type, SubType, IsCash, IsBank, CurrentBalance, IsActive) 
    VALUES ('5010', 'Van Sale Expenses', 'Vansale daily expenses like fuel, maintenance etc.', 4, 18, 0, 0, 0, 0, 1);
END
GO
