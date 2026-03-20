-- Reset Balances and Quantities for Fresh Start
-- Preserves master records but zeros out accumulated values

BEGIN TRANSACTION;

BEGIN TRY
    -- Reset Ingredient Quantities
    PRINT 'Resetting IngredientItem quantities...';
    UPDATE [IngredientItems] SET [Quantity] = 0;

    -- Reset Account Balances
    PRINT 'Resetting GLAccount balances...';
    UPDATE [GLAccounts] SET [CurrentBalance] = 0;

    PRINT 'Balances and quantities reset successfully.';

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT 'Error occurred during reset:';
    PRINT ERROR_MESSAGE();
END CATCH
