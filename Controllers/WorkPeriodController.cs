using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Saffrat.Models;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Saffrat.Services;
using Microsoft.EntityFrameworkCore;
using Saffrat.ViewModels;
using System.Globalization;
using Saffrat.Services.AccountingEngine;

namespace Saffrat.Controllers
{
    // Controller for managing work periods
    public class WorkPeriodController : BaseController
    {
        private readonly ILogger<WorkPeriodController> _logger;
        private readonly RestaurantDBContext _dbContext;
        private readonly IAccountingEngine _accountingEngine;

        // Constructor for WorkPeriodController
        public WorkPeriodController(
            ILogger<WorkPeriodController> logger,
            RestaurantDBContext dbContext,
            IAccountingEngine accountingEngine,
            ILanguageService languageService,
            ILocalizationService localizationService,
            IDateTimeService dateTimeService)
            : base(languageService, localizationService, dateTimeService)
        {
            _logger = logger;
            _dbContext = dbContext;
            _accountingEngine = accountingEngine;
        }

        // Action for displaying work period information
        [HttpGet]
        [Authorize(Roles = "admin,staff")]
        public IActionResult Index()
        {
            var workPeriod = _dbContext.WorkPeriods.OrderByDescending(x => x.Id).FirstOrDefault();
            return View(workPeriod);
        }

        // Action to start a new work period
        [HttpPost]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> StartWorkPeriod([Required] decimal OpeningBalance)
        {
            var response = new Dictionary<string, string>();
            try
            {
                var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (ModelState.IsValid)
                {
                    if (!IsWorkPeriodStarted())
                    {
                        // Create a new work period
                        WorkPeriod workPeriod = new()
                        {
                            OpeningBalance = OpeningBalance,
                            StartedBy = userName,
                            ClosingBalance = 0,
                            EndBy = userName,
                            IsEnd = false,
                            StartedAt = CurrentDateTime(),
                            EndAt = CurrentDateTime()
                        };

                        _dbContext.WorkPeriods.Add(workPeriod);
                        await _dbContext.SaveChangesAsync();

                        response.Add("status", "success");
                        response.Add("message", "success");
                    }
                    else
                    {
                        response.Add("status", "error");
                        response.Add("message", "Work Period already started.");
                    }
                }
                else
                {
                    response.Add("status", "error");
                    response.Add("message", "Enter required fields.");
                }
            }
            catch
            {
                response.Add("status", "error");
                response.Add("message", "Something went wrong.");
            }

            return Json(response);
        }

        // Action to end the current work period
        [HttpPost]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> EndWorkPeriod()
        {
            var response = new Dictionary<string, string>();
            try
            {
                var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var workPeriod = _dbContext.WorkPeriods.FirstOrDefault(x => x.IsEnd.Equals(false));

                if (workPeriod != null)
                {
                    // Check if there are opened orders before ending the work period
                    var openedOrders = _dbContext.RunningOrders.Where(x => x.Status > 0 && x.Status < 5).Count();

                    if (openedOrders > 0)
                    {
                        response.Add("status", "error");
                        response.Add("message", "You can't end work period because you need to settle all opened orders.");
                    }
                    else
                    {
                        // 1. ALL Cash collected from ALL orders (POS + Van Sales)
                        // Formula: (Total amount - Unpaid Due amount) = Cash actually in hand
                        var cashSales = _dbContext.Orders
                            .Where(x => x.CreatedAt >= workPeriod.StartedAt)
                            .Sum(x => (decimal?)(x.PaidAmount)) ?? 0;
                        
                        // We must subtract Card payments since they aren't physical cash in the drawer
                        var cardPayments = _dbContext.Orders
                            .Where(x => x.CreatedAt >= workPeriod.StartedAt && x.PaymentMethod != "Cash" && x.PriceType != "VanSale")
                            .Sum(x => (decimal?)x.PaidAmount) ?? 0;

                        decimal netOrderCash = cashSales - cardPayments;

                        // 2. Net Cash Movements from Ledger (Expenses, Manual Journals, etc.)
                        var cashAccountIds = _dbContext.GLAccounts.Where(x => x.IsCash).Select(x => x.Id).ToList();
                        var cashMovements = _dbContext.LedgerEntries
                            .Include(x => x.JournalEntry)
                            .Where(x => x.JournalEntry.EntryDate >= workPeriod.StartedAt 
                                        && x.JournalEntry.SourceDocumentType != "DailyClose"
                                        && x.JournalEntry.SourceDocumentType != "pos"
                                        && cashAccountIds.Contains(x.GLAccountId))
                            .ToList();
                        
                        decimal netMovement = 0;
                        foreach (var m in cashMovements)
                        {
                            netMovement += (m.Debit - m.Credit);
                        }

                        // Final Expected Balance: Opening + Net Order Cash + Other Ledger Movements
                        workPeriod.ClosingBalance = workPeriod.OpeningBalance + netOrderCash + netMovement;
                        workPeriod.IsEnd = true;
                        workPeriod.EndAt = CurrentDateTime();
                        workPeriod.EndBy = userName;

                        _dbContext.WorkPeriods.Update(workPeriod);
                        await _dbContext.SaveChangesAsync();

                        // [NEW] Automation: Perform Daily Close using StartedAt to classify late night closures correctly
                        await _accountingEngine.PerformDailyCloseAsync(workPeriod.StartedAt);

                        response.Add("status", "success");
                        response.Add("message", "success");
                        response.Add("id", workPeriod.Id.ToString());
                    }
                }    
                else
                {
                    response.Add("status", "error");
                    response.Add("message", "Enter required fields.");
                }
            }
            catch
            {
                response.Add("status", "error");
                response.Add("message", "Something went wrong.");
            }

            return Json(response);
        }

