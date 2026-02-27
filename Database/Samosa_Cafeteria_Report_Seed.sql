-- Samosa Cafeteria Report Generator Seed Script
-- Standard Accounting Software Setup Flow
-- ----------------------------------------------------------------------
-- PHASE 1: MASTER DATA MIGRATION (Chart of Accounts, Employees, Vendors)
-- PHASE 2: SYSTEM MAPPING (Mapping Ledgers to Application Settings)
-- PHASE 3: OPENING BALANCES (Importing Historical General Journal & Subledgers)
-- ----------------------------------------------------------------------

BEGIN TRANSACTION;

-- ==============================================================================
-- PHASE 0: ACCOUNTING ENGINE SCHEMA SETUP
-- ==============================================================================
IF NOT EXISTS(SELECT 1 FROM sys.columns WHERE Name = N'FinancialYearStart' AND Object_ID = Object_ID(N'dbo.AppSettings'))
BEGIN
    ALTER TABLE [dbo].[AppSettings] ADD [FinancialYearStart] DATETIME NULL;
END

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[GLAccounts]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[GLAccounts] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [AccountCode] NVARCHAR(50) NOT NULL,
    [AccountName] NVARCHAR(255) NOT NULL,
    [Description] NVARCHAR(MAX) NULL,
    [Category] INT NOT NULL,  
    [Type] INT NOT NULL,      
    [CurrentBalance] DECIMAL(18, 2) NOT NULL DEFAULT 0.00,
    [IsActive] BIT NOT NULL DEFAULT 1
);
END

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[JournalEntries]') AND type in (N'U'))
BEGIN
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
END

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[LedgerEntries]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[LedgerEntries] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [JournalEntryId] INT NOT NULL FOREIGN KEY REFERENCES [dbo].[JournalEntries]([Id]) ON DELETE CASCADE,
    [GLAccountId] INT NOT NULL FOREIGN KEY REFERENCES [dbo].[GLAccounts]([Id]),
    [Description] NVARCHAR(MAX) NULL,
    [Debit] DECIMAL(18, 2) NOT NULL DEFAULT 0.00,
    [Credit] DECIMAL(18, 2) NOT NULL DEFAULT 0.00
);
END

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Invoices]') AND type in (N'U'))
BEGIN
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
END

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Bills]') AND type in (N'U'))
BEGIN
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
END
-- Clean existing seeded GL entries to be idempotent
DELETE FROM JournalEntries WHERE ReferenceNumber = 'OB-2025-12-31';

DECLARE @TargetDate DATETIME = '2025-12-31';

-- ==============================================================================
-- PHASE 1: MASTER DATA MIGRATION
-- ==============================================================================

-- >> A. Hydrate Chart of Accounts
-- The Trial Balance strictly maps to the Chart of Accounts. We create the entities first.
DECLARE @SeedData TABLE (AccountGroup NVARCHAR(100), AccountName NVARCHAR(100), AccountType NVARCHAR(50), Debit DECIMAL(18,2), Credit DECIMAL(18,2));

