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
            ILanguageService languageService, ILocalizationService localizationService)
        : base(languageService, localizationService)
        {
            _dbContext = dbContext;
            _accountingEngine = accountingEngine;
        }

        [HttpGet]
        public async Task<IActionResult> ChartOfAccounts()
        {
            // Fetch directly from DB via DbSet or Set<T>
            var accounts = await _dbContext.Set<GLAccount>().ToListAsync();
            return View(accounts);
        }

        [HttpGet]
        public async Task<IActionResult> JournalEntries(DateTime? start, DateTime? end, int? accountId)
        {
            var startDate = start ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var endDate = end ?? DateTime.Today;

            var query = _dbContext.Set<JournalEntry>()
                .Include(j => j.LedgerEntries)
                .ThenInclude(l => l.GLAccount)
                .AsQueryable();

            if (start.HasValue) query = query.Where(j => j.EntryDate >= startDate);
            if (end.HasValue) query = query.Where(j => j.EntryDate <= endDate);
            
            if (accountId.HasValue && accountId > 0)
            {
                query = query.Where(j => j.LedgerEntries.Any(l => l.GLAccountId == accountId));
            }

            var entries = await query.OrderByDescending(j => j.EntryDate).ToListAsync();
            
            ViewBag.Start = startDate.ToString("yyyy-MM-dd");
            ViewBag.End = endDate.ToString("yyyy-MM-dd");
            ViewBag.AccountId = accountId;
            ViewBag.Accounts = await _dbContext.Set<GLAccount>().OrderBy(a => a.AccountCode).ToListAsync();

            return View(entries);
        }

        [HttpGet]
        public async Task<IActionResult> BalanceSheet(DateTime? asOfDate)
        {
            var date = asOfDate ?? DateTime.Today;
            ViewBag.Date = date.ToString("yyyy-MM-dd");
            var report = await _accountingEngine.GenerateBalanceSheetAsync(date);
            return View(report);
        }

        [HttpGet]
        public async Task<IActionResult> ProfitAndLoss(DateTime? start, DateTime? end)
        {
            var startDate = start ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var endDate = end ?? DateTime.Today;
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
                invoice.InvoiceNumber = $"INV-{DateTime.Now:yyyyMMddHHmmss}";
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
                newBill.BillNumber = $"BILL-{DateTime.Now:yyyyMMddHHmmss}";
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
                return Json(new { status = "success", message = "Daily Close Journal generated!" });
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
        public async Task<IActionResult> ManualJournal()
        {
            ViewBag.Accounts = await _dbContext.Set<GLAccount>().Where(a => a.IsActive).ToListAsync();
            return View(new JournalEntry { EntryDate = DateTime.Today });
        }

        [HttpPost]
        public async Task<IActionResult> ManualJournal(JournalEntry entry)
        {
            if (ModelState.IsValid)
            {
                entry.SourceDocumentType = "ManualJournal";
                entry.CreatedAt = DateTime.Now;
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
                EntryDate = entry.EntryDate.ToString("yyyy-MM-dd"),
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
                                PurchaseDate = purchase.PurchaseDate.ToString("yyyy-MM-dd"),
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
                                Date = adjustment.EntryDate.ToString("yyyy-MM-dd")
                            }
                        });

                    case "cashledger":
                        var cash = await _dbContext.CashLedgers
                            .Include(c => c.GLAccount)
                            .FirstOrDefaultAsync(c => c.Id == id);
                        if (cash == null) return Json(new { status = "error", message = "Cash entry not found" });
                        return Json(new
                        {
                            status = "success",
                            type = "Cash Transaction",
                            data = new
                            {
                                offsetAccount = cash.GLAccount?.AccountName,
                                cash.Type,
                                cash.Amount,
                                cash.Description,
                                Date = cash.EntryDate.ToString("yyyy-MM-dd")
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
                                Date = pTrans.EntryDate.ToString("yyyy-MM-dd")
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
        public async Task<IActionResult> StatementOfAccounts(int? accountId, DateTime? startDate, DateTime? endDate)
        {
            var model = new StatementOfAccountsVM
            {
                Accounts = await _dbContext.Set<GLAccount>().Where(a => a.IsActive).ToListAsync(),
                SelectedAccountId = accountId,
                StartDate = startDate ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1),
                EndDate = endDate ?? DateTime.Today
            };

            if (accountId.HasValue)
            {
                var account = await _dbContext.Set<GLAccount>().FirstOrDefaultAsync(a => a.Id == accountId.Value);
                if (account != null)
                {
                    // 1. Calculate Opening Balance
                    var openingEntries = await _dbContext.Set<LedgerEntry>()
                        .Where(l => l.GLAccountId == accountId.Value && l.JournalEntry.EntryDate < model.StartDate && l.JournalEntry.IsPosted)
                        .Select(l => new { l.Debit, l.Credit })
                        .ToListAsync();

                    if (account.Category == (int)AccountCategory.Asset || account.Category == (int)AccountCategory.Expense)
                        model.OpeningBalance = openingEntries.Sum(e => e.Debit - e.Credit);
                    else
                        model.OpeningBalance = openingEntries.Sum(e => e.Credit - e.Debit);

                    // 2. Fetch Transactions in Range
                    var transactions = await _dbContext.Set<LedgerEntry>()
                        .Include(l => l.JournalEntry)
                        .Where(l => l.GLAccountId == accountId.Value && l.JournalEntry.EntryDate >= model.StartDate && l.JournalEntry.EntryDate <= model.EndDate && l.JournalEntry.IsPosted)
                        .OrderBy(l => l.JournalEntry.EntryDate)
                        .ThenBy(l => l.JournalEntryId)
                        .ToListAsync();

                    decimal currentRunningBalance = model.OpeningBalance;
                    foreach (var trans in transactions)
                    {
                        decimal effect = 0;
                        if (account.Category == (int)AccountCategory.Asset || account.Category == (int)AccountCategory.Expense)
                            effect = trans.Debit - trans.Credit;
                        else
                            effect = trans.Credit - trans.Debit;

                        currentRunningBalance += effect;

                        model.Transactions.Add(new StatementOfAccountRow
                        {
                            JournalEntryId = trans.JournalEntryId,
                            Date = trans.JournalEntry.EntryDate,
                            Reference = trans.JournalEntry.ReferenceNumber,
                            Description = trans.Description ?? trans.JournalEntry.Description,
                            Debit = trans.Debit,
                            Credit = trans.Credit,
                            RunningBalance = currentRunningBalance,
                            SourceDocumentType = trans.JournalEntry.SourceDocumentType,
                            SourceDocumentId = trans.JournalEntry.SourceDocumentId
                        });
                    }

                    model.ClosingBalance = currentRunningBalance;
                }
            }

            return View(model);
        }
    }
}