        // Check if a work period has already started
        private bool IsWorkPeriodStarted()
        {
            var workPeriod = _dbContext.WorkPeriods.Where(x => x.IsEnd.Equals(false)).Count();
            return workPeriod > 0;
        }

        [HttpGet]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> PrintWorkPeriodReport(int? Id)
        {
            var workPeriod = await _dbContext.WorkPeriods.FirstOrDefaultAsync(x => x.Id == Id);
            if (workPeriod == null) return NotFound();

            var start = workPeriod.StartedAt;
            var end = workPeriod.EndAt ?? CurrentDateTime();

            var orders = await _dbContext.Orders
                .Where(x => x.CreatedAt >= start && x.CreatedAt <= end)
                .ToListAsync();

            var purchases = await _dbContext.Purchases
                .Where(x => x.PurchaseDate >= start && x.PurchaseDate <= end)
                .ToListAsync();

            var expenses = await _dbContext.LedgerEntries
                .Include(x => x.GLAccount)
                .Include(x => x.JournalEntry)
                .Where(x => x.GLAccount.Category == (int)Saffrat.Models.AccountingEngine.AccountCategory.Expense 
                            && x.JournalEntry.EntryDate >= start && x.JournalEntry.EntryDate <= end)
                .ToListAsync();

            var model = new WorkPeriodSummaryVM
            {
                Id = Convert.ToInt32(workPeriod.Id),
                StartedAt = workPeriod.StartedAt,
                EndAt = workPeriod.EndAt,
                StartedBy = workPeriod.StartedBy,
                EndBy = workPeriod.EndBy,
                OpeningBalance = workPeriod.OpeningBalance,
                ClosingBalance = workPeriod.ClosingBalance ?? 0,
                POSSalesTotal = orders.Where(x => x.PriceType != "VanSale").Sum(x => x.Total),
                VanSalesTotal = orders.Where(x => x.PriceType == "VanSale").Sum(x => x.Total),
                TotalSales = orders.Sum(x => x.Total),
                DueAmountTotal = orders.Sum(x => x.DueAmount),
                POSDueAmount = orders.Where(x => x.PriceType != "VanSale").Sum(x => x.DueAmount),
                VanDueAmount = orders.Where(x => x.PriceType == "VanSale").Sum(x => x.DueAmount),
                PaidAmountTotal = orders.Sum(x => x.PaidAmount),
                PurchasesTotal = purchases.Sum(x => x.TotalAmount),
                ExpensesTotal = expenses.Sum(x => Math.Max(0, (decimal)(x.Debit - x.Credit))),
                ChargesTotal = orders.Sum(x => x.ChargeTotal),
                TaxTotal = orders.Sum(x => x.TaxTotal),
                DiscountTotal = orders.Sum(x => x.DiscountTotal)
            };

            // 1. Calculate Old Debt Collected (Collections from Previous Bills during this period)
            model.OldDebtCollected = await _dbContext.JournalEntries
                .Include(j => j.LedgerEntries)
                .Where(j => j.EntryDate >= start && j.EntryDate <= end && j.SourceDocumentType == "CustomerCollection")
                .SelectMany(j => j.LedgerEntries)
                .Where(le => _dbContext.GLAccounts.Any(ac => ac.Id == le.GLAccountId && ac.IsCash))
                .SumAsync(le => (decimal?)(le.Debit - le.Credit)) ?? 0;

            // 2. Calculate Category Sales (BBQ, Biriyani, etc.)
            var orderIds = orders.Select(o => o.Id).ToList();
            var categoryData = await _dbContext.OrderDetails
                .Include(od => od.Item)
                .ThenInclude(fi => fi.Group)
                .Where(od => od.OrderId.HasValue && orderIds.Contains(od.OrderId.Value))
                .ToListAsync();

            model.CategorySales = categoryData
                .Where(od => od.Item?.Group != null)
                .GroupBy(od => od.Item.Group.GroupName)
                .ToDictionary(g => g.Key, g => g.Sum(od => od.Total ?? 0));

            // 4. Record Voids
            model.VoidCount = await _dbContext.DeletedOrders.CountAsync(x => x.DeletedAt >= start && x.DeletedAt <= end);

            foreach (var order in orders)
            {
                var method = string.IsNullOrEmpty(order.PaymentMethod) ? "Other" : order.PaymentMethod;
                if (model.PaymentMethodBreakdown.ContainsKey(method))
                    model.PaymentMethodBreakdown[method] += order.PaidAmount;
                else
                    model.PaymentMethodBreakdown.Add(method, order.PaidAmount);
            }

            var lang = _dbContext.Languages.FirstOrDefault(x => x.Culture == GetSetting.DefaultLanguage);
            var html = GenerateReportHtml(model, lang?.Id ?? 1);

            html += "<script>window.onload = function() { window.print(); }</script>";
            return Content(html, "text/html");
        }

