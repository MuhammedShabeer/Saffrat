using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Saffrat.Models;
using Saffrat.Models.AccountingEngine;
using Saffrat.Services;
using Saffrat.Services.AccountingEngine;
using Saffrat.ViewModels;

namespace Saffrat.Controllers
{
    [Authorize(Roles = "admin")]
    public class AccountingEngineController : BaseController
    {
        private readonly RestaurantDBContext _dbContext;
        private readonly IAccountingEngine _accountingEngine;

        public AccountingEngineController(RestaurantDBContext dbContext, IAccountingEngine accountingEngine,
            ILanguageService languageService, ILocalizationService localizationService, IDateTimeService dateTimeService)
        : base(languageService, localizationService, dateTimeService)
        {
            _dbContext = dbContext;
            _accountingEngine = accountingEngine;
        }

        [HttpPost]
        public async Task<IActionResult> SaveBankEntry(DateTime entryDate, string referenceNumber, int bankAccountId, string transactionType, int offsetAccountId, decimal amount, string description)
        {
            return await SaveEntryInternal("Bank", entryDate, referenceNumber, bankAccountId, transactionType, offsetAccountId, amount, description);
        }

        [HttpPost]
        public async Task<IActionResult> SaveCashEntry(DateTime entryDate, string referenceNumber, int bankAccountId, string transactionType, int offsetAccountId, decimal amount, string description)
        {
            return await SaveEntryInternal("Cash", entryDate, referenceNumber, bankAccountId, transactionType, offsetAccountId, amount, description);
        }

