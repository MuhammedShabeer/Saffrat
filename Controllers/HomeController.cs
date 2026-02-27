using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Saffrat.Models;
using Saffrat.ViewModels;
using System.Data;
using System.Diagnostics;
using System.Text.Json;
using Saffrat.Services;
using Microsoft.EntityFrameworkCore;

namespace Saffrat.Controllers
{
    public class HomeController : BaseController
    {
        private readonly ILogger<HomeController> _logger;
        private readonly RestaurantDBContext _dbContext;

        public HomeController(ILogger<HomeController> logger, RestaurantDBContext dbContext,
            ILanguageService languageService, ILocalizationService localizationService)
        : base(languageService, localizationService)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        [HttpGet]
        [Authorize(Roles = "admin,staff")]
        public IActionResult Index()
        {
            var totalSales = _dbContext.Orders.Sum(x => x.Total);
            var totalPurchases = _dbContext.Purchases.Sum(x => x.TotalAmount);
            var totalExpenses = _dbContext.LedgerEntries.Include(x => x.GLAccount)
                .Where(x => x.GLAccount.Category == 4 || x.GLAccount.Category == 5)
                .ToList()
                .Sum(x => Math.Max(0, x.Debit - x.Credit));
            var totalOrders = _dbContext.Orders.Count();

            ViewBag.TotalSales = Math.Round(totalSales, 2);
            ViewBag.TotalPurchases = Math.Round(totalPurchases, 2);
            ViewBag.TotalExpenses = Math.Round(totalExpenses, 2);
            ViewBag.TotalOrders = totalOrders;

            return View();
        }

        [HttpGet]
        public IActionResult ChangeLanguage(string culture, string returnUrl)
        {
            try
            {
                var lang = _dbContext.Languages.FirstOrDefault(x => x.Culture.Equals(culture));

                if (lang != null)
                {
                    Response.Cookies.Append(
                        CookieRequestCultureProvider.DefaultCookieName,
                        CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(GetSetting.DefaultRegion, culture)),
                        new CookieOptions
                        {
                            Expires = DateTimeOffset.UtcNow.AddDays(7)
                        }
                        );
                }
            }
            catch { }

            return LocalRedirect(returnUrl);
        }

        [HttpGet]
        [Authorize(Roles = "admin,staff")]
        public IActionResult TodayReport()
        {
            var results = new Dictionary<string, string>();
            try
            {
                var today = StartOfDay(null);
                var order = _dbContext.Orders.Where(x => x.CreatedAt >= today);
                decimal totalSales = order.Sum(x => x.Total);
                decimal customerDue = order.Sum(x => x.DueAmount);
                int todayOrders = order.Count();
                var purchase = _dbContext.Purchases.Where(x => x.PurchaseDate >= today);
                decimal totalPurchases = purchase.Sum(x => x.TotalAmount);
                decimal supplierDue = purchase.Sum(x => x.DueAmount);
                var expense = _dbContext.LedgerEntries.Include(x => x.GLAccount).Include(x => x.JournalEntry)
                    .Where(x => (x.GLAccount.Category == 4 || x.GLAccount.Category == 5) && x.JournalEntry.EntryDate >= today).ToList();
                decimal totalExpenses = expense.Sum(x => Math.Max(0, x.Debit - x.Credit));

                results.Add("totalSales", totalSales.ToString());
                results.Add("customersDue", customerDue.ToString());
                results.Add("totalOrders", todayOrders.ToString());
                results.Add("totalPurchases", totalPurchases.ToString());
                results.Add("suppliersDue", supplierDue.ToString());
                results.Add("totalExpenses", totalExpenses.ToString());
                results.Add("status", "success");
                results.Add("message", "");
            }
            catch
            {
                results.Add("status", "error");
                results.Add("message", "Something went wrong.");
            }

            return Json(results);
        }