        private string GenerateReportHtml(WorkPeriodSummaryVM model, int langId)
        {
            var logoUrl = !string.IsNullOrEmpty(GetSetting.InvoiceLogo) ? GetSetting.InvoiceLogo : GetSetting.Logo;
            var host = HttpContext.Request.Host;
            var protocol = HttpContext.Request.Scheme;
            var finalLogoUrl = logoUrl.StartsWith("/") ? logoUrl : "/" + logoUrl;
            var logoTagHtml = GetSetting.PrintLogo ? $@"<img src=""{protocol}://{host}{finalLogoUrl}"" alt=""Logo"" class=""logo"">" : "";

            var paymentHtml = "";
            decimal currentCollected = model.PaidAmountTotal - model.OldDebtCollected;
            
            paymentHtml += $@"<tr><td>{Localize("Current Collections", langId)}:</td><td class=""right"">{GetCurrency(currentCollected)}</td></tr>";
            if (model.OldDebtCollected > 0)
            {
                paymentHtml += $@"<tr><td>{Localize("Old Debt Collected", langId)}:</td><td class=""right"">{GetCurrency(model.OldDebtCollected)}</td></tr>";
            }

            foreach (var item in model.PaymentMethodBreakdown)
            {
                paymentHtml += $@"<tr><td class=""meta-info"" style=""padding-left:10px;"">- {item.Key}:</td><td class=""right meta-info"">{GetCurrency(item.Value)}</td></tr>";
            }

            var categoryHtml = "";
            foreach (var cat in model.CategorySales.OrderByDescending(x => x.Value))
            {
                categoryHtml += $@"<tr><td>{cat.Key}:</td><td class=""right"">{GetCurrency(cat.Value)}</td></tr>";
            }

            var html = $@"<!DOCTYPE html><html><head>
<meta charset=""UTF-8"">
<meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
<title>Work Period Report</title>
<style>
@page {{ size: auto; margin: 0mm; }}
body {{ margin: 0px; padding: 0px; font-family: 'Segoe UI', system-ui, -apple-system, sans-serif; font-size: 11px; color: #000; line-height: 1.4; }}
.ticket {{ width: 100%; max-width: 80mm; margin: 0 auto; padding: 4mm; background-color: #fff; box-sizing: border-box; }}
.centered {{ text-align: center; }}
.right {{ text-align: right; }}
.logo {{ display: block; margin: 0 auto 10px auto; max-width: 120px; height: auto; }}
p {{ margin: 3px 0; }}
table {{ width: 100%; border-collapse: collapse; margin-top: 5px; }}
td, th {{ padding: 3px 0; text-align: left; vertical-align: top; font-size: 11px; }}
.divider {{ border-top: 1px dashed #444; margin: 8px 0; }}
.section-title {{ font-weight: 800; text-transform: uppercase; letter-spacing: 0.5px; margin-top: 15px; margin-bottom: 5px; font-size: 11px; border-bottom: 1px solid #eee; padding-bottom: 2px; }}
.grand-total {{ font-size: 12px; font-weight: bold; background: #f9f9f9; padding: 4px; border-top: 1px solid #000; }}
.meta-info {{ font-size: 10px; color: #555; }}
</style>
</head>
<body>
<div class=""ticket"">
    {logoTagHtml}
    <div class=""centered"">
        <p style=""font-weight:bold; font-size:16px; margin-bottom: 4px;"">{GetSetting.CompanyName}</p>
        <p class=""meta-info"">{GetSetting.CompanyAddress}</p>
        <p class=""meta-info"">{Localize("Phone", langId)}: {GetSetting.CompanyPhone}</p>
    </div>
    <div class=""divider""></div>
    <div class=""centered"">
        <p style=""font-weight:bold; font-size: 13px;"">WORK PERIOD REPORT</p>
        <p class=""meta-info"">ID: #{model.Id}</p>
    </div>
    <table>
        <tr><td>{Localize("Start", langId)}:</td><td class=""right"">{model.StartedAt:g}</td></tr>
        <tr><td>{Localize("End", langId)}:</td><td class=""right"">{model.EndAt:g}</td></tr>
        <tr><td>{Localize("Started By", langId)}:</td><td class=""right"">&#64;{model.StartedBy}</td></tr>
        <tr><td>{Localize("Ended By", langId)}:</td><td class=""right"">&#64;{model.EndBy}</td></tr>
    </table>
    
    <div class=""divider""></div>
    <table>
        <tr><td>{Localize("Opening Balance", langId)}:</td><td class=""right"" style=""font-weight:bold;"">{GetCurrency(model.OpeningBalance)}</td></tr>
        <tr><td>{Localize("Closing Balance", langId)}:</td><td class=""right"" style=""font-weight:bold;"">{GetCurrency(model.ClosingBalance)}</td></tr>
        <tr class=""meta-info""><td>{Localize("Van Sales Balance", langId)}:</td><td class=""right"">{GetCurrency(model.VanSalesTotal - model.VanDueAmount)}</td></tr>
    </table>

    <div class=""section-title"">{Localize("Audit Summary", langId)}</div>
    <table>
        <tr><td>{Localize("Void Orders", langId)}:</td><td class=""right"">{model.VoidCount}</td></tr>
        <tr><td>{Localize("Total Discount", langId)}:</td><td class=""right"">({GetCurrency(model.DiscountTotal)})</td></tr>
    </table>
    
    <div class=""section-title"">{Localize("Sales Summary", langId)}</div>
    <table>
        <tr><td>{Localize("POS Sales", langId)}:</td><td class=""right"">{GetCurrency(model.POSSalesTotal)}</td></tr>
        <tr><td>{Localize("Van Sales", langId)}:</td><td class=""right"">{GetCurrency(model.VanSalesTotal)}</td></tr>
        <tr><td>{Localize("Service Charges", langId)}:</td><td class=""right"">{GetCurrency(model.ChargesTotal)}</td></tr>
        <tr><td>{Localize("Total Tax", langId)}:</td><td class=""right"">{GetCurrency(model.TaxTotal)}</td></tr>
        <tr class=""grand-total""><td>{Localize("Total Sales", langId)}:</td><td class=""right"">{GetCurrency(model.TotalSales)}</td></tr>
    </table>

    <div class=""section-title"">{Localize("Collections", langId)}</div>
    <table>
        {paymentHtml}
        <tr style=""font-weight:bold; border-top: 1px solid #000; background: #eee;""><td>{Localize("Total Receipts", langId)}:</td><td class=""right"">{GetCurrency(model.PaidAmountTotal)}</td></tr>
        <tr class=""meta-info""><td>{Localize("Due Amount (POS)", langId)}:</td><td class=""right"">{GetCurrency(model.POSDueAmount)}</td></tr>
        <tr class=""meta-info""><td>{Localize("Due Amount (Van Sales)", langId)}:</td><td class=""right"">{GetCurrency(model.VanDueAmount)}</td></tr>
    </table>

    <div class=""section-title"">{Localize("By Category", langId)}</div>
    <table>
        {categoryHtml}
    </table>

    <div class=""section-title"">{Localize("Purchases & Expenses", langId)}</div>
    <table>
        <tr><td>{Localize("Total Purchases", langId)}:</td><td class=""right"">{GetCurrency(model.PurchasesTotal)}</td></tr>
        <tr><td>{Localize("Total Expenses", langId)}:</td><td class=""right"">{GetCurrency(model.ExpensesTotal)}</td></tr>
    </table>

    <div class=""divider""></div>
    <div class=""centered"" style=""margin-top:10px;"">
        <p style=""font-weight:bold;"">*** {Localize("End of Report", langId)} ***</p>
        <p class=""meta-info"" style=""margin-top: 5px;"">{Localize("Printed At", langId)}: {_dateTimeService.Now():g}</p>
    </div>
</div>
</body></html>";

            return html;
        }
    }
}
