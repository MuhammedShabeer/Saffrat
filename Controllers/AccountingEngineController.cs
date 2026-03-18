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
        public async Task<IActionResult> JournalEntries(DateTime? start, DateTime? end, int? accountId, string search)
        {
            var startDate = start ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var endDate = end ?? DateTime.Today;

            // 1. Initialize HashSet for faster lookups and to avoid duplicate IDs
            var matchingPurchaseIds = new HashSet<int>();
            var matchingOrderIds = new HashSet<int>();
            var matchingPayrollIds = new HashSet<int>();

            var query = _dbContext.Set<JournalEntry>()
                .Include(j => j.LedgerEntries)
                .ThenInclude(l => l.GLAccount)
                .AsQueryable();

            // 2. Standard Date and Account Filtering
            query = query.Where(j => j.EntryDate >= startDate && j.EntryDate <= endDate);

            if (accountId.HasValue && accountId > 0)
            {
                query = query.Where(j => j.LedgerEntries.Any(l => l.GLAccountId == accountId));
            }

            // 3. Complex Search Logic
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower().Trim();

                // Search Ingredients & Purchases
                var purchases = await _dbContext.Purchases
                    .Include(p => p.Supplier)
                    .Include(p => p.PurchaseDetails).ThenInclude(d => d.IngredientItem)
                    .Where(p =>
                        (p.InvoiceNo != null && p.InvoiceNo.ToLower().Contains(s)) ||
                        (p.Description != null && p.Description.ToLower().Contains(s)) ||
                        (p.Supplier != null && p.Supplier.SupplierName.ToLower().Contains(s)) ||
                        p.PurchaseDetails.Any(d => d.IngredientItem != null && d.IngredientItem.ItemName.ToLower().Contains(s)))
                    .Select(p => p.Id)
                    .ToListAsync();

                foreach (var id in purchases) matchingPurchaseIds.Add(id ?? 0);

                // Search Payroll (Matches D-NIZAR, MD NUR RASUL, etc.)
                var payrolls = await _dbContext.Payrolls
                    .Include(p => p.Employee)
                    .Where(p => p.Employee != null && p.Employee.Name.ToLower().Contains(s))
                    .Select(p => p.Id)
                    .ToListAsync();

                foreach (var id in payrolls) matchingPayrollIds.Add(id);

                // Search POS Orders (Matches Samosa types/Notes)
                var orders = await _dbContext.Orders
                    .Include(o => o.OrderDetails).ThenInclude(d => d.Item)
                    .Where(o =>
                        (o.Note != null && o.Note.ToLower().Contains(s)) ||
                        o.OrderDetails.Any(d => d.Item != null && d.Item.ItemName.ToLower().Contains(s)))
                    .Select(o => o.Id)
                    .ToListAsync();

                foreach (var id in orders) matchingOrderIds.Add(id);

                // 4. APPLY the filters to the main Journal query
                // Assuming JournalEntry has SourceId and SourceDoc properties
                query = query.Where(j =>
                    (j.Description != null && j.Description.ToLower().Contains(s)) ||
                    (j.ReferenceNumber != null && j.ReferenceNumber.ToLower().Contains(s)) ||
                    (j.SourceDoc == "purchase" && matchingPurchaseIds.Contains(j.SourceId ?? 0)) ||
                    (j.SourceDoc == "pos" && matchingOrderIds.Contains(j.SourceId ?? 0)) ||
                    (j.SourceDoc == "payroll" && matchingPayrollIds.Contains(j.SourceId ?? 0)
                    )
                );
            }

            var entries = await query.OrderByDescending(j => j.EntryDate).ToListAsync();

            // 5. Populate ViewBags for the UI
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
        public async Task<IActionResult> StatementOfAccounts(int? accountId, DateTime? startDate, DateTime? endDate, string search)
        {
            var model = new StatementOfAccountsVM
            {
                Accounts = await _dbContext.Set<GLAccount>().Where(a => a.IsActive).OrderBy(a => a.AccountCode).ToListAsync(),
                SelectedAccountId = accountId,
                StartDate = startDate ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1),
                EndDate = endDate ?? DateTime.Today,
                Search = search
            };

            // Build cross-entity search sets if a search term is provided (shared for both modes)
            HashSet<int> matchingPurchaseIds = new HashSet<int>();
            HashSet<int> matchingPayrollIds = new HashSet<int>();
            HashSet<int> matchingOrderIds = new HashSet<int>();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower().Trim();

                // Ingredient search for purchases
                var purchaseIdsByIngredient = await _dbContext.PurchaseDetails
                    .Include(pd => pd.IngredientItem)
                    .Where(pd => pd.IngredientItem != null && pd.IngredientItem.ItemName.ToLower().Contains(s))
                    .Select(pd => pd.PurchaseId)
                    .ToListAsync();

                // Food item search for POS Orders
                var orderIdsByFoodItem = await _dbContext.OrderDetails
                    .Include(od => od.Item)
                    .Where(od => od.Item != null && od.Item.ItemName.ToLower().Contains(s))
                    .Select(od => od.OrderId)
                    .ToListAsync();

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
                foreach (var id in purchaseIds) if (id.HasValue) matchingPurchaseIds.Add(id.Value);
                foreach (var id in purchaseIdsByIngredient) if (id != 0) matchingPurchaseIds.Add(id);

                var payrollIds = await _dbContext.Payrolls
                    .Include(p => p.Employee)
                    .Where(p => p.Employee != null && p.Employee.Name.ToLower().Contains(s))
                    .Select(p => p.Id)
                    .ToListAsync();
                foreach (var id in payrollIds) matchingPayrollIds.Add(id);

                var orderIds = await _dbContext.Orders
                    .Include(o => o.OrderDetails).ThenInclude(d => d.Item)
                    .Where(o =>
                        (o.Note != null && o.Note.ToLower().Contains(s)) ||
                        o.OrderDetails.Any(d => d.Item != null && d.Item.ItemName.ToLower().Contains(s)))
                    .Select(o => o.Id)
                    .ToListAsync();
                foreach (var id in orderIds) matchingOrderIds.Add(id);
                foreach (var id in orderIdsByFoodItem) if (id.HasValue && id.Value != 0) matchingOrderIds.Add(id.Value);
            }

            if (accountId.HasValue)
            {
                // Single-account mode: show running balance
                var account = await _dbContext.Set<GLAccount>().FirstOrDefaultAsync(a => a.Id == accountId.Value);
                if (account != null)
                {
                    var openingEntries = await _dbContext.Set<LedgerEntry>()
                        .Where(l => l.GLAccountId == accountId.Value && l.JournalEntry.EntryDate < model.StartDate && l.JournalEntry.IsPosted)
                        .Select(l => new { l.Debit, l.Credit })
                        .ToListAsync();

                    if (account.Category == (int)AccountCategory.Asset || account.Category == (int)AccountCategory.Expense)
                        model.OpeningBalance = openingEntries.Sum(e => e.Debit - e.Credit);
                    else
                        model.OpeningBalance = openingEntries.Sum(e => e.Credit - e.Debit);

                    var transactions = await _dbContext.Set<LedgerEntry>()
                        .Include(l => l.JournalEntry)
                        .Include(l => l.GLAccount)
                        .Where(l => l.GLAccountId == accountId.Value && l.JournalEntry.EntryDate >= model.StartDate && l.JournalEntry.EntryDate <= model.EndDate && l.JournalEntry.IsPosted)
                        .OrderBy(l => l.JournalEntry.EntryDate)
                        .ThenBy(l => l.JournalEntryId)
                        .ToListAsync();

                    decimal currentRunningBalance = model.OpeningBalance;
                    foreach (var trans in transactions)
                    {
                        decimal effect = (account.Category == (int)AccountCategory.Asset || account.Category == (int)AccountCategory.Expense)
                            ? trans.Debit - trans.Credit
                            : trans.Credit - trans.Debit;
                        currentRunningBalance += effect;

                        var row = new StatementOfAccountRow
                        {
                            JournalEntryId = trans.JournalEntryId,
                            Date = trans.JournalEntry.EntryDate,
                            Reference = trans.JournalEntry.ReferenceNumber,
                            Description = trans.GLAccount?.AccountName,
                            AccountName = trans.GLAccount?.AccountName,
                            Debit = trans.Debit,
                            Credit = trans.Credit,
                            RunningBalance = currentRunningBalance,
                            SourceDocumentType = trans.JournalEntry.SourceDocumentType,
                            SourceDocumentId = trans.JournalEntry.SourceDocumentId
                        };

                        if (!string.IsNullOrWhiteSpace(search))
                        {
                            var s = search.ToLower().Trim();
                            bool directMatch = (row.Description != null && row.Description.ToLower().Contains(s)) ||
                                              (row.Reference != null && row.Reference.ToLower().Contains(s)) ||
                                              (row.SourceDocumentType != null && row.SourceDocumentType.ToLower().Contains(s));
                            bool sourceMatch =
                                (row.SourceDocumentType == "Purchase" && row.SourceDocumentId.HasValue && matchingPurchaseIds.Contains(row.SourceDocumentId.Value)) ||
                                (row.SourceDocumentType == "Payroll" && row.SourceDocumentId.HasValue && matchingPayrollIds.Contains(row.SourceDocumentId.Value)) ||
                                (row.SourceDocumentType == "POS Order" && row.SourceDocumentId.HasValue && matchingOrderIds.Contains(row.SourceDocumentId.Value));
                            if (!directMatch && !sourceMatch) continue;
                        }

                        model.Transactions.Add(row);
                    }

                    model.ClosingBalance = currentRunningBalance;
                }
            }
            else
            {
                // All-accounts mode: show all ledger lines in date range
                var transactions = await _dbContext.Set<LedgerEntry>()
                    .Include(l => l.JournalEntry)
                    .Include(l => l.GLAccount)
                    .Where(l => l.JournalEntry.EntryDate >= model.StartDate && l.JournalEntry.EntryDate <= model.EndDate && l.JournalEntry.IsPosted)
                    .OrderBy(l => l.JournalEntry.EntryDate)
                    .ThenBy(l => l.JournalEntryId)
                    .ToListAsync();

                foreach (var trans in transactions)
                {
                    var row = new StatementOfAccountRow
                    {
                        JournalEntryId = trans.JournalEntryId,
                        Date = trans.JournalEntry.EntryDate,
                        Reference = trans.JournalEntry.ReferenceNumber,
                        Description = trans.GLAccount?.AccountName,
                        AccountName = trans.GLAccount?.AccountName,
                        Debit = trans.Debit,
                        Credit = trans.Credit,
                        RunningBalance = 0, // Not meaningful without a specific account
                        SourceDocumentType = trans.JournalEntry.SourceDocumentType,
                        SourceDocumentId = trans.JournalEntry.SourceDocumentId
                    };

                    if (!string.IsNullOrWhiteSpace(search))
                    {
                        var s = search.ToLower().Trim();
                        bool directMatch = (row.Description != null && row.Description.ToLower().Contains(s)) ||
                                          (row.Reference != null && row.Reference.ToLower().Contains(s)) ||
                                          (row.SourceDocumentType != null && row.SourceDocumentType.ToLower().Contains(s)) ||
                                          (row.AccountName != null && row.AccountName.ToLower().Contains(s));
                        bool sourceMatch =
                            (row.SourceDocumentType == "Purchase" && row.SourceDocumentId.HasValue && matchingPurchaseIds.Contains(row.SourceDocumentId.Value)) ||
                            (row.SourceDocumentType == "Payroll" && row.SourceDocumentId.HasValue && matchingPayrollIds.Contains(row.SourceDocumentId.Value)) ||
                            (row.SourceDocumentType == "POS Order" && row.SourceDocumentId.HasValue && matchingOrderIds.Contains(row.SourceDocumentId.Value));

                        // Ingredient/food item search for purchases
                        bool ingredientMatch = false;
                        if (!directMatch && !sourceMatch && row.SourceDocumentType == "Purchase" && row.SourceDocumentId.HasValue)
                        {
                            var purchase = await _dbContext.Purchases
                                .Include(p => p.PurchaseDetails).ThenInclude(d => d.IngredientItem)
                                .FirstOrDefaultAsync(p => p.Id == row.SourceDocumentId.Value);
                            if (purchase != null && purchase.PurchaseDetails.Any(d => d.IngredientItem != null && d.IngredientItem.ItemName.ToLower().Contains(s)))
                                ingredientMatch = true;
                        }
                        // Food item search for POS Orders
                        if (!directMatch && !sourceMatch && row.SourceDocumentType == "POS Order" && row.SourceDocumentId.HasValue)
                        {
                            var order = await _dbContext.Orders
                                .Include(o => o.OrderDetails).ThenInclude(d => d.Item)
                                .FirstOrDefaultAsync(o => o.Id == row.SourceDocumentId.Value);
                            if (order != null && order.OrderDetails.Any(d => d.Item != null && d.Item.ItemName.ToLower().Contains(s)))
                                ingredientMatch = true;
                        }
                        if (!directMatch && !sourceMatch && !ingredientMatch) continue;
                    }

                    model.Transactions.Add(row);
                }

                model.OpeningBalance = 0;
                model.ClosingBalance = model.Transactions.Sum(t => t.Debit) - model.Transactions.Sum(t => t.Credit);
            }

            return View(model);
        }
    }
}