INSERT INTO @SeedData VALUES
('Capital', 'CAPITAL INVESTMENT', 'Equity', 235413.00, 0.00),
('Partners Current Account', 'D -NIZAR PONICHERI', 'Equity', 0.00, 26000.00),
('Partners Current Account', 'D-ASHRAF', 'Equity', 0.00, 25000.00),
('Partners Current Account', 'D-CV MOHAMED ALI', 'Equity', 0.00, 95713.00),
('Partners Current Account', 'D-HAMED', 'Equity', 0.00, 80000.00),
('Partners Current Account', 'D-RAHMAN', 'Equity', 0.00, 15000.00),
('Partners Current Account', 'D-SHIHAB PUTHAN PALLI', 'Equity', 0.00, 15000.00),
('Trade Payables / Vendors', 'AUTOZONE TYRES & SERVICES', 'Liability', 0.00, 160.00),
('Trade Payables / Vendors', 'GALAXY PRINTING PRESS W.L.L', 'Liability', 0.00, 2500.00),
('Trade Payables / Vendors', 'QUALITY PRINTING PRESS W.L.L', 'Liability', 0.00, 350.00),
('Salary Payables', 'ABDULLAH AL NOMAN', 'Liability', 0.00, 1000.00),
('Salary Payables', 'EAKRAMUL HOQUE ASHAN ULLAH', 'Liability', 0.00, 1616.67),
('Salary Payables', 'LAKSHMAN TAMLING', 'Liability', 1550.00, 0.00),
('Salary Payables', 'MD AKIBUL MIA', 'Liability', 28.00, 0.00),
('Salary Payables', 'MD NUR RASUL', 'Liability', 0.00, 1200.00),
('Salary Payables', 'MD SOUROV SHAH AKHILAS SHAH', 'Liability', 0.00, 1200.00),
('Salary Payables', 'MUHAMED SAHAL', 'Liability', 0.00, 400.00),
('Salary Payables', 'NIZAR PONICHERI', 'Liability', 0.00, 3245.00),
('Salary Payables', 'SHAJALAL RONI', 'Liability', 950.00, 0.00),
('Salary Payables', 'SHIHAB PUTHAN PALLI', 'Liability', 0.00, 1000.00),
('Salary Payables', 'YASIR', 'Liability', 33.33, 0.00),
('Trade Receivables / Customers', 'CAFE SALES', 'Asset', 0.00, 0.00),
('Trade Receivables / Customers', 'CARD SALES', 'Asset', 0.00, 0.00),
('Trade Receivables / Customers', 'VAN SALES', 'Asset', 0.00, 0.00),
('Trade Receivables / Customers', 'WHOLE SALES', 'Asset', 0.00, 0.00),
('Bank Account', 'CBQ', 'Asset', 500.00, 0.00),
('Bank Account', 'POS', 'Asset', 6462.00, 0.00),
('Cash In Hand', 'Cash', 'Asset', 11759.50, 0.00),
('Petty Cash', 'PETTY CASH', 'Asset', 2000.00, 0.00),
('Purchase', 'AJINOMOTO', 'Expense', 30.00, 0.00),
('Purchase', 'ATTA', 'Expense', 22.50, 0.00),
('Purchase', 'BANANA', 'Expense', 6.00, 0.00),
('Purchase', 'BEEF', 'Expense', 365.00, 0.00),
('Purchase', 'BEEF KEEMA', 'Expense', 40.00, 0.00),
('Purchase', 'BESAN FLOUR', 'Expense', 10.00, 0.00),
('Purchase', 'CABBAGE', 'Expense', 5.00, 0.00),
('Purchase', 'CARDAMOM', 'Expense', 29.00, 0.00),
('Purchase', 'CARROT', 'Expense', 80.00, 0.00),
('Purchase', 'CHANA', 'Expense', 22.50, 0.00),
('Purchase', 'CHEESE', 'Expense', 14554.00, 0.00),
('Purchase', 'CHICK PEAS', 'Expense', 38.00, 0.00),
('Purchase', 'CHICKEN', 'Expense', 289.00, 0.00),
('Purchase', 'CHICKEN BREAST', 'Expense', 455.00, 0.00),
('Purchase', 'CHICKEN MASALA', 'Expense', 108.00, 0.00),
('Purchase', 'CHILLY', 'Expense', 123.00, 0.00),
('Purchase', 'CHILLY PODER', 'Expense', 50.00, 0.00),
('Purchase', 'CONDENSED MILK', 'Expense', 626.00, 0.00),
('Purchase', 'CONSUMABLE EXPENSES', 'Expense', 2119.00, 0.00),
('Purchase', 'CORIANDER LEAVES', 'Expense', 82.00, 0.00),
('Purchase', 'DAL', 'Expense', 35.00, 0.00),
('Purchase', 'EGG', 'Expense', 92.00, 0.00),
('Purchase', 'FRUITS', 'Expense', 132.00, 0.00),
('Purchase', 'GARLIC', 'Expense', 55.00, 0.00),
('Purchase', 'GHEE', 'Expense', 104.00, 0.00),
('Purchase', 'GINGER', 'Expense', 103.00, 0.00),
('Purchase', 'GREEN PEAS', 'Expense', 178.00, 0.00),
('Purchase', 'JEERAKAM', 'Expense', 206.50, 0.00),
('Purchase', 'KARI LEAVES', 'Expense', 93.00, 0.00),
('Purchase', 'KASS -veg', 'Expense', 13.00, 0.00),
('Purchase', 'KAYAM POWDER', 'Expense', 7.00, 0.00),
('Purchase', 'KOOSA-VEG', 'Expense', 40.00, 0.00),
('Purchase', 'KUBOOS', 'Expense', 33.00, 0.00),
('Purchase', 'MAIDA -POROTTA', 'Expense', 10100.00, 0.00),
('Purchase', 'MAIDA -PREMIUM', 'Expense', 2382.00, 0.00),
('Purchase', 'MASALA', 'Expense', 86.00, 0.00),
('Purchase', 'MAYONNAISE', 'Expense', 20.00, 0.00),
('Purchase', 'MILK AND MILK PRODUCTS', 'Expense', 18.00, 0.00),
('Purchase', 'MINERAL WATER', 'Expense', 131.00, 0.00),
('Purchase', 'MINT', 'Expense', 7.00, 0.00),
('Purchase', 'NUTS AND CASHEWS', 'Expense', 14.00, 0.00),
('Purchase', 'ONION', 'Expense', 854.00, 0.00),
('Purchase', 'PALM OIL', 'Expense', 1554.00, 0.00),
('Purchase', 'PARSLEY', 'Expense', 8.00, 0.00),
('Purchase', 'POTATO', 'Expense', 3026.00, 0.00),
('Purchase', 'SALT', 'Expense', 60.00, 0.00),
('Purchase', 'SAMBAR POWDER', 'Expense', 15.50, 0.00),
('Purchase', 'SAUCE', 'Expense', 3.50, 0.00),
('Purchase', 'SODA POWDER', 'Expense', 7.00, 0.00),
('Purchase', 'SPICES(PATTA)', 'Expense', 16.00, 0.00),
('Purchase', 'SUGAR', 'Expense', 43.00, 0.00),
('Purchase', 'SWEET CORN', 'Expense', 70.00, 0.00),
('Purchase', 'TAMARIND', 'Expense', 6.00, 0.00),
('Purchase', 'TEA POWDER', 'Expense', 218.00, 0.00),
('Purchase', 'TOMATO', 'Expense', 95.00, 0.00),
('Purchase', 'TURMERIC POWDER', 'Expense', 31.00, 0.00),
('Purchase', 'VEGETABLE', 'Expense', 2.00, 0.00),
('Direct Expenses', 'CLEANING ITEMS', 'Expense', 280.00, 0.00),
('Direct Expenses', 'FOOD MESS EXPENSE', 'Expense', 1237.00, 0.00),
('Direct Expenses', 'GAS', 'Expense', 1155.00, 0.00),
('Direct Expenses', 'MISC EXPENDITURE', 'Expense', 1500.00, 0.00),
('Direct Expenses', 'PACKING AND DIPO MATERIALS', 'Expense', 1335.00, 0.00),
('Direct Expenses', 'SALARY EXPENSES', 'Expense', 3783.00, 0.00),
('Direct Expenses', 'TRANSPORTATION EXPENSE', 'Expense', 38.00, 0.00),
('Direct Expenses', 'TRASH BAG', 'Expense', 47.00, 0.00),
('Direct Expenses', 'VEHICLE FUEL', 'Expense', 2754.00, 0.00),
('Sales', 'Sales Account', 'Revenue', 0.00, 92609.50),
('Staff Welfare', 'SALARY', 'Expense', 14033.34, 0.00),
('Remuneration', 'STAFF MEDICAL EXPENSES', 'Expense', 20.00, 0.00),
('Administration Expense', 'Bank Charges', 'Expense', 500.00, 0.00),
('Administration Expense', 'COMPANY DOCUMENTS', 'Expense', 1100.00, 0.00),
('Administration Expense', 'Electricity And Water Charges', 'Expense', 750.00, 0.00),
('Administration Expense', 'PRINTING AND STATIONARY', 'Expense', 2937.50, 0.00),
('Administration Expense', 'RENT', 'Expense', 20200.00, 0.00),
('Administration Expense', 'TELEPHONE EXPENSE', 'Expense', 10.00, 0.00),
('Administration Expense', 'VISA AND MEDICAL EXPENSE', 'Expense', 857.00, 0.00),
('Selling And Distribution', 'Sales Commission', 'Expense', 2700.00, 0.00),
('Repairs And Maintenance', 'Repair & Maintenance', 'Expense', 8089.00, 0.00),
('Vehicle Repair & Maintenance', 'Vehicle Repair & Maintenance', 'Expense', 1060.00, 0.00);

