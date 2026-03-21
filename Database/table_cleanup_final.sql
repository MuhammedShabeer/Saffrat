-- Database Table Cleanup Script
-- DROPPING UNREFERENCED/LEGACY TABLES AS PER APPROVED PLAN

BEGIN TRANSACTION;

BEGIN TRY
    -- Drop tables if they exist
    IF OBJECT_ID('[dbo].[CashLedgers]', 'U') IS NOT NULL DROP TABLE [dbo].[CashLedgers];
    IF OBJECT_ID('[dbo].[AccountMoneyTransfers]', 'U') IS NOT NULL DROP TABLE [dbo].[AccountMoneyTransfers];
    IF OBJECT_ID('[dbo].[Accounts]', 'U') IS NOT NULL DROP TABLE [dbo].[Accounts];
    IF OBJECT_ID('[dbo].[Deposits]', 'U') IS NOT NULL DROP TABLE [dbo].[Deposits];
    IF OBJECT_ID('[dbo].[Expenses]', 'U') IS NOT NULL DROP TABLE [dbo].[Expenses];
    IF OBJECT_ID('[dbo].[Transactions]', 'U') IS NOT NULL DROP TABLE [dbo].[Transactions];
    IF OBJECT_ID('[dbo].[Tenants]', 'U') IS NOT NULL DROP TABLE [dbo].[Tenants];

    PRINT 'Selected legacy tables dropped successfully.';
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT 'Error occurred during table cleanup:';
    PRINT ERROR_MESSAGE();
END CATCH
