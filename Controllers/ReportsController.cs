using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Saffrat.Models;
using Saffrat.Models.AccountingEngine;
using Saffrat.ViewModels;
using System.Data;
using Saffrat.Services;

namespace Saffrat.Controllers
{
    public class ReportsController : BaseController
    {
        private readonly ILogger<ReportsController> _logger;
        private readonly RestaurantDBContext _dbContext;

        public ReportsController(ILogger<ReportsController> logger, RestaurantDBContext dbContext,
            ILanguageService languageService, ILocalizationService localizationService, IDateTimeService dateTimeService)
        : base(languageService, localizationService, dateTimeService)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        //Work Periods Report
        [HttpGet]
        [Authorize(Roles = "admin")]
        public IActionResult WorkPeriod(DateTime? start, DateTime? end)
        {
            DateTime from = StartOfDay(start);
            DateTime to = EndOfDay(end);

            var periods = _dbContext.WorkPeriods.OrderByDescending(x => x.Id)
                .Where(x => x.IsEnd.Equals(true) && x.StartedAt >= from && x.EndAt <= to);

            ViewBag.start = from.ToString("yyyy-MM-dd");
            ViewBag.end = to.ToString("yyyy-MM-dd");

            return View(periods);
        }

        [HttpGet]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> GetAccountStatement(int accountId, DateTime start, DateTime end)
        {
            var account = await _dbContext.GLAccounts.FirstOrDefaultAsync(a => a.Id == accountId);
            if (account == null) return NotFound();

            var ledgerEntries = await _dbContext.LedgerEntries
                .Include(l => l.JournalEntry)
                .Include(l => l.GLAccount)
                .Where(l => l.JournalEntry.IsPosted && l.GLAccountId == accountId)
                .ToListAsync();

            // Opening Balance Calculation (all history before 'start')
            var openingEntries = ledgerEntries.Where(l => l.JournalEntry.EntryDate < start).ToList();

            bool isDebitAccount = account.Category == (int)AccountCategory.Asset || account.Category == (int)AccountCategory.Expense;
            decimal openingBalance = isDebitAccount
                ? openingEntries.Sum(e => e.Debit - e.Credit)
                : openingEntries.Sum(e => e.Credit - e.Debit);

            // Transactions within period
            var transactions = ledgerEntries
                .Where(l => l.JournalEntry.EntryDate >= start && l.JournalEntry.EntryDate <= end)
                .OrderBy(l => l.JournalEntry.EntryDate).ThenBy(l => l.JournalEntryId).ToList();

            var journalIds = transactions.Select(t => t.JournalEntryId).Distinct().ToList();
            var allJournalEntries = await _dbContext.LedgerEntries
                .Include(le => le.GLAccount)
                .Where(le => journalIds.Contains(le.JournalEntryId))
                .ToListAsync();

            var model = new StatementOfAccountsVM
            {
                SelectedAccountId = accountId,
                StartDate = start,
                EndDate = end,
                OpeningBalance = openingBalance
            };

            decimal runningBal = openingBalance;
            foreach (var trans in transactions)
            {
                runningBal += isDebitAccount ? (trans.Debit - trans.Credit) : (trans.Credit - trans.Debit);
                
                // Detailed Vision: Resolve Offset Account
                var otherEntries = allJournalEntries
                    .Where(le => le.JournalEntryId == trans.JournalEntryId && le.Id != trans.Id)
                    .ToList();
                
                string offset = "Multiple Accounts";
                if (otherEntries.Count == 1)
                {
                    offset = otherEntries.First().GLAccount?.AccountName ?? "N/A";
                }
                else if (otherEntries.Count == 0)
                {
                    offset = "Self/Adjustment";
                }

                model.Transactions.Add(new StatementOfAccountRow
                {
                    JournalEntryId = trans.JournalEntryId,
                    Date = trans.JournalEntry.EntryDate,
                    Reference = trans.JournalEntry.ReferenceNumber,
                    Description = trans.Description ?? trans.JournalEntry.Description,
                    AccountName = trans.GLAccount?.AccountName,
                    OffsetAccountName = offset,
                    Debit = trans.Debit,
                    Credit = trans.Credit,
                    RunningBalance = runningBal,
                    SourceDocumentType = trans.JournalEntry.SourceDocumentType,
                    SourceDocumentId = trans.JournalEntry.SourceDocumentId
                });
            }
            model.ClosingBalance = runningBal;

            ViewBag.AccountName = account.AccountName;
            ViewBag.AccountCode = account.AccountCode;
            ViewBag.IsDebitAccount = isDebitAccount;

            return PartialView("_AccountStatementPopup", model);
        }