-- Hydrate Accounts Master Definitions
DECLARE @Grp NVARCHAR(100), @Name NVARCHAR(100), @Type NVARCHAR(50), @Deb DECIMAL(18,2), @Cred DECIMAL(18,2);
DECLARE AccountsCursor CURSOR FOR SELECT AccountGroup, AccountName, AccountType, Debit, Credit FROM @SeedData;
OPEN AccountsCursor;
FETCH NEXT FROM AccountsCursor INTO @Grp, @Name, @Type, @Deb, @Cred;

WHILE @@FETCH_STATUS = 0
BEGIN

    -- Hydrate the new Double-Entry GLAccounts based on mapped Types
    IF NOT EXISTS(SELECT 1 FROM GLAccounts WHERE AccountName = @Name)
    BEGIN
        DECLARE @Cat INT = CASE @Type WHEN 'Asset' THEN 0 WHEN 'Liability' THEN 1 WHEN 'Equity' THEN 2 WHEN 'Revenue' THEN 3 WHEN 'Expense' THEN 4 ELSE 0 END;
        DECLARE @AccType INT = CASE @Grp
            WHEN 'Bank Account' THEN 0 WHEN 'Cash In Hand' THEN 0 WHEN 'Petty Cash' THEN 0 WHEN 'Trade Receivables / Customers' THEN 1
            WHEN 'Trade Payables / Vendors' THEN 4 WHEN 'Salary Payables' THEN 6 WHEN 'Capital' THEN 8 WHEN 'Partners Current Account' THEN 8
            WHEN 'Sales' THEN 10 WHEN 'Purchase' THEN 13 WHEN 'Direct Expenses' THEN 18 WHEN 'Staff Welfare' THEN 15
            WHEN 'Remuneration' THEN 15 WHEN 'Administration Expense' THEN 18 WHEN 'Selling And Distribution' THEN 17
            WHEN 'Repairs And Maintenance' THEN 18 WHEN 'Vehicle Repair & Maintenance' THEN 18 ELSE 0 END;
        
        DECLARE @RndCode NVARCHAR(50) = CAST(ABS(CHECKSUM(NEWID())) % 100000 AS NVARCHAR(50));
        INSERT INTO GLAccounts (AccountCode, AccountName, Description, Category, Type, CurrentBalance, IsActive)
        VALUES (@RndCode, @Name, @Grp, @Cat, @AccType, ABS(@Deb - @Cred), 1);
    END
    FETCH NEXT FROM AccountsCursor INTO @Grp, @Name, @Type, @Deb, @Cred;
