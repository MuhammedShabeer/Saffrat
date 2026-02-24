using System;
using System.Collections.Generic;

namespace Saffrat.Models.AccountingEngine
{
    // These enums support the Accounting Engine and map to the scaffolded INT columns in the DB.

    public enum AccountCategory
    {
        Asset = 0,
        Liability = 1,
        Equity = 2,
        Revenue = 3,
        Expense = 4
    }

    public enum AccountType
    {
        // Assets
        CashAndBank = 0, AccountsReceivable = 1, Inventory = 2, FixedAssets = 3,
        // Liabilities
        AccountsPayable = 4, CreditCard = 5, OtherCurrentLiability = 6, LongTermLiability = 7,
        // Equity
        OwnerEquity = 8, RetainedEarnings = 9,
        // Revenue
        Sales = 10, OtherIncome = 11,
        // Expenses
        CostOfGoodsSold = 12, FoodCost = 13, BeverageCost = 14, Labor = 15, Rent = 16, Marketing = 17, GeneralAdministrative = 18, Depreciation = 19
    }
}

namespace Saffrat.Models
{
    // TEMPORARY STUBS TO FIX BUILD BEFORE SCAFFOLDING 
    // Wait for the user to run Scaffold-DbContext which will overwrite these or build on top of them.
    public partial class GLAccount
    {
        public int Id { get; set; }
        public string AccountCode { get; set; }
        public string AccountName { get; set; }
        public string Description { get; set; }
        public int Category { get; set; }
        public int Type { get; set; }
        public decimal CurrentBalance { get; set; }
        public bool IsActive { get; set; }
    }
    public partial class JournalEntry
    {
        public int Id { get; set; }
        public string ReferenceNumber { get; set; }
        public string Description { get; set; }
        public DateTime EntryDate { get; set; }
        public bool IsPosted { get; set; }
        public string SourceDocumentType { get; set; }
        public int? SourceDocumentId { get; set; }
        public DateTime CreatedAt { get; set; }
        public virtual ICollection<LedgerEntry> LedgerEntries { get; set; } = new List<LedgerEntry>();
    }
    public partial class LedgerEntry
    {
        public int Id { get; set; }
        public int JournalEntryId { get; set; }
        public int GLAccountId { get; set; }
        public string Description { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public virtual GLAccount GLAccount { get; set; }
        public virtual JournalEntry JournalEntry { get; set; }
    }
    public partial class Invoice
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public string InvoiceNumber { get; set; }
        public DateTime IssueDate { get; set; }
        public DateTime DueDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AmountPaid { get; set; }
        public string Status { get; set; }
        public int? JournalEntryId { get; set; }
    }
    public partial class Bill
    {
        public int Id { get; set; }
        public int SupplierId { get; set; }
        public string BillNumber { get; set; }
        public DateTime Date { get; set; }
        public DateTime DueDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AmountPaid { get; set; }
        public string Status { get; set; }
        public int? JournalEntryId { get; set; }
    }
}