        // Sale Report Daily, Monthly, Yearly
        [HttpGet]
        [Authorize(Roles = "admin")]
        public IActionResult SaleReport(string period, DateTime? start, DateTime? end)
        {
            DateTime from = StartOfDay(start);
            DateTime to = EndOfDay(end);

            IEnumerable<Order> orders = new List<Order>();

            if (period == "yearly")
            {
                orders = _dbContext.Orders
                .Where(x => x.CreatedAt >= from && x.CreatedAt <= to)
                .OrderByDescending(a => a.Id)
                .GroupBy(o => new
                {
                    Year = o.CreatedAt.Year
                })
                .Select(g => new Order
                {
                    SubTotal = g.Sum(x => x.SubTotal),
                    DiscountTotal = g.Sum(x => x.DiscountTotal),
                    ChargeTotal = g.Sum(x => x.ChargeTotal),
                    TaxTotal = g.Sum(x => x.TaxTotal),
                    PaidAmount = g.Sum(x => x.PaidAmount),
                    DueAmount = g.Sum(x => x.DueAmount),
                    Total = g.Sum(x => x.Total),
                    CreatedAt = new DateTime(g.Key.Year, 1, 1)
                })
                .ToList();
            }
            else if (period == "monthly")
            {
                orders = _dbContext.Orders
                .Where(x => x.CreatedAt >= from && x.CreatedAt <= to)
                .OrderByDescending(a => a.Id)
                .GroupBy(o => new
                {
                    o.CreatedAt.Month,
                    o.CreatedAt.Year
                })
                .Select(g => new Order
                {
                    SubTotal = g.Sum(x => x.SubTotal),
                    DiscountTotal = g.Sum(x => x.DiscountTotal),
                    ChargeTotal = g.Sum(x => x.ChargeTotal),
                    TaxTotal = g.Sum(x => x.TaxTotal),
                    PaidAmount = g.Sum(x => x.PaidAmount),
                    DueAmount = g.Sum(x => x.DueAmount),
                    Total = g.Sum(x => x.Total),
                    CreatedAt = new DateTime(g.Key.Year, g.Key.Month, 1)
                })
                .ToList();
            }
            else
            {
                orders = _dbContext.Orders
                .Where(x => x.CreatedAt >= from && x.CreatedAt <= to)
                .OrderByDescending(a => a.Id)
                .GroupBy(o => new
                {
                    o.CreatedAt.Day,
                    o.CreatedAt.Month,
                    o.CreatedAt.Year
                })
                .Select(g => new Order
                {
                    SubTotal = g.Sum(x => x.SubTotal),
                    DiscountTotal = g.Sum(x => x.DiscountTotal),
                    ChargeTotal = g.Sum(x => x.ChargeTotal),
                    TaxTotal = g.Sum(x => x.TaxTotal),
                    PaidAmount = g.Sum(x => x.PaidAmount),
                    DueAmount = g.Sum(x => x.DueAmount),
                    Total = g.Sum(x => x.Total),
                    CreatedAt = new DateTime(g.Key.Year, g.Key.Month, g.Key.Day)
                })
                .ToList();

                period = "daily";
            }

            ViewBag.start = from.ToString("yyyy-MM-dd");
            ViewBag.end = to.ToString("yyyy-MM-dd");
            ViewBag.period = period;

            return View(orders);
        }

