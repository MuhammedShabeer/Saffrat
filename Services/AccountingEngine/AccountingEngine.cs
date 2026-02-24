using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Saffrat.Models;
using Saffrat.Models.AccountingEngine;

namespace Saffrat.Services.AccountingEngine
{
    public class DefaultAccountingEngine : IAccountingEngine
    {
        private readonly RestaurantDBContext _dbContext;

        public DefaultAccountingEngine(RestaurantDBContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// Aggregates all daily transactions into a single "Daily Sales Summary" journal entry.
        /// </summary>
        public async Task<JournalEntry> PerformDailyCloseAsync(DateTime closeDate)
        {
            var startOfDay = new DateTime(closeDate.Year, closeDate.Month, closeDate.Day, 0, 0, 0);
            var endOfDay = startOfDay.AddDays(1).AddTicks(-1);

            // Fetch raw operational data
            var dailyOrders = await _dbContext.Orders
                .Where(o => o.CreatedAt >= startOfDay && o.CreatedAt <= endOfDay)
                .ToListAsync();

            var totalSales = dailyOrders.Sum(o => o.SubTotal);
            var totalTaxes = dailyOrders.Sum(o => o.TaxTotal);
            var totalCashIn = dailyOrders.Sum(o => o.Total);

            decimal totalCOGS = dailyOrders.Sum(o => o.Total) * 0.30m; // Example 30% COGS

            // Create Master Journal
            var journalEntry = new JournalEntry
            {
                ReferenceNumber = $"DC-{closeDate:yyyyMMdd}",
                Description = $"Daily Close Summary for {closeDate:yyyy-MM-dd}",
                EntryDate = closeDate,
                SourceDocumentType = "DailyClose",
                IsPosted = false,
                LedgerEntries = new List<LedgerEntry>()
            };

            // In production, GLAccount IDs are dynamically queried based on Type mappings 
            // from the AppSettings or explicit GLAccount Type configurations.
            // Assuming dynamic mapping fallback here:
            int cashAccId = await GetOrCreateGLAccountAsync("Cash & Bank", AccountType.CashAndBank, AccountCategory.Asset);
            int salesAccId = await GetOrCreateGLAccountAsync("Food Sales", AccountType.Sales, AccountCategory.Revenue);
            int taxAccId = await GetOrCreateGLAccountAsync("Sales Tax", AccountType.OtherCurrentLiability, AccountCategory.Liability);
            int cogsAccId = await GetOrCreateGLAccountAsync("Cost of Goods Sold", AccountType.CostOfGoodsSold, AccountCategory.Expense);
            int invAccId = await GetOrCreateGLAccountAsync("Inventory", AccountType.Inventory, AccountCategory.Asset);

            // Ledger Line 1: Debit Cash (Asset)
            journalEntry.LedgerEntries.Add(new LedgerEntry { Description = "Daily Register Cash", Debit = totalCashIn, Credit = 0, GLAccountId = cashAccId });
            // Ledger Line 2: Credit Sales (Revenue)
            journalEntry.LedgerEntries.Add(new LedgerEntry { Description = "Daily Food Sales", Debit = 0, Credit = totalSales, GLAccountId = salesAccId });
            // Ledger Line 3: Credit Tax (Liability)
            journalEntry.LedgerEntries.Add(new LedgerEntry { Description = "Daily Tax Collected", Debit = 0, Credit = totalTaxes, GLAccountId = taxAccId });

            // Ledger Line 4: Debit COGS (Expense)
            journalEntry.LedgerEntries.Add(new LedgerEntry { Description = "Daily Food Cost", Debit = totalCOGS, Credit = 0, GLAccountId = cogsAccId });
            // Ledger Line 5: Credit Inventory (Asset)
            journalEntry.LedgerEntries.Add(new LedgerEntry { Description = "Inventory Depletion", Debit = 0, Credit = totalCOGS, GLAccountId = invAccId });

            // Post if valid
            await PostJournalEntryAsync(journalEntry);

            return journalEntry;
        }

        /// <summary>
        /// Generates a Real-time Balance Sheet using DbContext.Set for DbFirst queries
        /// </summary>
        public async Task<BalanceSheetReport> GenerateBalanceSheetAsync(DateTime asOfDate)
        {
            var report = new BalanceSheetReport { AsOfDate = asOfDate };

            var actualBalances = await _dbContext.Set<LedgerEntry>()
                .Include(l => l.GLAccount)
                .Where(l => l.JournalEntry.EntryDate <= asOfDate && l.JournalEntry.IsPosted)
                .GroupBy(l => new { l.GLAccount.Type, l.GLAccount.Category })
                .Select(g => new
                {
                    Type = g.Key.Type,
                    Category = g.Key.Category,
                    DebitSum = g.Sum(x => x.Debit),
                    CreditSum = g.Sum(x => x.Credit)
                })
                .ToListAsync();

            foreach (var bal in actualBalances)
            {
                if (bal.Category == (int)AccountCategory.Asset)
                {
                    decimal value = bal.DebitSum - bal.CreditSum; // Normal Debit
                    if (value != 0) report.Assets[(AccountType)bal.Type] = value;
                }
                else if (bal.Category == (int)AccountCategory.Liability)
                {
                    decimal value = bal.CreditSum - bal.DebitSum; // Normal Credit
                    if (value != 0) report.Liabilities[(AccountType)bal.Type] = value;
                }
                else if (bal.Category == (int)AccountCategory.Equity)
                {
                    decimal value = bal.CreditSum - bal.DebitSum; // Normal Credit
                    if (value != 0) report.Equity[(AccountType)bal.Type] = value;
                }
            }

            report.TotalAssets = report.Assets.Values.Sum();
            report.TotalLiabilities = report.Liabilities.Values.Sum();
            report.TotalEquity = report.Equity.Values.Sum();

            return report;
        }

        /// <summary>
        /// Generates a Profit & Loss Statement (Income Statement)
        /// </summary>
        public async Task<ProfitAndLossReport> GenerateProfitAndLossAsync(DateTime startDate, DateTime endDate)
        {
            var report = new ProfitAndLossReport { StartDate = startDate, EndDate = endDate };

            var incomeStmtEntries = await _dbContext.Set<LedgerEntry>()
                .Include(l => l.GLAccount)
                .Where(l => (l.GLAccount.Category == (int)AccountCategory.Revenue || l.GLAccount.Category == (int)AccountCategory.Expense)
                            && l.JournalEntry.EntryDate >= startDate
                            && l.JournalEntry.EntryDate <= endDate
                            && l.JournalEntry.IsPosted)
                .GroupBy(l => new { l.GLAccount.AccountName, l.GLAccount.Category })
                .Select(g => new
                {
                    Name = g.Key.AccountName,
                    Category = g.Key.Category,
                    DebitSum = g.Sum(x => x.Debit),
                    CreditSum = g.Sum(x => x.Credit)
                })
                .ToListAsync();

            foreach (var entry in incomeStmtEntries)
            {
                if (entry.Category == (int)AccountCategory.Revenue)
                {
                    decimal value = entry.CreditSum - entry.DebitSum; // Normal Credit
                    if (value != 0) report.Revenues[entry.Name] = value;
                }
                else if (entry.Category == (int)AccountCategory.Expense)
                {
                    decimal value = entry.DebitSum - entry.CreditSum; // Normal Debit
                    if (value != 0) report.Expenses[entry.Name] = value;
                }
            }

            report.GrossProfit = report.Revenues.Values.Sum();
            report.TotalExpenses = report.Expenses.Values.Sum();
            report.NetIncome = report.GrossProfit - report.TotalExpenses;

            return report;
        }

        /// <summary>
        /// Validates Double-Entry math and Posts the journal to the General Ledger.
        /// </summary>
        public async Task<bool> PostJournalEntryAsync(JournalEntry entry)
        {
            decimal debits = entry.LedgerEntries.Sum(e => e.Debit);
            decimal credits = entry.LedgerEntries.Sum(e => e.Credit);

            if (Math.Round(debits, 2) != Math.Round(credits, 2))
                throw new InvalidOperationException($"Double-entry mismatch! Debits: {debits}, Credits: {credits}");

            entry.IsPosted = true;

            _dbContext.Set<JournalEntry>().Add(entry);

            // Updates CurrentBalance in memory for the accounts attached
            foreach (var le in entry.LedgerEntries)
            {
                var acct = await _dbContext.Set<GLAccount>().FirstOrDefaultAsync(a => a.Id == le.GLAccountId);
                if (acct != null)
                {
                    if (acct.Category == (int)AccountCategory.Asset || acct.Category == (int)AccountCategory.Expense)
                        acct.CurrentBalance += (le.Debit - le.Credit);
                    else
                        acct.CurrentBalance += (le.Credit - le.Debit);
                }
            }

            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<JournalEntry> DraftInvoiceJournalEntryAsync(Invoice invoice)
        {
            var journalEntry = new JournalEntry
            {
                ReferenceNumber = $"INV-{invoice.InvoiceNumber}",
                Description = $"Accounts Receivable for Invoice {invoice.InvoiceNumber}",
                EntryDate = invoice.IssueDate,
                SourceDocumentType = "Invoice",
                SourceDocumentId = invoice.Id,
                IsPosted = false,
                LedgerEntries = new List<LedgerEntry>()
            };

            int arAccId = await GetOrCreateGLAccountAsync("Accounts Receivable", AccountType.AccountsReceivable, AccountCategory.Asset);
            int salesAccId = await GetOrCreateGLAccountAsync("Food Sales", AccountType.Sales, AccountCategory.Revenue);

            // Debit Accounts Receivable
            journalEntry.LedgerEntries.Add(new LedgerEntry { Description = "AR Increase", Debit = invoice.TotalAmount, Credit = 0, GLAccountId = arAccId });
            // Credit Sales Revenue
            journalEntry.LedgerEntries.Add(new LedgerEntry { Description = "Revenue Booked", Debit = 0, Credit = invoice.TotalAmount, GLAccountId = salesAccId });

            return journalEntry;
        }

        public async Task<JournalEntry> DraftBillJournalEntryAsync(Bill bill)
        {
            var journalEntry = new JournalEntry
            {
                ReferenceNumber = $"BILL-{bill.BillNumber}",
                Description = $"Accounts Payable for Bill {bill.BillNumber}",
                EntryDate = bill.Date,
                SourceDocumentType = "Bill",
                SourceDocumentId = bill.Id,
                IsPosted = false,
                LedgerEntries = new List<LedgerEntry>()
            };

            int foodCostAccId = await GetOrCreateGLAccountAsync("Cost of Goods Sold", AccountType.CostOfGoodsSold, AccountCategory.Expense);
            int apAccId = await GetOrCreateGLAccountAsync("Accounts Payable", AccountType.AccountsPayable, AccountCategory.Liability);

            // Debit Expense (COGS)
            journalEntry.LedgerEntries.Add(new LedgerEntry { Description = "Expense Booked", Debit = bill.TotalAmount, Credit = 0, GLAccountId = foodCostAccId });
            // Credit Accounts Payable
            journalEntry.LedgerEntries.Add(new LedgerEntry { Description = "AP Increase", Debit = 0, Credit = bill.TotalAmount, GLAccountId = apAccId });

            return journalEntry;
        }

        // Helper Method for testing logic locally
        private async Task<int> GetOrCreateGLAccountAsync(string defaultName, AccountType type, AccountCategory category)
        {
            var account = await _dbContext.Set<GLAccount>().FirstOrDefaultAsync(a => a.Type == (int)type);
            if (account == null)
            {
                account = new GLAccount
                {
                    AccountCode = $"{(int)category + 1}00{DateTime.Now.Millisecond}",
                    AccountName = defaultName,
                    Category = (int)category,
                    Type = (int)type,
                    IsActive = true
                };
                _dbContext.Set<GLAccount>().Add(account);
                await _dbContext.SaveChangesAsync();
            }
            return account.Id;
        }
    }
}