END
CLOSE AccountsCursor;
DEALLOCATE AccountsCursor;


-- >> B. Hydrate Master Employees (HRM Subledger Setup)
DECLARE @DeptId INT; SELECT TOP 1 @DeptId = Id FROM Departments; IF @DeptId IS NULL BEGIN INSERT INTO Departments (Title, UpdatedBy, UpdatedAt) VALUES ('General', 'admin', GETDATE()); SET @DeptId = SCOPE_IDENTITY(); END
DECLARE @DesigId INT; SELECT TOP 1 @DesigId = Id FROM Designations; IF @DesigId IS NULL BEGIN INSERT INTO Designations (Title, UpdatedBy, UpdatedAt) VALUES ('Employee', 'admin', GETDATE()); SET @DesigId = SCOPE_IDENTITY(); END
DECLARE @ShiftId INT; SELECT TOP 1 @ShiftId = Id FROM Shifts; IF @ShiftId IS NULL BEGIN INSERT INTO Shifts (Title, StartAt, EndAt, UpdatedBy, UpdatedAt) VALUES ('Regular', '09:00:00', '17:00:00', 'admin', GETDATE()); SET @ShiftId = SCOPE_IDENTITY(); END

DECLARE @EmpCursor CURSOR;
SET @EmpCursor = CURSOR FOR SELECT AccountName, Debit, Credit FROM @SeedData WHERE AccountGroup = 'Salary Payables';