        [HttpGet]
        [Authorize(Roles = "admin")]
        public IActionResult SaleSummaryReport(DateTime? start, DateTime? end)
        {
            DateTime from = StartOfDay(start);
            DateTime to = EndOfDay(end);

            ViewBag.start = from.ToString("yyyy-MM-dd");
            ViewBag.end = to.ToString("yyyy-MM-dd");

            var order = _dbContext.Orders.Where(x => x.CreatedAt >= from && x.CreatedAt <= to);
            if (order != null)
            {
                ViewBag.totalOrders = order.Count();
                ViewBag.totalSub = order.Sum(x => x.SubTotal);
                ViewBag.totalAmount = order.Sum(x => x.Total);
                ViewBag.totalCharges = order.Sum(x => x.ChargeTotal);
                ViewBag.totalDiscount = order.Sum(x => x.DiscountTotal);
                ViewBag.totalTax = order.Sum(x => x.TaxTotal);
                ViewBag.totalTaxExclude = ViewBag.totalAmount - ViewBag.totalTax;
            }
            else
            {
                ViewBag.totalSub = 0;
                ViewBag.totalOrders = 0;
                ViewBag.totalAmount = 0;
                ViewBag.totalCharges = 0;
                ViewBag.totalDiscount = 0;
                ViewBag.totalTax = 0;
                ViewBag.totalTaxExclude = 0;
            }

            ViewBag.purchaseTotal = _dbContext.Purchases.Where(x => x.PurchaseDate >= from && x.PurchaseDate <= to).Sum(x => x.TotalAmount);
            ViewBag.purchaseTotal = _dbContext.Purchases.Where(x => x.PurchaseDate >= from && x.PurchaseDate <= to).Sum(x => x.TotalAmount);
            ViewBag.expenseTotal = _dbContext.LedgerEntries.Include(x => x.GLAccount).Include(x => x.JournalEntry)
                .Where(x => (x.GLAccount.Category == (int)AccountCategory.Expense) && x.JournalEntry.EntryDate >= from && x.JournalEntry.EntryDate <= to)
                .Sum(x => Math.Max(0, (decimal)(x.Debit - x.Credit))); // Sum only net debits as expenses

            return View();
        }

        [HttpGet]
        [Authorize(Roles = "admin")]
        public IActionResult SaleDetailedReport(DateTime? start, DateTime? end)
        {
            DateTime from = StartOfDay(start);
            DateTime to = EndOfDay(end);

            ViewBag.start = from.ToString("yyyy-MM-dd");
            ViewBag.end = to.ToString("yyyy-MM-dd");

            var order = _dbContext.Orders.Where(x => x.CreatedAt >= from && x.CreatedAt <= to)
                .Include(x => x.Customer)
                .Include(x => x.OrderDetails)
                .ThenInclude(x => x.Item)
                .Include(x => x.OrderDetails)
                .ThenInclude(x => x.OrderItemModifiers)
                .ThenInclude(x => x.Modifier).ToList();

            return View(order);
        }

        [HttpGet]
        [Authorize(Roles = "admin")]
        public IActionResult ItemSaleReport(DateTime? start, DateTime? end)
        {
            DateTime from = StartOfDay(start);
            DateTime to = EndOfDay(end);

            var orders = _dbContext.OrderDetails
                    .Where(x => x.CreatedAt >= from && x.CreatedAt <= to)
                    .OrderByDescending(a => a.Id)
                    .GroupBy(o => new
                    {
                        o.ItemId
                    })
                    .Select(g => new OrderDetail
                    {
                        Item = g.First().Item,
                        Quantity = g.Sum(x => x.Quantity),
                    })
                    .ToList();

            orders = orders.OrderByDescending(x => x.Quantity).ToList();

            ViewBag.start = from.ToString("yyyy-MM-dd");
            ViewBag.end = to.ToString("yyyy-MM-dd");

            return View(orders);
        }

