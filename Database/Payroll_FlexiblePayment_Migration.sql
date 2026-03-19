-- Migration Script for Flexible Payroll Payment System
-- Add new columns to Payroll table and create PayrollPayments table

-- 1. Add new columns to Payroll table
ALTER TABLE Payrolls ADD 
    AdvanceAmountPaid DECIMAL(10, 2) DEFAULT 0 NOT NULL,
    TotalAmountPaid DECIMAL(10, 2) DEFAULT 0 NOT NULL,
    RemainingBalance DECIMAL(10, 2) DEFAULT 0 NOT NULL,
    JournalEntryId INT NULL;

-- 2. Add foreign key constraint for JournalEntryId
ALTER TABLE Payrolls ADD CONSTRAINT FK_Payroll_JournalEntry 
    FOREIGN KEY (JournalEntryId) REFERENCES JournalEntries(Id);

-- 3. Create PayrollPayments table
CREATE TABLE PayrollPayments (
    Id INT PRIMARY KEY IDENTITY(1,1),
    PayrollId INT NOT NULL,
    Amount DECIMAL(10, 2) NOT NULL,
    PaymentMethod NVARCHAR(50) NULL,
    PaymentDate DATETIME NOT NULL,
    Notes NVARCHAR(MAX) NULL,
    JournalEntryId INT NULL,
    CreatedAt DATETIME NOT NULL,
    CONSTRAINT FK_PayrollPayment_Payroll FOREIGN KEY (PayrollId) REFERENCES Payrolls(Id),
    CONSTRAINT FK_PayrollPayment_JournalEntry FOREIGN KEY (JournalEntryId) REFERENCES JournalEntries(Id)
);

-- 4. Add indexes for better query performance
CREATE INDEX IDX_PayrollPayments_PayrollId ON PayrollPayments(PayrollId);
CREATE INDEX IDX_PayrollPayments_PaymentDate ON PayrollPayments(PaymentDate);
CREATE INDEX IDX_Payroll_JournalEntryId ON Payrolls(JournalEntryId);

-- 5. Initialize RemainingBalance for existing records
UPDATE Payrolls 
SET RemainingBalance = NetSalary 
WHERE RemainingBalance = 0 AND PaymentStatus IN ('Unpaid', 'PartiallyPaid');

UPDATE Payrolls 
SET RemainingBalance = 0 
WHERE PaymentStatus = 'Paid';

-- Verify the changes
SELECT 'Payroll columns updated' as Status;
SELECT COUNT(*) as PayrollPaymentsTableCreated FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'PayrollPayments';