        private async Task<IActionResult> SaveEntryInternal(string sourceType, DateTime entryDate, string referenceNumber, int bankAccountId, string transactionType, int offsetAccountId, decimal amount, string description)
        {
            try
            {
                var bankAccount = await _dbContext.GLAccounts.FindAsync(bankAccountId);
                var offsetAccount = await _dbContext.GLAccounts.FindAsync(offsetAccountId);

                if (bankAccount == null || offsetAccount == null)
                    return Json(new { status = "error", message = "Invalid account selected." });

                var prefix = sourceType == "Cash" ? "CASH-" : "BANK-";
                var journal = new JournalEntry
                {
                    EntryDate = entryDate.Date.Add(CurrentDateTime().TimeOfDay),
                    ReferenceNumber = prefix + (string.IsNullOrEmpty(referenceNumber) ? CurrentDateTime().Ticks.ToString().Substring(10) : referenceNumber),
                    Description = description ?? $"Manual {sourceType} {transactionType}",
                    SourceDocumentType = sourceType + "Entry",
                    SourceDocumentId = 0,
                    CreatedAt = CurrentDateTime()
                };

                decimal bankDebit = transactionType == "Inflow" ? amount : 0;
                decimal bankCredit = transactionType == "Outflow" ? amount : 0;
                decimal offsetDebit = transactionType == "Outflow" ? amount : 0;
                decimal offsetCredit = transactionType == "Inflow" ? amount : 0;

                journal.LedgerEntries.Add(new LedgerEntry { GLAccountId = bankAccountId, Description = journal.Description, Debit = bankDebit, Credit = bankCredit });
                journal.LedgerEntries.Add(new LedgerEntry { GLAccountId = offsetAccountId, Description = journal.Description, Debit = offsetDebit, Credit = offsetCredit });

                await _accountingEngine.PostJournalEntryAsync(journal);
                return Json(new { status = "success", message = "Transaction saved successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> AddBankEntry()
        {
            ViewBag.BankAccounts = await _dbContext.GLAccounts.Where(x => x.IsActive && x.IsBank).ToListAsync();
            ViewBag.AllAccounts = await _dbContext.GLAccounts.Where(x => x.IsActive).OrderBy(x => x.AccountCode).ToListAsync();
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> AddCashEntry()
        {
            ViewBag.BankAccounts = await _dbContext.GLAccounts.Where(x => x.IsActive && x.IsCash).ToListAsync();
            ViewBag.AllAccounts = await _dbContext.GLAccounts.Where(x => x.IsActive).OrderBy(x => x.AccountCode).ToListAsync();
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> BankTransactions()
        {
            var bankAccounts = await _dbContext.GLAccounts.Where(x => x.IsActive && x.IsBank).Select(x => x.Id).ToListAsync();
            var journals = await _dbContext.JournalEntries
                .Where(j => j.LedgerEntries.Any(e => bankAccounts.Contains(e.GLAccountId)))
                .Include(j => j.LedgerEntries).ThenInclude(e => e.GLAccount)
                .OrderByDescending(j => j.EntryDate).ThenByDescending(j => j.Id)
                .Take(200).ToListAsync();
            return View(journals);
        }

        [HttpGet]
        public async Task<IActionResult> CashTransactions()
        {
            var cashAccounts = await _dbContext.GLAccounts.Where(x => x.IsActive && x.IsCash).Select(x => x.Id).ToListAsync();
            var journals = await _dbContext.JournalEntries
                .Where(j => j.LedgerEntries.Any(e => cashAccounts.Contains(e.GLAccountId)))
                .Include(j => j.LedgerEntries).ThenInclude(e => e.GLAccount)
                .OrderByDescending(j => j.EntryDate).ThenByDescending(j => j.Id)
                .Take(200).ToListAsync();
            return View(journals);
        }

        [HttpGet]
        public async Task<IActionResult> BulkAddBankEntry()
        {
            return await BulkAddEntryInternal("Bank");
        }

        [HttpGet]
        public async Task<IActionResult> BulkAddCashEntry()
        {
            return await BulkAddEntryInternal("Cash");
        }

        private async Task<IActionResult> BulkAddEntryInternal(string sourceType)
        {
            var bankAccounts = sourceType == "Cash" 
                ? await _dbContext.GLAccounts.Where(x => x.IsActive && x.IsCash).ToListAsync()
                : await _dbContext.GLAccounts.Where(x => x.IsActive && x.IsBank).ToListAsync();
            
            var offsetAccounts = await _dbContext.GLAccounts.Where(x => x.IsActive).OrderBy(x => x.AccountCode).ToListAsync();

            var model = new BulkEntryVM { SourceType = sourceType, BankAccounts = bankAccounts, OffsetAccounts = offsetAccounts };
            for (int i = 0; i < 5; i++) model.Entries.Add(new BulkEntryRow());

            return View("BulkAddEntry", model);
        }

        [HttpPost]
        public async Task<IActionResult> SaveBulkEntries([FromBody] BulkEntryVM model)
        {
            try
            {
                if (model.Entries == null || !model.Entries.Any())
                    return Json(new { status = "error", message = "No entries to save." });

                int savedCount = 0;
                foreach (var entry in model.Entries)
                {
                    if (entry.Amount <= 0 || entry.BankAccountId == 0 || entry.OffsetAccountId == 0) continue;

                    var prefix = model.SourceType == "Cash" ? "CASH-" : "BANK-";
                    var now = CurrentDateTime();
                    var journal = new JournalEntry
                    {
                        EntryDate = entry.EntryDate.Date.Add(now.TimeOfDay),
                        ReferenceNumber = prefix + (string.IsNullOrEmpty(entry.ReferenceNumber) ? now.Ticks.ToString().Substring(10) : entry.ReferenceNumber),
                        Description = entry.Description ?? $"Bulk {model.SourceType} {entry.TransactionType}",
                        SourceDocumentType = model.SourceType + "Entry",
                        SourceDocumentId = 0,
                        CreatedAt = now
                    };

                    decimal bankDebit = entry.TransactionType == "Inflow" ? entry.Amount : 0;
                    decimal bankCredit = entry.TransactionType == "Outflow" ? entry.Amount : 0;
                    decimal offsetDebit = entry.TransactionType == "Outflow" ? entry.Amount : 0;
                    decimal offsetCredit = entry.TransactionType == "Inflow" ? entry.Amount : 0;

                    journal.LedgerEntries.Add(new LedgerEntry { GLAccountId = entry.BankAccountId, Description = journal.Description, Debit = bankDebit, Credit = bankCredit });
                    journal.LedgerEntries.Add(new LedgerEntry { GLAccountId = entry.OffsetAccountId, Description = journal.Description, Debit = offsetDebit, Credit = offsetCredit });

                    await _accountingEngine.PostJournalEntryAsync(journal);
                    savedCount++;
                }

                return Json(new { status = "success", message = $"{savedCount} transactions saved successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ChartOfAccounts()
        {
            // Fetch directly from DB via DbSet or Set<T>
            var accounts = await _dbContext.Set<GLAccount>().ToListAsync();
            return View(accounts);
        }

        [HttpGet]
        public async Task<IActionResult> JournalEntries(DateTime? start, DateTime? end, int? accountId, string search)
        {
            var now = CurrentDateTime();
            var startDate = start ?? new DateTime(now.Year, now.Month, 1);
            var endDate = end ?? now;

            var matchingReferenceNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var query = _dbContext.Set<JournalEntry>()
                .Include(j => j.LedgerEntries)
                .ThenInclude(l => l.GLAccount)
                .AsQueryable();

            query = query.Where(j => j.EntryDate >= startDate && j.EntryDate <= endDate);

            if (accountId.HasValue && accountId > 0)
            {
                query = query.Where(j => j.LedgerEntries.Any(l => l.GLAccountId == accountId));
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower().Trim();

                // Search Purchases
                var purchaseIds = await _dbContext.Purchases
                    .Include(p => p.Supplier)
                    .Include(p => p.PurchaseDetails).ThenInclude(d => d.IngredientItem)
                    .Where(p =>
                        (p.InvoiceNo != null && p.InvoiceNo.ToLower().Contains(s)) ||
                        (p.Description != null && p.Description.ToLower().Contains(s)) ||
                        (p.Supplier != null && p.Supplier.SupplierName.ToLower().Contains(s)) ||
                        p.PurchaseDetails.Any(d => d.IngredientItem != null && d.IngredientItem.ItemName.ToLower().Contains(s)))
                    .Select(p => p.Id)
                    .ToListAsync();

                foreach (var id in purchaseIds)
                {
                    if (id.HasValue) matchingReferenceNumbers.Add($"PUR-{id.Value}");
                }

                // Search Payroll Accrual
                var payrollIds = await _dbContext.Payrolls
                    .Include(p => p.Employee)
                    .Where(p => p.Employee != null && p.Employee.Name.ToLower().Contains(s))
                    .Select(p => p.Id)
                    .ToListAsync();
                foreach (var id in payrollIds) matchingReferenceNumbers.Add($"PAYROLL-{id}");

                // Search Payroll Payments
                var payrollPaymentIds = await _dbContext.PayrollPayments
                    .Include(p => p.Payroll).ThenInclude(pr => pr.Employee)
                    .Where(p => p.Payroll.Employee != null && p.Payroll.Employee.Name.ToLower().Contains(s))
                    .Select(p => p.Id)
                    .ToListAsync();
                foreach (var id in payrollPaymentIds) matchingReferenceNumbers.Add($"PAYROL-PAY-{id}");

                // Search Invoices
                var invoiceIds = await (from i in _dbContext.Invoices
                                        join c in _dbContext.Customers on i.CustomerId equals c.Id
                                        where (i.InvoiceNumber != null && i.InvoiceNumber.ToLower().Contains(s)) ||
                                              (c.CustomerName != null && c.CustomerName.ToLower().Contains(s))
                                        select new { i.Id, i.InvoiceNumber }).ToListAsync();
                foreach (var inv in invoiceIds) 
                {
                    matchingReferenceNumbers.Add($"INV-{inv.InvoiceNumber}");
                    matchingReferenceNumbers.Add($"PAY-{inv.InvoiceNumber}");
                }

                // Search Bills
                var billIds = await (from b in _dbContext.Bills
                                     join spp in _dbContext.Suppliers on b.SupplierId equals spp.Id
                                     where (b.BillNumber != null && b.BillNumber.ToLower().Contains(s)) ||
                                           (spp.SupplierName != null && spp.SupplierName.ToLower().Contains(s))
                                     select new { b.Id, b.BillNumber }).ToListAsync();
                foreach (var bill in billIds)
                {
                    matchingReferenceNumbers.Add($"BILL-{bill.BillNumber}");
                    matchingReferenceNumbers.Add($"PMT-{bill.BillNumber}");
                }

                // Search Orders
                var orderIds = await _dbContext.Orders
                    .Include(o => o.OrderDetails).ThenInclude(d => d.Item)
                    .Where(o =>
                        (o.Note != null && o.Note.ToLower().Contains(s)) ||
                        o.OrderDetails.Any(d => d.Item != null && d.Item.ItemName.ToLower().Contains(s)))
                    .Select(o => o.Id)
                    .ToListAsync();
                foreach (var id in orderIds) matchingReferenceNumbers.Add($"SALE-{id}");

                query = query.Where(j =>
                    (j.Description != null && j.Description.ToLower().Contains(s)) ||
                    (j.ReferenceNumber != null && (
                        j.ReferenceNumber.ToLower().Contains(s) ||
                        matchingReferenceNumbers.Contains(j.ReferenceNumber)
                    ))
                );
            }

            var entries = await query.OrderByDescending(j => j.EntryDate).ToListAsync();

            ViewBag.Start = startDate.ToString("yyyy-MM-dd");
            ViewBag.End = endDate.ToString("yyyy-MM-dd");
            ViewBag.AccountId = accountId;
            ViewBag.Search = search;
            ViewBag.Accounts = await _dbContext.Set<GLAccount>().OrderBy(a => a.AccountCode).ToListAsync();

            return View(entries);
        }
        [HttpGet]
        public async Task<IActionResult> BalanceSheet(DateTime? asOfDate)
        {
            var date = asOfDate ?? CurrentDateTime();
            ViewBag.Date = date.ToString("yyyy-MM-dd");
            var report = await _accountingEngine.GenerateBalanceSheetAsync(date);
            return View(report);
        }

        [HttpGet]
        public async Task<IActionResult> ProfitAndLoss(DateTime? start, DateTime? end)
        {
            var now = CurrentDateTime();
            var startDate = start ?? new DateTime(now.Year, now.Month, 1);
            var endDate = end ?? now;
            ViewBag.Start = startDate.ToString("yyyy-MM-dd");
            ViewBag.End = endDate.ToString("yyyy-MM-dd");

            var report = await _accountingEngine.GenerateProfitAndLossAsync(startDate, endDate);
            return View(report);
        }

        [HttpGet]
        public async Task<IActionResult> GetInvoiceDetails(int id)
        {
            var invoice = await _dbContext.Set<Invoice>()
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null) return NotFound();

            var customer = await _dbContext.Customers.FirstOrDefaultAsync(c => c.Id == invoice.CustomerId);

            return Json(new
            {
                invoice.InvoiceNumber,
                CustomerName = customer?.CustomerName ?? "Unknown",
                IssueDate = invoice.IssueDate.ToString("yyyy-MM-dd"),
                DueDate = invoice.DueDate.ToString("yyyy-MM-dd"),
                invoice.TotalAmount,
                invoice.AmountPaid,
                invoice.Status,
                invoice.JournalEntryId
            });
        }

        [HttpGet]
        public async Task<IActionResult> Invoices()
        {
            var invoices = await _dbContext.Set<Invoice>().ToListAsync();
            ViewBag.Customers = await _dbContext.Customers.ToListAsync();
            return View(invoices);
        }

        [HttpPost]
        public async Task<IActionResult> AddInvoice(Invoice invoice)
        {
            try
            {
                invoice.InvoiceNumber = $"INV-{_dateTimeService.Now():yyyyMMddHHmmss}";
                invoice.Status = "Draft";
                invoice.AmountPaid = 0;

                _dbContext.Set<Invoice>().Add(invoice);
                await _dbContext.SaveChangesAsync();

                var journalEntry = await _accountingEngine.DraftInvoiceJournalEntryAsync(invoice);
                await _accountingEngine.PostJournalEntryAsync(journalEntry);

                invoice.JournalEntryId = journalEntry.Id;
                _dbContext.Set<Invoice>().Update(invoice);
                await _dbContext.SaveChangesAsync();

                return Json(new { status = "success", message = "Draft Invoice created successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> MarkInvoicePaid(int id)
        {
            try
            {
                var invoice = await _dbContext.Set<Invoice>().FirstOrDefaultAsync(i => i.Id == id);
                if (invoice == null) return Json(new { status = "error", message = "Invoice not found." });

                if (invoice.Status == "Paid") return Json(new { status = "warning", message = "Invoice is already paid." });

                // 1. Generate Payment Journal Entry
                var paymentJournal = await _accountingEngine.RecordInvoicePaymentAsync(invoice);
                await _accountingEngine.PostJournalEntryAsync(paymentJournal);

                // 2. Update Invoice Status
                invoice.Status = "Paid";
                invoice.AmountPaid = invoice.TotalAmount;
                // Note: We don't overwrite invoice.JournalEntryId because that points to the original AR generation.
                // The payment journal acts as a separate ledger entry linked via SourceDocumentId in the Engine.

                _dbContext.Set<Invoice>().Update(invoice);
                await _dbContext.SaveChangesAsync();

                return Json(new { status = "success", message = "Invoice marked as paid and journal entry created." });
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Bills()
        {
            var bills = await _dbContext.Set<Bill>().ToListAsync();
            ViewBag.Suppliers = await _dbContext.Suppliers.ToListAsync();
            return View(bills);
        }

        [HttpGet]
        public async Task<IActionResult> GetBillDetails(int id)
        {
            var bill = await _dbContext.Set<Bill>().FirstOrDefaultAsync(b => b.Id == id);
            if (bill == null) return NotFound();

            var supplier = await _dbContext.Suppliers.FirstOrDefaultAsync(s => s.Id == bill.SupplierId);

            return Json(new
            {
                bill.BillNumber,
                SupplierName = supplier?.SupplierName ?? "Unknown",
                Date = bill.Date.ToString("yyyy-MM-dd"),
                DueDate = bill.DueDate.ToString("yyyy-MM-dd"),
                bill.TotalAmount,
                bill.AmountPaid,
                bill.Status,
                bill.JournalEntryId
            });
        }

        [HttpPost]
        public async Task<IActionResult> AddBill(Bill newBill)
        {
            try
            {
                newBill.BillNumber = $"BILL-{_dateTimeService.Now():yyyyMMddHHmmss}";
                newBill.Status = "Draft";
                newBill.AmountPaid = 0;

                _dbContext.Set<Bill>().Add(newBill);
                await _dbContext.SaveChangesAsync();

                var journalEntry = await _accountingEngine.DraftBillJournalEntryAsync(newBill);
                await _accountingEngine.PostJournalEntryAsync(journalEntry);

                newBill.JournalEntryId = journalEntry.Id;
                _dbContext.Set<Bill>().Update(newBill);
                await _dbContext.SaveChangesAsync();

                return Json(new { status = "success", message = "Bill saved successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> MarkBillPaid(int id)
        {
            try
            {
                var bill = await _dbContext.Set<Bill>().FirstOrDefaultAsync(b => b.Id == id);
                if (bill == null) return Json(new { status = "error", message = "Bill not found." });

                if (bill.Status == "Paid") return Json(new { status = "warning", message = "Bill is already paid." });

                var paymentJournal = await _accountingEngine.RecordBillPaymentAsync(bill);
                await _accountingEngine.PostJournalEntryAsync(paymentJournal);

                bill.Status = "Paid";
                bill.AmountPaid = bill.TotalAmount;

                _dbContext.Set<Bill>().Update(bill);
                await _dbContext.SaveChangesAsync();

                return Json(new { status = "success", message = "Bill marked as paid and journal entry created." });
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RunDailyClose(DateTime closeDate)
        {
            try
            {
                var entry = await _accountingEngine.PerformDailyCloseAsync(closeDate);
                if (entry == null)
                {
                    return Json(new { status = "error", message = "No orders found for this date." });
                }
                return Json(new { status = "success", message = "Daily Close Journal generated!" });
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message });
            }
        }

        // --- PAYROLL PAYMENT FLOWS ---

        [HttpGet]
        public async Task<IActionResult> GetPayrollsList()
        {
            try
            {
                var payrolls = await _dbContext.Payrolls
                    .Include(p => p.Employee)
                    .Include(p => p.PayrollPayments)
                    .OrderByDescending(p => p.Year)
                    .ThenByDescending(p => p.Month)
                    .ToListAsync();

                return Json(new
                {
                    status = "success",
                    data = payrolls.Select(p => new
                    {
                        p.Id,
                        p.Month,
                        p.Year,
                        p.Salary,
                        p.NetSalary,
                        p.TotalAmountPaid,
                        p.RemainingBalance,
                        p.PaymentStatus,
                        Employee = new
                        {
                            p.Employee.Id,
                            p.Employee.Name,
                            p.Employee.Email,
                            p.Employee.Phone
                        }
                    })
                });
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AccruePayroll(int payrollId)
        {
            try
            {
                var payroll = await _dbContext.Payrolls
                    .Include(p => p.Employee)
                    .FirstOrDefaultAsync(p => p.Id == payrollId);

                if (payroll == null)
                    return Json(new { status = "error", message = "Payroll not found." });

                if (payroll.JournalEntryId.HasValue)
                    return Json(new { status = "warning", message = "Payroll is already accrued." });

                // Create accrual journal entry
                var accrualEntry = await _accountingEngine.DraftPayrollJournalEntryAsync(payroll);
                await _accountingEngine.PostJournalEntryAsync(accrualEntry);

                // Update payroll with journal entry reference
                payroll.JournalEntryId = accrualEntry.Id;
                payroll.RemainingBalance = payroll.NetSalary;
                _dbContext.Payrolls.Update(payroll);
                await _dbContext.SaveChangesAsync();

                return Json(new { status = "success", message = "Payroll accrued successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> PayPartialSalary(int payrollId, decimal amount, int? glAccountId, string paymentMethod = "Cash", string notes = "")
        {
            try
            {
                var payroll = await _dbContext.Payrolls
                    .Include(p => p.Employee)
                    .Include(p => p.PayrollPayments)
                    .FirstOrDefaultAsync(p => p.Id == payrollId);

                if (payroll == null)
                    return Json(new { status = "error", message = "Payroll not found." });

                if (!payroll.JournalEntryId.HasValue)
                    return Json(new { status = "error", message = "Payroll must be accrued before making payments." });

                if (amount <= 0)
                    return Json(new { status = "error", message = "Payment amount must be greater than 0." });

                // Check if payment exceeds remaining balance
                decimal remainingBalance = payroll.NetSalary - payroll.TotalAmountPaid;
                if (amount > remainingBalance)
                    return Json(new { status = "error", message = $"Payment amount exceeds remaining balance of {remainingBalance:C}." });

                // Auto-determine payment method based on account type
                string actualPaymentMethod = paymentMethod;
                if (glAccountId.HasValue)
                {
                    var account = await _dbContext.GLAccounts.FindAsync(glAccountId.Value);
                    if (account != null)
                    {
                        actualPaymentMethod = account.IsBank ? "BankTransfer" : "Cash";
                    }
                }

                // Create payment record
                var payment = new PayrollPayment
                {
                    PayrollId = payrollId,
                    Amount = amount,
                    PaymentMethod = actualPaymentMethod,
                    PaymentDate = CurrentDateTime(),
                    Notes = notes,
                    CreatedAt = CurrentDateTime()
                };

                _dbContext.PayrollPayments.Add(payment);
                await _dbContext.SaveChangesAsync();

                // Create journal entry for the payment
                var paymentEntry = await _accountingEngine.RecordFlexiblePayrollPaymentAsync(payment, payroll, glAccountId);
                await _accountingEngine.PostJournalEntryAsync(paymentEntry);

                // Update payment with journal entry reference
                payment.JournalEntryId = paymentEntry.Id;
                _dbContext.PayrollPayments.Update(payment);

                // Update payroll tracking
                payroll.TotalAmountPaid += amount;
                payroll.RemainingBalance = payroll.NetSalary - payroll.TotalAmountPaid;

                // Update payment status
                if (payroll.RemainingBalance <= 0)
                {
                    payroll.PaymentStatus = "Paid";
                }
                else if (payroll.TotalAmountPaid > 0)
                {
                    payroll.PaymentStatus = "PartiallyPaid";
                }

                _dbContext.Payrolls.Update(payroll);
                await _dbContext.SaveChangesAsync();

                return Json(new
                {
                    status = "success",
                    message = $"Payment of {amount:C} recorded successfully.",
                    payroll = new
                    {
                        payroll.Id,
                        payroll.NetSalary,
                        payroll.TotalAmountPaid,
                        payroll.RemainingBalance,
                        payroll.PaymentStatus
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> PayrollPaymentDetails(int payrollId)
        {
            try
            {
                var payroll = await _dbContext.Payrolls
                    .Include(p => p.Employee)
                    .Include(p => p.PayrollPayments)
                    .FirstOrDefaultAsync(p => p.Id == payrollId);

                if (payroll == null)
                    return Json(new { status = "error", message = "Payroll not found." });

                return Json(new
                {
                    status = "success",
                    data = new
                    {
                        payroll.Id,
                        EmployeeName = payroll.Employee?.Name,
                        Period = $"{payroll.Month}/{payroll.Year}",
                        payroll.Salary,
                        payroll.NetSalary,
                        payroll.TotalAmountPaid,
                        payroll.AdvanceAmountPaid,
                        payroll.RemainingBalance,
                        payroll.PaymentStatus,
                        Payments = payroll.PayrollPayments.OrderByDescending(p => p.PaymentDate).Select(p => new
                        {
                            p.Id,
                            p.Amount,
                            p.PaymentMethod,
                            PaymentDate = p.PaymentDate.ToString("yyyy-MM-dd"),
                            p.Notes
                        })
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ReversePayrollPayment(int paymentId)
        {
            try
            {
                var payment = await _dbContext.PayrollPayments
                    .Include(p => p.Payroll)
                    .FirstOrDefaultAsync(p => p.Id == paymentId);

                if (payment == null)
                    return Json(new { status = "error", message = "Payment not found." });

                var payroll = payment.Payroll;

                // Reverse the payment tracking
                payroll.TotalAmountPaid -= payment.Amount;
                payroll.RemainingBalance = payroll.NetSalary - payroll.TotalAmountPaid;

                // Update payment status
                if (payroll.TotalAmountPaid == 0)
                {
                    payroll.PaymentStatus = "Unpaid";
                }
                else if (payroll.TotalAmountPaid < payroll.NetSalary)
                {
                    payroll.PaymentStatus = "PartiallyPaid";
                }

                _dbContext.Payrolls.Update(payroll);

                // Delete payment and its journal entry using the service to maintain balance integrity
                if (payment.JournalEntryId.HasValue)
                {
                    await _accountingEngine.ReverseJournalEntryAsync(payment.JournalEntryId.Value);
                }

                _dbContext.PayrollPayments.Remove(payment);
                await _dbContext.SaveChangesAsync();

                return Json(new { status = "success", message = "Payment reversed successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message });
            }
        }

        // --- DATA ENTRY FLOWS ---

        [HttpGet]
        public IActionResult AddAccount()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddAccount(GLAccount account)
        {
            if (ModelState.IsValid)
            {
                account.IsActive = true;
                account.CurrentBalance = 0;
                _dbContext.Set<GLAccount>().Add(account);
                await _dbContext.SaveChangesAsync();
                return RedirectToAction(nameof(ChartOfAccounts));
            }
            return View(account);
        }

        [HttpGet]
        public async Task<IActionResult> EditAccount(int id)
        {
            var account = await _dbContext.Set<GLAccount>().FindAsync(id);
            if (account == null) return NotFound();
            return View(account);
        }

        [HttpPost]
        public async Task<IActionResult> EditAccount(GLAccount account)
        {
            if (ModelState.IsValid)
            {
                var existing = await _dbContext.Set<GLAccount>().FindAsync(account.Id);
                if (existing == null) return NotFound();

                // Update properties
                existing.AccountName = account.AccountName;
                existing.AccountCode = account.AccountCode;
                existing.Category = account.Category;
                existing.Type = account.Type;
                existing.SubType = account.SubType;
                existing.IsCash = account.IsCash;
                existing.IsBank = account.IsBank;
                existing.Description = account.Description;
                existing.Description = account.Description;
                existing.IsActive = account.IsActive;

                await _dbContext.SaveChangesAsync();
                return RedirectToAction(nameof(ChartOfAccounts));
            }
            return View(account);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAccount(int id)
        {
            var account = await _dbContext.Set<GLAccount>().FindAsync(id);
            if (account == null) return Json(new { success = false, message = "Account not found." });

            // VALIDATION: Check if any LedgerEntries exist for this account
            bool hasTransactions = await _dbContext.Set<LedgerEntry>().AnyAsync(l => l.GLAccountId == id);
            if (hasTransactions)
            {
                return Json(new { success = false, message = "Cannot delete account because it has transaction history (ledger entries)." });
            }

            _dbContext.Set<GLAccount>().Remove(account);
            await _dbContext.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> ManualJournal()
        {
            ViewBag.Accounts = await _dbContext.Set<GLAccount>().Where(a => a.IsActive).ToListAsync();
            return View(new JournalEntry { EntryDate = CurrentDateTime() });
        }

        [HttpPost]
        public async Task<IActionResult> ManualJournal(JournalEntry entry)
        {
            if (ModelState.IsValid)
            {
                var now = CurrentDateTime();
                entry.EntryDate = entry.EntryDate.Date.Add(now.TimeOfDay);
                entry.SourceDocumentType = "ManualJournal";
                entry.CreatedAt = now;
                entry.IsPosted = false;

                try
                {
                    // Clean up empty lines
                    entry.LedgerEntries = entry.LedgerEntries.Where(l => l.GLAccountId > 0 && (l.Debit > 0 || l.Credit > 0)).ToList();

                    await _accountingEngine.PostJournalEntryAsync(entry);
                    return RedirectToAction(nameof(JournalEntries));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }
            ViewBag.Accounts = await _dbContext.Set<GLAccount>().Where(a => a.IsActive).ToListAsync();
            return View(entry);
        }
        [HttpGet]
        public async Task<IActionResult> GetJournalDetails(int id)
        {
            var entry = await _dbContext.Set<JournalEntry>()
                .Include(j => j.LedgerEntries)
                .ThenInclude(l => l.GLAccount)
                .FirstOrDefaultAsync(j => j.Id == id);

            if (entry == null) return NotFound();

            var result = new
            {
                entry.ReferenceNumber,
                entry.Description,
                EntryDate = entry.EntryDate.ToString("yyyy-MM-dd HH:mm"),
                SourceDocumentType = entry.SourceDocumentType,
                SourceDocumentId = entry.SourceDocumentId,
                Lines = entry.LedgerEntries.Select(l => new
                {
                    AccountName = l.GLAccount.AccountName,
                    AccountCode = l.GLAccount.AccountCode,
                    l.Description,
                    l.Debit,
                    l.Credit
                })
            };

            return Json(result);
        }

        [HttpGet]
        public async Task<JsonResult> GetSourceDocumentDetails(string type, int id)
        {
            try
            {
                switch (type?.ToLower())
                {
                    case "payroll":
                        var payroll = await _dbContext.Payrolls
                            .Include(p => p.Employee).ThenInclude(e => e.Department)
                            .Include(p => p.Employee).ThenInclude(e => e.Designation)
                            .Include(p => p.PayrollDetails)
                            .FirstOrDefaultAsync(p => p.Id == id);
                        if (payroll == null) return Json(new { status = "error", message = "Payroll not found" });
                        return Json(new
                        {
                            status = "success",
                            type = "Payroll",
                            data = new
                            {
                                EmployeeName = payroll.Employee.Name,
                                Department = payroll.Employee.Department?.Title,
                                Designation = payroll.Employee.Designation?.Title,
                                Period = $"{payroll.Month}/{payroll.Year}",
                                payroll.Salary,
                                payroll.NetSalary,
                                Details = payroll.PayrollDetails.Select(d => new { d.Title, d.AmountType, d.Amount })
                            }
                        });

                    case "purchase":
                        var purchase = await _dbContext.Purchases
                            .Include(p => p.Supplier)
                            .Include(p => p.PurchaseDetails).ThenInclude(pd => pd.IngredientItem)
                            .FirstOrDefaultAsync(p => p.Id == id);
                        if (purchase == null) return Json(new { status = "error", message = "Purchase not found" });
                        return Json(new
                        {
                            status = "success",
                            type = "Purchase",
                            data = new
                            {
                                SupplierName = purchase.Supplier?.SupplierName,
                                purchase.InvoiceNo,
                                PurchaseDate = purchase.PurchaseDate.ToString("yyyy-MM-dd HH:mm"),
                                purchase.TotalAmount,
                                purchase.PaidAmount,
                                purchase.DueAmount,
                                Items = purchase.PurchaseDetails.Select(pd => new { pd.IngredientItem.ItemName, pd.Quantity, pd.PurchasePrice, pd.Total })
                            }
                        });

                    case "pos":
                        var order = await _dbContext.Orders
                            .Include(o => o.Customer)
                            .Include(o => o.OrderDetails).ThenInclude(od => od.Item)
                            .Include(o => o.OrderDetails).ThenInclude(od => od.OrderItemModifiers).ThenInclude(m => m.Modifier)
                            .FirstOrDefaultAsync(o => o.Id == id);
                        if (order == null) return Json(new { status = "error", message = "Order not found" });
                        return Json(new
                        {
                            status = "success",
                            type = "POS Order",
                            data = new
                            {
                                CustomerName = order.Customer?.CustomerName,
                                order.OrderType,
                                order.PaymentMethod,
                                OrderDate = order.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                                order.SubTotal,
                                order.TaxTotal,
                                order.DiscountTotal,
                                order.ChargeTotal,
                                order.Total,
                                Items = order.OrderDetails.Select(od => new
                                {
                                    od.Item.ItemName,
                                    od.Quantity,
                                    od.Price,
                                    od.Total,
                                    Modifiers = od.OrderItemModifiers.Select(m => m.Modifier.Title)
                                })
                            }
                        });

                    case "stockadjustment":
                        var adjustment = await _dbContext.StockAdjustments
                            .Include(s => s.IngredientItem)
                            .FirstOrDefaultAsync(s => s.Id == id);
                        if (adjustment == null) return Json(new { status = "error", message = "Adjustment not found" });
                        return Json(new
                        {
                            status = "success",
                            type = "Stock Adjustment",
                            data = new
                            {
                                ItemName = adjustment.IngredientItem?.ItemName,
                                adjustment.Type,
                                adjustment.Quantity,
                                adjustment.Reason,
                                Date = adjustment.EntryDate.ToString("yyyy-MM-dd HH:mm")
                            }
                        });


                    case "partnertransaction":
                        var pTrans = await _dbContext.PartnerTransactions
                            .Include(p => p.Partner)
                            .FirstOrDefaultAsync(p => p.Id == id);
                        if (pTrans == null) return Json(new { status = "error", message = "Partner transaction not found" });
                        return Json(new
                        {
                            status = "success",
                            type = "Partner Equity Transaction",
                            data = new
                            {
                                PartnerName = pTrans.Partner?.Name,
                                pTrans.Type,
                                pTrans.Amount,
                                Note = pTrans.Note,
                                Date = pTrans.EntryDate.ToString("yyyy-MM-dd HH:mm")
                            }
                        });

                    default:
                        return Json(new { status = "error", message = "Unsupported source document type" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message });
            }
        }
        [HttpGet]
        public async Task<IActionResult> StatementOfAccounts(int? accountId, DateTime? startDate, DateTime? endDate, string search)
        {
            var model = new StatementOfAccountsVM
            {
                Accounts = await _dbContext.Set<GLAccount>().Where(a => a.IsActive).OrderBy(a => a.AccountCode).ToListAsync(),
                SelectedAccountId = accountId,
                StartDate = startDate ?? new DateTime(_dateTimeService.Now().Year, _dateTimeService.Now().Month, 1),
                EndDate = endDate ?? _dateTimeService.Now(),
                Search = search
            };

            HashSet<int> matchingPurchaseIds = new HashSet<int>();
            HashSet<int> matchingPayrollIds = new HashSet<int>();
            HashSet<int> matchingOrderIds = new HashSet<int>();

            HashSet<int> matchingInvoiceIds = new HashSet<int>();
            HashSet<int> matchingBillIds = new HashSet<int>();

            // 1. PRE-GATHER ALL MATCHING IDs (Execute these queries only once)
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower().Trim();
                
                // Search Purchases
                var purchases = await _dbContext.Purchases
                    .Include(p => p.Supplier)
                    .Include(p => p.PurchaseDetails).ThenInclude(d => d.IngredientItem)
                    .Where(p =>
                        (p.InvoiceNo != null && p.InvoiceNo.ToLower().Contains(s)) ||
                        (p.Description != null && p.Description.ToLower().Contains(s)) ||
                        (p.Supplier != null && p.Supplier.SupplierName.ToLower().Contains(s)) ||
                        p.PurchaseDetails.Any(d => d.IngredientItem != null && d.IngredientItem.ItemName.ToLower().Contains(s)))
                    .Select(p => p.Id).ToListAsync();
                foreach (var id in purchases) if (id.HasValue) matchingPurchaseIds.Add(id.Value);

                // Search Payroll
                var payrolls = await _dbContext.Payrolls
                    .Include(p => p.Employee)
                    .Where(p => p.Employee != null && p.Employee.Name.ToLower().Contains(s))
                    .Select(p => p.Id).ToListAsync();
                foreach (var id in payrolls) matchingPayrollIds.Add(id);

                // Search Orders
                var orders = await _dbContext.Orders
                    .Include(o => o.OrderDetails).ThenInclude(d => d.Item)
                    .Where(o =>
                        (o.Note != null && o.Note.ToLower().Contains(s)) ||
                        o.OrderDetails.Any(d => d.Item != null && d.Item.ItemName.ToLower().Contains(s)))
                    .Select(o => o.Id).ToListAsync();
                foreach (var id in orders) matchingOrderIds.Add(id);

                // Search Invoices
                var invoiceIds = await (from i in _dbContext.Invoices
                                        join c in _dbContext.Customers on i.CustomerId equals c.Id
                                        where (i.InvoiceNumber != null && i.InvoiceNumber.ToLower().Contains(s)) ||
                                              (c.CustomerName != null && c.CustomerName.ToLower().Contains(s))
                                        select i.Id).ToListAsync();
                foreach (var id in invoiceIds) matchingInvoiceIds.Add(id);

                // Search Bills
                var billIds = await (from b in _dbContext.Bills
                                     join spp in _dbContext.Suppliers on b.SupplierId equals spp.Id
                                     where (b.BillNumber != null && b.BillNumber.ToLower().Contains(s)) ||
                                           (spp.SupplierName != null && spp.SupplierName.ToLower().Contains(s))
                                     select b.Id).ToListAsync();
                foreach (var id in billIds) matchingBillIds.Add(id);
            }

            // 2. FETCH ALL RELEVANT LEDGER DATA
            var ledgerQuery = _dbContext.Set<LedgerEntry>()
                .Include(l => l.JournalEntry)
                .Include(l => l.GLAccount)
                .Where(l => l.JournalEntry.IsPosted);

            if (accountId.HasValue)
            {
                var account = model.Accounts.FirstOrDefault(a => a.Id == accountId.Value);
                if (account != null)
                {
                    // Opening Balance Calculation
                    var openingEntries = await ledgerQuery
                        .Where(l => l.GLAccountId == accountId.Value && l.JournalEntry.EntryDate < model.StartDate)
                        .Select(l => new { l.Debit, l.Credit }).ToListAsync();

                    bool isDebitAccount = account.Category == (int)AccountCategory.Asset || account.Category == (int)AccountCategory.Expense;
                    model.OpeningBalance = isDebitAccount
                        ? openingEntries.Sum(e => e.Debit - e.Credit)
                        : openingEntries.Sum(e => e.Credit - e.Debit);

                    var transactions = await ledgerQuery
                        .Where(l => l.GLAccountId == accountId.Value && l.JournalEntry.EntryDate >= model.StartDate && l.JournalEntry.EntryDate <= model.EndDate)
                        .OrderBy(l => l.JournalEntry.EntryDate).ThenBy(l => l.JournalEntryId).ToListAsync();

                    decimal runningBal = model.OpeningBalance;
                    foreach (var trans in transactions)
                    {
                        var row = MapToRow(trans);

                        // 3. APPLY FILTER IN MEMORY (Instant performance)
                        if (!IsRowMatch(row, search, matchingPurchaseIds, matchingPayrollIds, matchingOrderIds, matchingInvoiceIds, matchingBillIds)) continue;

                        runningBal += isDebitAccount ? (trans.Debit - trans.Credit) : (trans.Credit - trans.Debit);
                        row.RunningBalance = runningBal;
                        model.Transactions.Add(row);
                    }
                    model.ClosingBalance = runningBal;
                }
            }
            else
            {
                // All-Accounts Mode (Day Book view)
                var transactions = await ledgerQuery
                    .Where(l => l.JournalEntry.EntryDate >= model.StartDate && l.JournalEntry.EntryDate <= model.EndDate)
                    .OrderBy(l => l.JournalEntry.EntryDate).ThenBy(l => l.JournalEntryId).ToListAsync();

                foreach (var trans in transactions)
                {
                    var row = MapToRow(trans);
                    if (!IsRowMatch(row, search, matchingPurchaseIds, matchingPayrollIds, matchingOrderIds, matchingInvoiceIds, matchingBillIds)) continue;
                    model.Transactions.Add(row);
                }
            }

            return View(model);
        }

        // Helper: Map LedgerEntry to ViewModel Row
        private StatementOfAccountRow MapToRow(LedgerEntry l) => new StatementOfAccountRow
        {
            JournalEntryId = l.JournalEntryId,
            Date = l.JournalEntry.EntryDate,
            Reference = l.JournalEntry.ReferenceNumber,
            Description = l.Description ?? l.JournalEntry.Description,
            AccountName = l.GLAccount?.AccountName,
            Debit = l.Debit,
            Credit = l.Credit,
            SourceDocumentType = l.JournalEntry.SourceDocumentType,
            SourceDocumentId = l.JournalEntry.SourceDocumentId
        };

        // Helper: Perform fast in-memory search
        private bool IsRowMatch(StatementOfAccountRow row, string search, HashSet<int> pIds, HashSet<int> payIds, HashSet<int> oIds, HashSet<int> iIds, HashSet<int> bIds)
        {
            if (string.IsNullOrWhiteSpace(search)) return true;
            var s = search.ToLower().Trim();

            bool directMatch = (row.Description?.ToLower().Contains(s) ?? false) ||
                               (row.Reference?.ToLower().Contains(s) ?? false) ||
                               (row.AccountName?.ToLower().Contains(s) ?? false);

            // Matches using the pre-collected IDs from sub-entities (Ingredients, Employee Names, Invoices, Bills, etc.)
            bool sourceMatch = (row.SourceDocumentType == "Purchase" && pIds.Contains(row.SourceDocumentId ?? 0)) ||
                               (row.SourceDocumentType == "Payroll" && payIds.Contains(row.SourceDocumentId ?? 0)) ||
                               (row.SourceDocumentType == "POS Order" && oIds.Contains(row.SourceDocumentId ?? 0)) ||
                               (row.SourceDocumentType == "Invoice" && iIds.Contains(row.SourceDocumentId ?? 0)) ||
                               (row.SourceDocumentType == "InvoicePayment" && iIds.Contains(row.SourceDocumentId ?? 0)) ||
                               (row.SourceDocumentType == "Bill" && bIds.Contains(row.SourceDocumentId ?? 0)) ||
                               (row.SourceDocumentType == "BillPayment" && bIds.Contains(row.SourceDocumentId ?? 0));

            return directMatch || sourceMatch;
        }
    }
}