        // Purchase Report
        [HttpGet]
        [Authorize(Roles = "admin")]
        public IActionResult PurchaseReport(string period, DateTime? start, DateTime? end)
        {
            IEnumerable<Purchase> purchases = new List<Purchase>();

            DateTime from = StartOfDay(start);
            DateTime to = EndOfDay(end);

            if (period == "yearly")
            {
                purchases = _dbContext.Purchases
                .Where(x => x.PurchaseDate >= from && x.PurchaseDate <= to)
                .OrderByDescending(a => a.Id)
                .GroupBy(p => new
                {
                    Year = p.PurchaseDate.Year
                })
                .Select(g => new Purchase
                {
                    TotalAmount = g.Sum(x => x.TotalAmount),
                    PaidAmount = g.Sum(x => x.PaidAmount),
                    DueAmount = g.Sum(x => x.DueAmount),
                    PurchaseDate = new DateTime(g.Key.Year, 1, 1)
                })
                .ToList();
            }
            else if (period == "monthly")
            {
                purchases = _dbContext.Purchases
                .Where(x => x.PurchaseDate >= from && x.PurchaseDate <= to)
                .OrderByDescending(a => a.Id)
                .GroupBy(p => new
                {
                    p.PurchaseDate.Month,
                    p.PurchaseDate.Year
                })
                .Select(g => new Purchase
                {
                    TotalAmount = g.Sum(x => x.TotalAmount),
                    PaidAmount = g.Sum(x => x.PaidAmount),
                    DueAmount = g.Sum(x => x.DueAmount),
                    PurchaseDate = new DateTime(g.Key.Year, g.Key.Month, 1)
                })
                .ToList();
            }
            else
            {
                purchases = _dbContext.Purchases
                .Where(x => x.PurchaseDate >= from && x.PurchaseDate <= to)
                .OrderByDescending(a => a.Id)
                .GroupBy(p => new
                {
                    p.PurchaseDate.Day,
                    p.PurchaseDate.Month,
                    p.PurchaseDate.Year
                })
                .Select(g => new Purchase
                {
                    TotalAmount = g.Sum(x => x.TotalAmount),
                    PaidAmount = g.Sum(x => x.PaidAmount),
                    DueAmount = g.Sum(x => x.DueAmount),
                    PurchaseDate = new DateTime(g.Key.Year, g.Key.Month, g.Key.Day)
                })
                .ToList();

                period = "daily";
            }

            ViewBag.start = from.ToString("yyyy-MM-dd");
            ViewBag.end = to.ToString("yyyy-MM-dd");
            ViewBag.period = period;

            return View(purchases);
        }



        // Stock Alert Report
        [HttpGet]
        [Authorize(Roles = "admin")]
        public IActionResult StockAlert()
        {
            var ingredients = _dbContext.IngredientItems
                .Where(x => x.Quantity <= x.AlertQuantity)
                .ToList();

            return View(ingredients);
        }

        // Customer Due Report
        [HttpGet]
        [Authorize(Roles = "admin")]
        public IActionResult CustomerDue(DateTime? start, DateTime? end)
        {
            IEnumerable<Order> orders = new List<Order>();

            DateTime from = StartOfDay(start);
            DateTime to = EndOfDay(end);

            orders = _dbContext.Orders
                .Where(x => x.CreatedAt >= from && x.CreatedAt <= to)
                .OrderByDescending(a => a.Id)
                .GroupBy(t => t.CustomerId)
                .Select(g => new Order
                {
                    Customer = g.First().Customer,
                    DueAmount = g.Sum(x => x.DueAmount),
                })
                .ToList();

            ViewBag.start = from.ToString("yyyy-MM-dd");
            ViewBag.end = to.ToString("yyyy-MM-dd");

            return View(orders);
        }

        // Supplier Due Report
        [HttpGet]
        [Authorize(Roles = "admin")]
        public IActionResult SupplierDue(DateTime? start, DateTime? end)
        {
            IEnumerable<Purchase> purchases = new List<Purchase>();

            DateTime from = StartOfDay(start);
            DateTime to = EndOfDay(end);

            purchases = _dbContext.Purchases
                .Where(x => x.PurchaseDate >= from && x.PurchaseDate <= to)
                .OrderByDescending(a => a.Id)
                .GroupBy(t => t.SupplierId)
                .Select(g => new Purchase
                {
                    Supplier = g.First().Supplier,
                    DueAmount = g.Sum(x => x.DueAmount),
                })
                .ToList();

            ViewBag.start = from.ToString("yyyy-MM-dd");
            ViewBag.end = to.ToString("yyyy-MM-dd");

            return View(purchases);
        }

