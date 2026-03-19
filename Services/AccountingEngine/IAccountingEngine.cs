using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Saffrat.Models;
using Saffrat.Models.AccountingEngine;

namespace Saffrat.Services.AccountingEngine
{
    public interface IAccountingEngine
    {
        // 1. The "Daily Close" Logic
        /// <summary>
        /// Aggregates all daily transactions (POS orders, expenses, etc.) into a single "Daily Sales Summary" Journal Entry.
        /// </summary>
        Task<JournalEntry> PerformDailyCloseAsync(DateTime closeDate);

        // 2. Reporting Engine: Real-time Balance Sheet
        /// <summary>
        /// Generates a Real-time Balance Sheet up to a specific date.
        /// </summary>
        Task<BalanceSheetReport> GenerateBalanceSheetAsync(DateTime asOfDate);

        // 3. Reporting Engine: Profit & Loss Statement
        /// <summary>
        /// Generates a Profit & Loss Statement within a date range.
        /// </summary>
        Task<ProfitAndLossReport> GenerateProfitAndLossAsync(DateTime startDate, DateTime endDate);

        // 4. Ledger Interactions
        /// <summary>
        /// Posts a Journal Entry and its Ledger Entries ensuring Double-Entry validation (Debits == Credits).
        /// </summary>
        Task<bool> PostJournalEntryAsync(JournalEntry entry);

        // 5. AP/AR Interfaces
        /// <summary>
        /// Handles the translation of a new Invoice (AR) into a pending Journal Entry.
        /// </summary>
        Task<JournalEntry> DraftInvoiceJournalEntryAsync(Invoice invoice);

        /// <summary>
        /// Handles the translation of a new Bill (AP) into a pending Journal Entry.
        Task<JournalEntry> DraftBillJournalEntryAsync(Bill bill);

        /// <summary>
        /// Handles the translation of a payment receipt against an Invoice into a pending Journal Entry.
        /// </summary>
        Task<JournalEntry> RecordInvoicePaymentAsync(Invoice invoice);

        /// <summary>
        /// Handles the translation of a payment sent against a Bill into a pending Journal Entry.
        /// </summary>
        Task<JournalEntry> RecordBillPaymentAsync(Bill bill);

        /// <summary>
        /// Creates a draft payroll accrual journal entry when payroll is generated.
        /// </summary>
        Task<JournalEntry> DraftPayrollJournalEntryAsync(Payroll payroll);

        /// <summary>
        /// Records a flexible/partial payroll payment.
        /// </summary>
        Task<JournalEntry> RecordFlexiblePayrollPaymentAsync(PayrollPayment payment, Payroll payroll);
    }

    // Report DTOs
    public class BalanceSheetReport
    {
        public DateTime AsOfDate { get; set; }
        public Dictionary<AccountType, decimal> Assets { get; set; } = new Dictionary<AccountType, decimal>();
        public Dictionary<AccountType, decimal> Liabilities { get; set; } = new Dictionary<AccountType, decimal>();
        public Dictionary<AccountType, decimal> Equity { get; set; } = new Dictionary<AccountType, decimal>();
        public decimal TotalAssets { get; set; }
        public decimal TotalLiabilities { get; set; }
        public decimal TotalEquity { get; set; }
    }

    public class ProfitAndLossReport
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public Dictionary<string, decimal> Revenues { get; set; } = new Dictionary<string, decimal>();
        public Dictionary<string, decimal> Expenses { get; set; } = new Dictionary<string, decimal>();
        public decimal GrossProfit { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal NetIncome { get; set; }
    }
}