DECLARE @EmpName NVARCHAR(100), @EmpDeb DECIMAL(18,2), @EmpCred DECIMAL(18,2);
OPEN @EmpCursor;
FETCH NEXT FROM @EmpCursor INTO @EmpName, @EmpDeb, @EmpCred;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF NOT EXISTS(SELECT 1 FROM Employees WHERE Name = @EmpName)
    BEGIN
        INSERT INTO Employees (DepartmentId, DesignationId, ShiftId, Name, Email, Phone, EmergencyContact, PresentAddress, PermanentAddress, Gender, Dob, Religion, MaritalStatus, Nidnumber, JoiningDate, PayslipType, Salary, Status, UpdatedBy, UpdatedAt)
        VALUES (@DeptId, @DesigId, @ShiftId, @EmpName, REPLACE(LOWER(@EmpName), ' ', '') + '@example.com', '-', '-', '-', '-', '-', '2025-01-01', '-', '-', '-', '2025-01-01', 'Monthly', ABS(@EmpDeb - @EmpCred), 1, 'admin', GETDATE());
    END
    FETCH NEXT FROM @EmpCursor INTO @EmpName, @EmpDeb, @EmpCred;
END
CLOSE @EmpCursor;
DEALLOCATE @EmpCursor;

-- ==============================================================================
-- PHASE 2: SYSTEM MAPPING AND CONTROL ACCOUNTS
-- ==============================================================================

DECLARE @SalesAcctId INT; SELECT TOP 1 @SalesAcctId = Id FROM GLAccounts WHERE AccountName = 'Sales Account';
DECLARE @PurchaseAcctId INT; SELECT TOP 1 @PurchaseAcctId = Id FROM GLAccounts WHERE Description = 'Purchase';
DECLARE @PayrollAcctId INT; SELECT TOP 1 @PayrollAcctId = Id FROM GLAccounts WHERE AccountName = 'SALARY EXPENSES' OR AccountName = 'SALARY';

IF EXISTS (SELECT 1 FROM AppSettings)
BEGIN
    UPDATE AppSettings
    SET SaleAccount = ISNULL(@SalesAcctId, SaleAccount),
        PurchaseAccount = ISNULL(@PurchaseAcctId, PurchaseAccount),
        PayrollAccount = ISNULL(@PayrollAcctId, PayrollAccount)
    WHERE Id = 1;
END