        // Attendance Report
        [HttpGet]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> AttendanceReport(int? employee, DateTime? start, DateTime? end)
        {
            DateTime from = StartOfDay(start);
            DateTime to = EndOfDay(end);

            var attendances = await _dbContext.Attendances.OrderByDescending(x => x.Id)
                .Where(x => x.EmployeeId == employee && x.AttendaceDate >= from && x.AttendaceDate <= to)
                .Include(x => x.Shift)
                .Include(x => x.Employee)
                .ThenInclude(x => x.Department)
                .Include(x => x.Employee)
                .ThenInclude(x => x.Designation).ToListAsync();

            ViewBag.start = from.ToString("yyyy-MM-dd");
            ViewBag.end = to.ToString("yyyy-MM-dd");
            ViewBag.employees = GetEmployees();
            ViewBag.employee = employee;

            return View(attendances);
        }

        [HttpGet]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> TrialBalance(DateTime? start, DateTime? end)
        {
            var from = StartOfDay(start);
            var to = EndOfDay(end);

            ViewBag.start = from.ToString("yyyy-MM-dd");
            ViewBag.end = to.ToString("yyyy-MM-dd");

            var accounts = await _dbContext.GLAccounts.ToListAsync();
            
            // All posted entries for calculations
            var allPostedEntries = await _dbContext.LedgerEntries
                .Include(x => x.JournalEntry)
                .Where(x => x.JournalEntry.IsPosted)
                .ToListAsync();

            var reportData = new List<Saffrat.ViewModels.TrialBalanceModel>();

            foreach (var account in accounts)
            {
                var history = allPostedEntries.Where(x => x.GLAccountId == account.Id).ToList();
                
                // Normal Balance Logic: Assets (0) and Expenses (4) are Debit-normal
                bool isDebitNormal = (account.Category == 0 || account.Category == 4);

                // Opening Balance (before 'from')
                var openingEntries = history.Where(x => x.JournalEntry.EntryDate < from).ToList();
                decimal openingBalance = isDebitNormal
                    ? openingEntries.Sum(x => x.Debit - x.Credit)
                    : openingEntries.Sum(x => x.Credit - x.Debit);

                // Period transactions
                var periodEntries = history.Where(x => x.JournalEntry.EntryDate >= from && x.JournalEntry.EntryDate <= to).ToList();
                decimal periodDebit = periodEntries.Sum(x => x.Debit);
                decimal periodCredit = periodEntries.Sum(x => x.Credit);

                // Closing Balance
                decimal closingBalance = openingBalance + (isDebitNormal ? (periodDebit - periodCredit) : (periodCredit - periodDebit));

                // Only show if there's any activity or balance
                if (openingBalance != 0 || periodDebit != 0 || periodCredit != 0)
                {
                    reportData.Add(new Saffrat.ViewModels.TrialBalanceModel
                    {
                        AccountId = Convert.ToInt32(account.Id),
                        AccountName = account.AccountName,
                        AccountGroup = ((AccountCategory)account.Category).ToString(),
                        Category = account.Category,
                        OpeningBalance = openingBalance,
                        TotalDebit = periodDebit,
                        TotalCredit = periodCredit,
                        ClosingBalance = closingBalance
                    });
                }
            }

            return View(reportData);
        }

        [HttpGet]
        [Authorize(Roles = "admin")]
        [HttpGet]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> PurchaseComparison(string search)
        {
            var query = _dbContext.PurchaseDetails
                .Include(x => x.Purchase)
                    .ThenInclude(p => p.Supplier)
                .Include(x => x.IngredientItem)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower();
                query = query.Where(x => x.IngredientItem.ItemName.ToLower().Contains(s));
            }

            var purchaseDetails = await query.ToListAsync();

            var ingredients = await _dbContext.IngredientItems
                .ToListAsync();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower();
                ingredients = ingredients.Where(x => x.ItemName.ToLower().Contains(s)).ToList();
            }

            var viewModel = new PurchaseComparisonVM
            {
                Search = search
            };

            foreach (var ingredient in ingredients)
            {
                var last5 = purchaseDetails
                    .Where(x => x.IngredientItemId == ingredient.Id)
                    .OrderByDescending(x => x.Purchase.PurchaseDate)
                    .Take(5)
                    .Select(x => new LastPurchaseInfo
                    {
                        Date = x.Purchase.PurchaseDate,
                        VendorName = x.Purchase.Supplier?.SupplierName ?? "N/A",
                        Qty = x.Quantity,
                        Price = x.PurchasePrice,
                        Total = x.Total
                    }).ToList();

                viewModel.Items.Add(new PurchaseComparisonItem
                {
                    IngredientId = (int)ingredient.Id,
                    IngredientName = ingredient.ItemName,
                    Unit = ingredient.Unit,
                    LastPurchases = last5
                });
            }