        [HttpGet]
        [Authorize(Roles = "admin,staff")]
        public IActionResult MonthlyReport()
        {
            var results = new Dictionary<string, string>();
            try
            {
                List<MonthlyReportVM> sales = new();
                List<MonthlyReportVM> tax = new();
                List<MonthlyReportVM> discount = new();
                List<MonthlyReportVM> charges = new();
                List<MonthlyReportVM> totalOrders = new();
                List<MonthlyReportVM> purchases = new();
                List<MonthlyReportVM> expenses = new();

                var currentDate = DateTime.Now;

                var today = EndOfDay(null);
                var from = StartOfDay(null);
                from = from.AddYears(-1);
                var order = _dbContext.Orders
                    .Where(x => x.CreatedAt >= from && x.CreatedAt <= today)
                    .OrderByDescending(x => x.Id)
                    .GroupBy(t => new
                    {
                        t.CreatedAt.Month,
                        t.CreatedAt.Year
                    })
                    .Select(g => new Order
                    {
                        SubTotal = g.Count(),
                        DiscountTotal = g.Sum(x => x.DiscountTotal),
                        ChargeTotal = g.Sum(x => x.ChargeTotal),
                        TaxTotal = g.Sum(x => x.TaxTotal),
                        Total = g.Sum(x => x.Total),
                        CreatedAt = new DateTime(g.Key.Year, g.Key.Month, 1)
                    })
                    .ToList();
                var purchase = _dbContext.Purchases
                    .Where(x => x.PurchaseDate >= from && x.PurchaseDate <= today)
                    .OrderByDescending(a => a.Id)
                    .GroupBy(p => new
                    {
                        p.PurchaseDate.Month,
                        p.PurchaseDate.Year
                    })
                    .Select(g => new Purchase
                    {
                        TotalAmount = g.Sum(x => x.TotalAmount),
                        PurchaseDate = new DateTime(g.Key.Year, g.Key.Month, 1)
                    })
                    .ToList();
                var expense = _dbContext.LedgerEntries.Include(x => x.GLAccount).Include(x => x.JournalEntry)
                    .Where(x => (x.GLAccount.Category == 4 || x.GLAccount.Category == 5) && x.JournalEntry.EntryDate >= from && x.JournalEntry.EntryDate <= today)
                    .OrderByDescending(a => a.Id)
                    .ToList()
                    .GroupBy(p => new
                    {
                        p.JournalEntry.EntryDate.Month,
                        p.JournalEntry.EntryDate.Year
                    })
                    .Select(g => new
                    {
                        Amount = g.Sum(x => Math.Max(0, x.Debit - x.Credit)),
                        ExpenseDate = new DateTime(g.Key.Year, g.Key.Month, 1)
                    })
                    .ToList();

                for (int i = 0; i < 12; i++)
                {
                    var o = order.FirstOrDefault(x => x.CreatedAt.Month == currentDate.Month && x.CreatedAt.Year == currentDate.Year);
                    var p = purchase.FirstOrDefault(x => x.PurchaseDate.Month == currentDate.Month && x.PurchaseDate.Year == currentDate.Year);
                    var e = expense.FirstOrDefault(x => x.ExpenseDate.Month == currentDate.Month && x.ExpenseDate.Year == currentDate.Year);
                    if (o != null)
                    {
                        sales.Add(new MonthlyReportVM
                        {
                            Month = currentDate.Month,
                            Year = currentDate.Year,
                            Total = o.Total,
                        });
                        tax.Add(new MonthlyReportVM
                        {
                            Month = currentDate.Month,
                            Year = currentDate.Year,
                            Total = o.TaxTotal,
                        });
                        discount.Add(new MonthlyReportVM
                        {
                            Month = currentDate.Month,
                            Year = currentDate.Year,
                            Total = o.DiscountTotal,
                        });
                        charges.Add(new MonthlyReportVM
                        {
                            Month = currentDate.Month,
                            Year = currentDate.Year,
                            Total = o.ChargeTotal,
                        });
                        totalOrders.Add(new MonthlyReportVM
                        {
                            Month = currentDate.Month,
                            Year = currentDate.Year,
                            Total = o.SubTotal,
                        });
                    }
                    else
                    {
                        sales.Add(new MonthlyReportVM
                        {
                            Month = currentDate.Month,
                            Year = currentDate.Year,
                            Total = Math.Round(0m, 2),
                        });
                        tax.Add(new MonthlyReportVM
                        {
                            Month = currentDate.Month,
                            Year = currentDate.Year,
                            Total = Math.Round(0m, 2),
                        });
                        discount.Add(new MonthlyReportVM
                        {
                            Month = currentDate.Month,
                            Year = currentDate.Year,
                            Total = Math.Round(0m, 2),
                        });
                        charges.Add(new MonthlyReportVM
                        {
                            Month = currentDate.Month,
                            Year = currentDate.Year,
                            Total = Math.Round(0m, 2),
                        });
                        totalOrders.Add(new MonthlyReportVM
                        {
                            Month = currentDate.Month,
                            Year = currentDate.Year,
                            Total = 0,
                        });
                    }
                    if (p != null)
                    {
                        purchases.Add(new MonthlyReportVM
                        {
                            Month = currentDate.Month,
                            Year = currentDate.Year,
                            Total = p.TotalAmount,
                        });
                    }
                    else
                    {
                        purchases.Add(new MonthlyReportVM
                        {
                            Month = currentDate.Month,
                            Year = currentDate.Year,
                            Total = Math.Round(0m, 2),
                        });
                    }
                    if (e != null)
                    {
                        expenses.Add(new MonthlyReportVM
                        {
                            Month = currentDate.Month,
                            Year = currentDate.Year,
                            Total = e.Amount,
                        });
                    }
                    else
                    {
                        expenses.Add(new MonthlyReportVM
                        {
                            Month = currentDate.Month,
                            Year = currentDate.Year,
                            Total = Math.Round(0m, 2),
                        });
                    }
                    currentDate = currentDate.AddMonths(-1);
                }

                results.Add("sales", JsonSerializer.Serialize(sales));
                results.Add("taxes", JsonSerializer.Serialize(tax));
                results.Add("discounts", JsonSerializer.Serialize(discount));
                results.Add("charges", JsonSerializer.Serialize(charges));
                results.Add("totalOrders", JsonSerializer.Serialize(totalOrders));
                results.Add("purchases", JsonSerializer.Serialize(purchases));
                results.Add("expenses", JsonSerializer.Serialize(expenses));
                results.Add("status", "success");
                results.Add("message", "");
            }
            catch
            {
                results.Add("status", "error");
                results.Add("message", "Something went wrong.");
            }

            return Json(results);
        }

        [HttpGet]
        [Authorize(Roles = "admin,staff")]
        public IActionResult SaleByOrderType()
        {
            var results = new Dictionary<string, string>();
            var today = DateTime.Now;
            var start = new DateTime(today.Year, today.Month, 1);
            try
            {
                var order = _dbContext.Orders
                    .Where(x => x.CreatedAt >= start);

                var dineIn = order.Where(x => x.OrderType == 1).Sum(x => x.Total);
                var pickUp = order.Where(x => x.OrderType == 2).Sum(x => x.Total);
                var delivery = order.Where(x => x.OrderType == 3).Sum(x => x.Total);
                results.Add("dineIn", dineIn.ToString());
                results.Add("pickUp", pickUp.ToString());
                results.Add("delivery", delivery.ToString());

                results.Add("status", "success");
                results.Add("message", "");
            }
            catch
            {
                results.Add("status", "error");
                results.Add("message", "Something went wrong.");
            }

            return Json(results);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}