-- ==============================================================================
-- PHASE 3: OPENING BALANCES IMPORT (Transactions & Financial Logging)
-- ==============================================================================

-- Double-Entry Initialization: Create a central Journal Entry for Opening Balances
DECLARE @JEntryId INT;
IF NOT EXISTS(SELECT 1 FROM JournalEntries WHERE ReferenceNumber = 'OB-2025-12-31')
BEGIN
    INSERT INTO JournalEntries (ReferenceNumber, Description, EntryDate, IsPosted, SourceDocumentType, SourceDocumentId, CreatedAt)
    VALUES ('OB-2025-12-31', 'Historical Opening Balances', @TargetDate, 1, 'historical', NULL, GETDATE());
    SET @JEntryId = SCOPE_IDENTITY();
END
ELSE
BEGIN
    SELECT TOP 1 @JEntryId = Id FROM JournalEntries WHERE ReferenceNumber = 'OB-2025-12-31';
END

DECLARE DataCursor CURSOR FOR SELECT AccountGroup, AccountName, AccountType, Debit, Credit FROM @SeedData;
OPEN DataCursor;
FETCH NEXT FROM DataCursor INTO @Grp, @Name, @Type, @Deb, @Cred;

WHILE @@FETCH_STATUS = 0
BEGIN
    -- 1. General Ledger Journals
    IF @Deb > 0 OR @Cred > 0
    BEGIN
        -- Insert into new Double-Entry Subledgers
        DECLARE @GLAccId INT;
        SELECT TOP 1 @GLAccId = Id FROM GLAccounts WHERE AccountName = @Name;
        IF @GLAccId IS NOT NULL AND NOT EXISTS(SELECT 1 FROM LedgerEntries WHERE JournalEntryId = @JEntryId AND GLAccountId = @GLAccId)
        BEGIN
            INSERT INTO LedgerEntries (JournalEntryId, GLAccountId, Description, Debit, Credit)
            VALUES (@JEntryId, @GLAccId, 'Opening Balance via Trial Balance', @Deb, @Cred);
        END
    END

    -- 2. Open Subledgers: Missing Payroll Slip creation
    IF @Grp = 'Salary Payables' AND (@Deb > 0 OR @Cred > 0)
    BEGIN
        DECLARE @ActualSalary DECIMAL(18,2) = ABS(@Deb - @Cred);
        DECLARE @EmployeeId INT; SELECT TOP 1 @EmployeeId = Id FROM Employees WHERE Name = @Name;
        
        IF @EmployeeId IS NOT NULL
        BEGIN
            INSERT INTO Payrolls (EmployeeId, PayrollType, Salary, NetSalary, Month, Year, PaymentStatus, GeneratedBy, GeneratedAt)
            VALUES (@EmployeeId, 'Monthly', @ActualSalary, @ActualSalary, 12, 2025, CASE WHEN @Deb > 0 THEN 'Paid' ELSE 'Unpaid' END, 'admin', GETDATE());
        END
    END

    -- 4. Open Subledgers: Purchases
    IF @Grp = 'Purchase' AND @Deb > 0
    BEGIN
        DECLARE @SupplierId INT = NULL; SELECT TOP 1 @SupplierId = Id FROM Suppliers;
        IF @SupplierId IS NULL BEGIN INSERT INTO Suppliers (SupplierName, Email, Phone, Address, UpdatedBy, UpdatedAt) VALUES ('Default Vendor', '-', '-', '-', 'admin', GETDATE()); SET @SupplierId = SCOPE_IDENTITY(); END

        DECLARE @IngredientId INT = NULL;
        SELECT TOP 1 @IngredientId = Id FROM IngredientItems WHERE ItemName = @Name;
        IF @IngredientId IS NULL BEGIN INSERT INTO IngredientItems (ItemName, Description, Unit, Price, Quantity, AlertQuantity, UpdatedBy, UpdatedAt) VALUES (@Name, '-', '-', @Deb, 1, 0, 'admin', GETDATE()); SET @IngredientId = SCOPE_IDENTITY(); END

        INSERT INTO Purchases (SupplierId, InvoiceNo, PurchaseDate, Description, PaymentType, TotalAmount, PaidAmount, DueAmount, UpdatedBy, UpdatedAt)
        VALUES (@SupplierId, 'HIST-123', @TargetDate, 'Historical Transfer', 'cash', @Deb, @Deb, 0, 'admin', GETDATE());
        DECLARE @PurchaseId INT = SCOPE_IDENTITY();

        INSERT INTO PurchaseDetails (PurchaseId, IngredientItemId, PurchasePrice, Quantity, Total, CreatedAt)
        VALUES (@PurchaseId, @IngredientId, @Deb, 1, @Deb, GETDATE());
    END

    -- 5. Open Subledgers: Sales Orders
    IF @Grp = 'Sales' AND @Cred > 0
    BEGIN
        -- Query Saffrat AppSettings explicitly for user-configured defaults
        DECLARE @CustId INT = NULL; SELECT TOP 1 @CustId = DefaultCustomer FROM AppSettings;
        DECLARE @OrderTypeId INT = NULL; SELECT TOP 1 @OrderTypeId = DefaultOrderType FROM AppSettings;
        
        IF @CustId IS NULL OR @CustId = 0 BEGIN SELECT TOP 1 @CustId = Id FROM Customers; END
        IF @CustId IS NULL OR @CustId = 0 BEGIN INSERT INTO Customers (CustomerName, Email, Phone, Address, UpdatedBy, UpdatedAt) VALUES ('Walk-in', '-', '-', '-', 'admin', GETDATE()); SET @CustId = SCOPE_IDENTITY(); END

        DECLARE @FoodGrpId INT = NULL; SELECT TOP 1 @FoodGrpId = Id FROM FoodGroups; 
        IF @FoodGrpId IS NULL BEGIN INSERT INTO FoodGroups (GroupName, Status, UpdatedBy, UpdatedAt) VALUES ('General Menu', 1, 'admin', GETDATE()); SET @FoodGrpId = SCOPE_IDENTITY(); END
        
        DECLARE @FoodItemId INT = NULL;
        SELECT TOP 1 @FoodItemId = Id FROM FoodItems WHERE ItemName = @Name;
        IF @FoodItemId IS NULL BEGIN INSERT INTO FoodItems (GroupId, ItemName, Description, Price, VanSalePrice, WholeSalePrice, UpdatedBy, UpdatedAt) VALUES (@FoodGrpId, @Name, '-', @Cred, @Cred, @Cred, 'admin', GETDATE()); SET @FoodItemId = SCOPE_IDENTITY(); END

        DECLARE @OrderId INT = NULL; SELECT @OrderId = ISNULL(MAX(Id), 0) + 1 FROM Orders;
        INSERT INTO Orders (Id, CustomerId, WaiterOrDriver, SubTotal, TaxTotal, DiscountTotal, ChargeTotal, Total, PaymentMethod, PaidAmount, DueAmount, OrderType, Status, CreatedBy, CreatedAt)
        VALUES (@OrderId, @CustId, 'System', @Cred, 0, 0, 0, @Cred, 'Cash', @Cred, 0, ISNULL(@OrderTypeId, 1), 3, 'admin', @TargetDate);

        INSERT INTO OrderDetails (OrderId, ItemId, Price, ModifierTotal, Quantity, Total, CreatedAt)
        VALUES (@OrderId, @FoodItemId, @Cred, 0, 1, @Cred, GETDATE());
    END

    FETCH NEXT FROM DataCursor INTO @Grp, @Name, @Type, @Deb, @Cred;
END

CLOSE DataCursor;
DEALLOCATE DataCursor;

COMMIT TRANSACTION;
GO