            return View(viewModel);
        }

        [HttpGet]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> CashBook(DateTime? date)
        {
            var reportDate = date ?? CurrentDateTime();
            var from = StartOfDay(reportDate);
            var to = EndOfDay(reportDate);

            // Cash accounts: identified by IsCash flag
            var cashAccounts = await _dbContext.GLAccounts
                .Where(x => x.IsActive && x.IsCash)
                .ToListAsync();

            var accountIds = cashAccounts.Select(x => x.Id).ToList();

            var viewModel = await GenerateBankBookVM(reportDate, accountIds, "Cash Book", string.Join(", ", cashAccounts.Select(x => x.AccountName)));

            ViewBag.date = reportDate.ToString("yyyy-MM-dd");
            return View(viewModel);
        }

        [HttpGet]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> DayBook(DateTime? date)
        {
            var reportDate = date ?? CurrentDateTime();
            var from = StartOfDay(reportDate);
            var to = EndOfDay(reportDate);

            // Day Book: Cash and Bank accounts
            var dayBookAccounts = await _dbContext.GLAccounts
                .Where(x => x.IsActive && (x.IsCash || x.IsBank))
                .ToListAsync();

            var accountIds = dayBookAccounts.Select(x => x.Id).ToList();

            var viewModel = await GenerateBankBookVM(reportDate, accountIds, "Day Book", string.Join(", ", dayBookAccounts.Select(x => x.AccountName)));

            ViewBag.date = reportDate.ToString("yyyy-MM-dd");
            return View(viewModel);
        }

        private async Task<BankBookVM> GenerateBankBookVM(DateTime reportDate, List<int> accountIds, string type, string accountNames)
        {
            var startOfToday = StartOfDay(reportDate);
            var endOfToday = EndOfDay(reportDate);

            // Opening Balance: All history before today
            var openingEntries = await _dbContext.LedgerEntries
                .Include(l => l.JournalEntry)
                .Where(l => accountIds.Contains(l.GLAccountId) && l.JournalEntry.EntryDate < startOfToday)
                .ToListAsync();

            decimal openingBalance = openingEntries.Sum(l => l.Debit - l.Credit);

            // Today's Transactions
            var todayEntries = await _dbContext.LedgerEntries
                .Include(l => l.JournalEntry)
                .Include(l => l.GLAccount)
                .Where(l => accountIds.Contains(l.GLAccountId) && l.JournalEntry.EntryDate >= startOfToday && l.JournalEntry.EntryDate <= endOfToday)
                .OrderBy(l => l.JournalEntry.CreatedAt)
                .ToListAsync();

            var transactions = todayEntries.Select(e => new BankBookEntry
            {
                Date = e.JournalEntry.EntryDate,
                Reference = e.JournalEntry.ReferenceNumber,
                AccountName = e.GLAccount.AccountName,
                Description = e.Description,
                Inflow = e.Debit,
                Outflow = e.Credit,
                SourceDocumentType = e.JournalEntry.SourceDocumentType,
                SourceDocumentId = e.JournalEntry.SourceDocumentId
            }).ToList();

            return new BankBookVM
            {
                Date = reportDate,
                OpeningBalance = openingBalance,
                Transactions = transactions,
                ClosingBalance = openingBalance + transactions.Sum(t => t.Inflow - t.Outflow),
                ReportType = type,
                AccountNames = accountNames
            };
        }

        private Dictionary<int, string> GetEmployees()
        {
            Dictionary<int, string> employees = _dbContext.Employees.Where(x => x.Status.Equals(true))
                .Select(t => new
                {
                    t.Id,
                    t.Name,
                }).ToDictionary(t => Convert.ToInt32(t.Id), t => String.Format("{0} (EMP-{1})", t.Name, t.Id));
            return employees;
        }
    }
}