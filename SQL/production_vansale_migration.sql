/* 
 * SQL Migration Script for Advanced VanSale & POS Enhancements
 * Date: 2026-04-12
 * Description: 
 *   - Adds FOC tracking to orders. 
 *   - Creates inventory tracking for van sales (FoodItemStocks & InventoryTransactions).
 *   - Creates Van Sale Expense GL account.
 */

USE [db43829-12-04-2026-aftercleanup]; -- Update to your production DB name
GO

-- 1. Support FOC (Free of Charge) tracking in finalized and running orders
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'OrderDetails' AND COLUMN_NAME = 'FocQuantity')
BEGIN
    ALTER TABLE OrderDetails ADD FocQuantity DECIMAL(18, 2) NOT NULL DEFAULT 0;
END
GO

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'RunningOrderDetails' AND COLUMN_NAME = 'FocQuantity')
BEGIN
    ALTER TABLE RunningOrderDetails ADD FocQuantity DECIMAL(18, 2) NOT NULL DEFAULT 0;
END
GO

-- 2. Create Stock Tracking for VanSale Drivers
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[FoodItemStocks]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[FoodItemStocks] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UserId] NVARCHAR(450) NOT NULL,
        [FoodItemId] INT NOT NULL,
        [StockQuantity] DECIMAL(18, 2) NOT NULL DEFAULT 0,
        [LastUpdated] DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [FK_FoodItemStocks_Users] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers]([Id]),
        CONSTRAINT [FK_FoodItemStocks_FoodItems] FOREIGN KEY ([FoodItemId]) REFERENCES [FoodItems]([Id])
    );
END
GO

-- 3. Create Inventory Transaction Log for Audit Trail
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[InventoryTransactions]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[InventoryTransactions] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UserId] NVARCHAR(450) NOT NULL,
        [FoodItemId] INT NOT NULL,
        [Quantity] DECIMAL(18, 2) NOT NULL,
        [TransactionType] NVARCHAR(50) NOT NULL, -- 'Load' (from Store to Van), 'Return' (from Van to Store), 'Sale' (Deducted on Sale)
        [ReferenceId] NVARCHAR(100) NULL,      -- Could be OrderId or a unique Load ID
        [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [FK_InventoryTransactions_Users] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers]([Id]),
        CONSTRAINT [FK_InventoryTransactions_FoodItems] FOREIGN KEY ([FoodItemId]) REFERENCES [FoodItems]([Id])
    );
END
GO

-- 4. Create Van Sale Expenses GL Account (General Ledger)
IF NOT EXISTS (SELECT * FROM GLAccounts WHERE AccountName = 'Van Sale Expenses')
BEGIN
    INSERT INTO GLAccounts (AccountCode, AccountName, Description, Category, Type, SubType, IsCash, IsBank, CurrentBalance, IsActive) 
    VALUES ('5010', 'Van Sale Expenses', 'Vansale daily expenses like fuel, maintenance etc.', 4, 18, 0, 0, 0, 0, 1);
END
GO
