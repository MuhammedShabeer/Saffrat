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
        private readonly IDateTimeService _dateTimeService;

        public DefaultAccountingEngine(RestaurantDBContext dbContext, IDateTimeService dateTimeService)
        {
            _dbContext = dbContext;
            _dateTimeService = dateTimeService;
        }

        /// <summary>
        /// Aggregates all daily transactions into a single "Daily Sales Summary" journal entry.
        /// </summary>
        public async Task<JournalEntry> PerformDailyCloseAsync(DateTime closeDate)
        {
            var startOfDay = new DateTime(closeDate.Year, closeDate.Month, closeDate.Day, 0, 0, 0);
            var endOfDay = startOfDay.AddDays(1).AddTicks(-1);

            string refNo = $"DC-{closeDate:yyyyMMdd}";
            var existing = await _dbContext.Set<JournalEntry>().AnyAsync(j => j.ReferenceNumber == refNo);
            if (existing)
            {
                throw new Exception($"Daily Close has already been performed for {closeDate:yyyy-MM-dd}.");
            }

            // Fetch raw operational data
            var dailyOrders = await _dbContext.Orders
                .Where(o => o.CreatedAt >= startOfDay && o.CreatedAt <= endOfDay)
                .ToListAsync();

            if (!dailyOrders.Any()) return null;

            var posOrders = dailyOrders.Where(o => o.PriceType != "VanSale").ToList();
            var vanOrders = dailyOrders.Where(o => o.PriceType == "VanSale").ToList();

            var totalSales = posOrders.Sum(o => o.SubTotal);
            var totalVanSales = vanOrders.Sum(o => o.SubTotal);
            var totalTaxes = dailyOrders.Sum(o => o.TaxTotal);
            var totalDiscounts = dailyOrders.Sum(o => o.DiscountTotal);
            var totalCharges = dailyOrders.Sum(o => o.ChargeTotal);
            
            // Group payments
            var paymentMethods = await _dbContext.PaymentMethods.ToListAsync();
            
            // 1. Standard POS Payments
            var posPayments = posOrders
                .GroupBy(o => o.PaymentMethod)
                .Select(g => new
                {
                    MethodTitle = g.Key,
                    Amount = g.Sum(o => o.Total),
                    GLAccountId = paymentMethods.FirstOrDefault(pm => pm.Title == g.Key)?.GLAccountId
                })
                .ToList();

            // 2. Van Sale Payments (Grouped by Driver)
            var vanPayments = vanOrders
                .GroupBy(o => o.ClosedBy)
                .Select(g => new
                {
                    DriverUsername = g.Key,
                    Amount = g.Sum(o => o.Total)
                })
                .ToList();

            // Create Master Journal
            var journalEntry = new JournalEntry
            {
                ReferenceNumber = refNo,
                Description = $"Daily Close Summary for {closeDate:yyyy-MM-dd}",
                EntryDate = closeDate.Date.AddHours(23).AddMinutes(59).AddSeconds(59),
                SourceDocumentType = "DailyClose",
                IsPosted = false,
                CreatedAt = _dateTimeService.Now(),
                LedgerEntries = new List<LedgerEntry>()
            };

            // Resolve General Accounts
            int salesAccId = await GetOrCreateGLAccountAsync("Food Sales", AccountType.Sales, AccountCategory.Revenue);
            int vanSalesAccId = await GetOrCreateGLAccountAsync("Van Sale Revenue", AccountType.Sales, AccountCategory.Revenue);
            int taxAccId = await GetOrCreateGLAccountAsync("Sales Tax", AccountType.OtherCurrentLiability, AccountCategory.Liability);
            int discountAccId = await GetOrCreateGLAccountAsync("General Sales Discounts", AccountType.Marketing, AccountCategory.Expense);
            int chargesAccId = await GetOrCreateGLAccountAsync("Service Charges", AccountType.OtherIncome, AccountCategory.Revenue);
            
            // Resolve Defaults by Flag
            int defaultCashAccId = await GetOrCreateGLAccountByFlagAsync("Main Cash", true, false);
            int defaultBankAccId = await GetOrCreateGLAccountByFlagAsync("Main Bank", false, true);

            // Debits: POS Cash/Bank accounts
            foreach (var pay in posPayments)
            {
                int targetAccId = pay.GLAccountId ?? 
                                 ((pay.MethodTitle?.ToLower().Contains("bank") ?? false) ? defaultBankAccId : defaultCashAccId);

                journalEntry.LedgerEntries.Add(new LedgerEntry 
                { 
                    Description = $"Daily Register {pay.MethodTitle}", 
                    Debit = pay.Amount, 
                    Credit = 0, 
                    GLAccountId = targetAccId 
                });
            }

            // Debits: Van Cash accounts
            var allUsers = await _dbContext.Users.ToListAsync();
            foreach (var vanPay in vanPayments)
            {
                var driver = allUsers.FirstOrDefault(x => x.UserName == vanPay.DriverUsername);
                int driverCashAccId = defaultCashAccId;

                if (driver != null)
                {
                    if (driver.VanCashAccountId.HasValue)
                    {
                        driverCashAccId = driver.VanCashAccountId.Value;
                    }
                    else
                    {
                        string accName = "Van Cash - " + (driver.FullName ?? driver.UserName);
                        driverCashAccId = await GetOrCreateGLAccountAsync(accName, AccountType.CashAndBank, AccountCategory.Asset);
                        
                        driver.VanCashAccountId = driverCashAccId;
                        _dbContext.Users.Update(driver);
                    }
                }

                journalEntry.LedgerEntries.Add(new LedgerEntry 
                { 
                    Description = $"Van Sales Collection: {driver?.FullName ?? vanPay.DriverUsername}", 
                    Debit = vanPay.Amount, 
                    Credit = 0, 
                    GLAccountId = driverCashAccId 
                });
            }

            // Credits: Revenue and Tax
            if (totalSales > 0)
                journalEntry.LedgerEntries.Add(new LedgerEntry { Description = "Daily Food Sales", Debit = 0, Credit = totalSales, GLAccountId = salesAccId });
            
            if (totalVanSales > 0)
                journalEntry.LedgerEntries.Add(new LedgerEntry { Description = "Daily Van Sales", Debit = 0, Credit = totalVanSales, GLAccountId = vanSalesAccId });

            journalEntry.LedgerEntries.Add(new LedgerEntry { Description = "Daily Tax Collected", Debit = 0, Credit = totalTaxes, GLAccountId = taxAccId });
            
            if (totalCharges != 0)
            {
                journalEntry.LedgerEntries.Add(new LedgerEntry { Description = "Daily Service Charges", Debit = 0, Credit = totalCharges, GLAccountId = chargesAccId });
            }

            // Debits: Discounts
            if (totalDiscounts != 0)
            {
                journalEntry.LedgerEntries.Add(new LedgerEntry { Description = "Daily Discounts Given", Debit = totalDiscounts, Credit = 0, GLAccountId = discountAccId });
            }

            // Post if valid
            await PostJournalEntryAsync(journalEntry);
            await _dbContext.SaveChangesAsync();

            return journalEntry;
        }

        /// <summary>
        /// Generates a Real-time Balance Sheet using DbContext.Set for DbFirst queries
        /// </summary>
        public async Task<BalanceSheetReport> GenerateBalanceSheetAsync(DateTime asOfDate)
        {
            var endOfAsOfDate = new DateTime(asOfDate.Year, asOfDate.Month, asOfDate.Day, 23, 59, 59, 999);
            var report = new BalanceSheetReport { AsOfDate = asOfDate };

            var actualBalances = await _dbContext.Set<LedgerEntry>()
                .Include(l => l.GLAccount)
                .Where(l => l.JournalEntry.EntryDate <= endOfAsOfDate && l.JournalEntry.IsPosted)
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
            var from = new DateTime(startDate.Year, startDate.Month, startDate.Day, 0, 0, 0);
            var to = new DateTime(endDate.Year, endDate.Month, endDate.Day, 23, 59, 59, 999);
            var report = new ProfitAndLossReport { StartDate = startDate, EndDate = endDate };

            var incomeStmtEntries = await _dbContext.Set<LedgerEntry>()
                .Include(l => l.GLAccount)
                .Where(l => (l.GLAccount.Category == (int)AccountCategory.Revenue || l.GLAccount.Category == (int)AccountCategory.Expense)
                            && l.JournalEntry.EntryDate >= from
                            && l.JournalEntry.EntryDate <= to
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
                CreatedAt = _dateTimeService.Now(),
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
                CreatedAt = _dateTimeService.Now(),
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

        public async Task<JournalEntry> RecordInvoicePaymentAsync(Invoice invoice, int? glAccountId = null)
        {
            var journalEntry = new JournalEntry
            {
                ReferenceNumber = $"PAY-{invoice.InvoiceNumber}",
                Description = $"Payment Receipt for Invoice {invoice.InvoiceNumber}",
                EntryDate = _dateTimeService.Now(),
                SourceDocumentType = "InvoicePayment",
                SourceDocumentId = invoice.Id,
                IsPosted = false,
                CreatedAt = _dateTimeService.Now(),
                LedgerEntries = new List<LedgerEntry>()
            };

            int cashAccId = glAccountId ?? await GetOrCreateGLAccountAsync("Cash & Bank", AccountType.CashAndBank, AccountCategory.Asset);
            int arAccId = await GetOrCreateGLAccountAsync("Accounts Receivable", AccountType.AccountsReceivable, AccountCategory.Asset);

            // Debit Cash (Asset Increase)
            journalEntry.LedgerEntries.Add(new LedgerEntry { Description = "Cash Received", Debit = invoice.TotalAmount, Credit = 0, GLAccountId = cashAccId });
            // Credit Accounts Receivable (Asset Decrease)
            journalEntry.LedgerEntries.Add(new LedgerEntry { Description = "AR Cleared", Debit = 0, Credit = invoice.TotalAmount, GLAccountId = arAccId });

            return journalEntry;
        }

        public async Task<JournalEntry> RecordBillPaymentAsync(Bill bill, int? glAccountId = null)
        {
            var journalEntry = new JournalEntry
            {
                ReferenceNumber = $"PMT-{bill.BillNumber}",
                Description = $"Payment Sent for Bill {bill.BillNumber}",
                EntryDate = _dateTimeService.Now(),
                SourceDocumentType = "BillPayment",
                SourceDocumentId = bill.Id,
                IsPosted = false,
                CreatedAt = _dateTimeService.Now(),
                LedgerEntries = new List<LedgerEntry>()
            };

            int apAccId = await GetOrCreateGLAccountAsync("Accounts Payable", AccountType.AccountsPayable, AccountCategory.Liability);
            int cashAccId = glAccountId ?? await GetOrCreateGLAccountAsync("Cash & Bank", AccountType.CashAndBank, AccountCategory.Asset);

            // Debit Accounts Payable (Liability Decrease)
            journalEntry.LedgerEntries.Add(new LedgerEntry { Description = "AP Cleared", Debit = bill.TotalAmount, Credit = 0, GLAccountId = apAccId });
            // Credit Cash (Asset Decrease)
            journalEntry.LedgerEntries.Add(new LedgerEntry { Description = "Cash Sent", Debit = 0, Credit = bill.TotalAmount, GLAccountId = cashAccId });

            return journalEntry;
        }

        // Helper Method for testing logic locally
        private async Task<int> GetOrCreateGLAccountAsync(string defaultName, AccountType type, AccountCategory category)
        {
            // Search by Name first to allow distinct "Heads" within the same AccountType
            var account = await _dbContext.Set<GLAccount>().FirstOrDefaultAsync(a => a.AccountName == defaultName);
            
            if (account == null)
            {
                account = new GLAccount
                {
                    AccountCode = $"{(int)category + 1}00{_dateTimeService.Now().Millisecond}",
                    AccountName = defaultName,
                    Category = (int)category,
                    Type = (int)type,
                    IsCash = (type == AccountType.CashAndBank),
                    IsActive = true
                };
                _dbContext.Set<GLAccount>().Add(account);
                await _dbContext.SaveChangesAsync();
            }
            return account.Id;
        }

        private async Task<int> GetOrCreateGLAccountByFlagAsync(string defaultName, bool isCash, bool isBank)
        {
            var account = await _dbContext.Set<GLAccount>().FirstOrDefaultAsync(a => (isCash && a.IsCash) || (isBank && a.IsBank));
            if (account == null)
            {
                account = new GLAccount
                {
                    AccountCode = isCash ? "1010" : "1020",
                    AccountName = defaultName,
                    Category = (int)AccountCategory.Asset,
                    Type = (int)AccountType.CashAndBank,
                    IsCash = isCash,
                    IsBank = isBank,
                    IsActive = true
                };
                _dbContext.Set<GLAccount>().Add(account);
                await _dbContext.SaveChangesAsync();
            }
            return account.Id;
        }

        /// <summary>
        /// Creates a draft payroll journal entry when payroll is first generated
        /// </summary>
        public async Task<JournalEntry> DraftPayrollJournalEntryAsync(Payroll payroll)
        {
            var journalEntry = new JournalEntry
            {
                ReferenceNumber = $"PAYROLL-{payroll.Id}",
                Description = $"Payroll Accrual for {payroll.Month}/{payroll.Year}",
                EntryDate = _dateTimeService.Now(),
                SourceDocumentType = "PayrollAccrual",
                SourceDocumentId = payroll.Id,
                IsPosted = false,
                CreatedAt = _dateTimeService.Now(),
                LedgerEntries = new List<LedgerEntry>()
            };

            int salaryExpenseAccId = await GetOrCreateGLAccountAsync("Salaries Expense", AccountType.Labor, AccountCategory.Expense);
            int salariesPayableAccId = await GetOrCreateGLAccountAsync("Salaries Payable", AccountType.OtherCurrentLiability, AccountCategory.Liability);

            // Debit Salary Expense
            journalEntry.LedgerEntries.Add(new LedgerEntry 
            { 
                Description = $"Payroll Accrual - {payroll.Employee?.Name}", 
                Debit = payroll.NetSalary, 
                Credit = 0, 
                GLAccountId = salaryExpenseAccId 
            });

            // Credit Salaries Payable
            journalEntry.LedgerEntries.Add(new LedgerEntry 
            { 
                Description = $"Salaries Payable - {payroll.Employee?.Name}", 
                Debit = 0, 
                Credit = payroll.NetSalary, 
                GLAccountId = salariesPayableAccId 
            });

            return journalEntry;
        }

        /// <summary>
        /// Records a partial/flexible payroll payment
        /// </summary>
        public async Task<JournalEntry> RecordFlexiblePayrollPaymentAsync(PayrollPayment payment, Payroll payroll, int? glAccountId = null)
        {
            var journalEntry = new JournalEntry
            {
                ReferenceNumber = $"PAYROL-PAY-{payment.Id}",
                Description = $"Partial Payroll Payment - {payroll.Employee?.Name}",
                EntryDate = payment.PaymentDate,
                SourceDocumentType = "PayrollPayment",
                SourceDocumentId = payroll.Id,
                IsPosted = false,
                CreatedAt = _dateTimeService.Now(),
                LedgerEntries = new List<LedgerEntry>()
            };

            int cashAccId = glAccountId ?? await GetOrCreateGLAccountAsync("Cash & Bank", AccountType.CashAndBank, AccountCategory.Asset);
            int salariesPayableAccId = await GetOrCreateGLAccountAsync("Salaries Payable", AccountType.OtherCurrentLiability, AccountCategory.Liability);

            // Debit Cash & Bank (Asset Increase) - but it's a decrease in cash
            journalEntry.LedgerEntries.Add(new LedgerEntry 
            { 
                Description = $"Salary Payment - {payroll.Employee?.Name}", 
                Debit = 0, 
                Credit = payment.Amount, 
                GLAccountId = cashAccId 
            });

            // Credit Salaries Payable (Liability Decrease)
            journalEntry.LedgerEntries.Add(new LedgerEntry 
            { 
                Description = $"Salaries Payable Cleared - {payroll.Employee?.Name}", 
                Debit = payment.Amount, 
                Credit = 0, 
                GLAccountId = salariesPayableAccId 
            });

            return journalEntry;
        }

        /// <summary>
        /// Reverses a Journal Entry and its Ledger Entries, adjusting account balances back.
        /// </summary>
        public async Task<bool> ReverseJournalEntryAsync(int journalEntryId)
        {
            var journalEntry = await _dbContext.Set<JournalEntry>()
                .Include(j => j.LedgerEntries)
                .FirstOrDefaultAsync(j => j.Id == journalEntryId);

            if (journalEntry == null) return false;

            // Undo the balance changes for each ledger entry
            foreach (var le in journalEntry.LedgerEntries)
            {
                var acct = await _dbContext.Set<GLAccount>().FirstOrDefaultAsync(a => a.Id == le.GLAccountId);
                if (acct != null)
                {
                    // If it's an Asset or Expense, balance = Debits - Credits
                    // To reverse: if we added (Debit - Credit), we now subtract (Debit - Credit)
                    if (acct.Category == (int)AccountCategory.Asset || acct.Category == (int)AccountCategory.Expense)
                        acct.CurrentBalance -= (le.Debit - le.Credit);
                    else
                        acct.CurrentBalance -= (le.Credit - le.Debit);
                }
            }

            // Remove entries from database
            _dbContext.Set<LedgerEntry>().RemoveRange(journalEntry.LedgerEntries);
            _dbContext.Set<JournalEntry>().Remove(journalEntry);

            await _dbContext.SaveChangesAsync();
            return true;
        }
    }
}
