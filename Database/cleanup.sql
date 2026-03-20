-- Optimized Database Cleanup Script for Fresh Start
-- Handles dependencies correctly

BEGIN TRANSACTION;

BEGIN TRY
    -- 1. Sales & POS (Leaf to Root)
    DELETE FROM [OrderItemModifiers];
    DELETE FROM [OrderDetails];
    DELETE FROM [Orders];
    DELETE FROM [RunningOrderItemModifiers];
    DELETE FROM [RunningOrderDetails];
    DELETE FROM [RunningOrders];
    DELETE FROM [WorkPeriods];

    -- 2. Payroll (Leaf to Root)
    DELETE FROM [PayrollPayments];
    DELETE FROM [PayrollDetails];
    DELETE FROM [Payrolls];

    -- 3. Purchases
    DELETE FROM [PurchaseDetails];
    DELETE FROM [Purchases];

    -- 4. Accounting Support
    DELETE FROM [LedgerEntries];
    DELETE FROM [Invoices];
    DELETE FROM [Bills];
    DELETE FROM [CashLedgers];
    DELETE FROM [PartnerTransactions];
    DELETE FROM [StockAdjustments];

    -- 5. Journal Entries (The core/root of most accounting)
    DELETE FROM [JournalEntries];

    -- 6. Misc Logs
    DELETE FROM [Attendances];
    DELETE FROM [LeaveRequests];
    DELETE FROM [AuditLogs];
    DELETE FROM [UserTokens];

    PRINT 'All transaction tables cleared successfully.';

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT 'Error occurred during cleanup:';
    PRINT ERROR_MESSAGE();
END CATCH
