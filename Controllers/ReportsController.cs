using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Saffrat.Models;
using System.Data;
using Saffrat.Services;

namespace Saffrat.Controllers
{
    public class ReportsController : BaseController
    {
        private readonly ILogger<ReportsController> _logger;
        private readonly RestaurantDBContext _dbContext;

        public ReportsController(ILogger<ReportsController> logger, RestaurantDBContext dbContext,
            ILanguageService languageService, ILocalizationService localizationService)
        : base(languageService, localizationService)
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
            ViewBag.expenseTotal = _dbContext.Expenses.Where(x => x.ExpenseDate >= from && x.ExpenseDate <= to).Sum(x => x.Amount);

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

        // Expense Report
        [HttpGet]
        [Authorize(Roles = "admin")]
        public IActionResult ExpenseReport(string period, DateTime? start, DateTime? end)
        {
            IEnumerable<Expense> expenses = new List<Expense>();

            DateTime from = StartOfDay(start);
            DateTime to = EndOfDay(end);

            if (period == "yearly")
            {
                expenses = _dbContext.Expenses
                .Where(x => x.ExpenseDate >= from && x.ExpenseDate <= to)
                .OrderByDescending(a => a.Id)
                .GroupBy(p => new
                {
                    p.ExpenseDate.Year
                })
                .Select(g => new Expense
                {
                    Amount = g.Sum(x => x.Amount),
                    ExpenseDate = new DateTime(g.Key.Year, 1, 1)
                })
                .ToList();
            }
            else if (period == "monthly")
            {
                expenses = _dbContext.Expenses
                .Where(x => x.ExpenseDate >= from && x.ExpenseDate <= to)
                .OrderByDescending(a => a.Id)
                .GroupBy(p => new
                {
                    p.ExpenseDate.Month,
                    p.ExpenseDate.Year
                })
                .Select(g => new Expense
                {
                    Amount = g.Sum(x => x.Amount),
                    ExpenseDate = new DateTime(g.Key.Year, g.Key.Month, 1)
                })
                .ToList();
            }
            else
            {
                expenses = _dbContext.Expenses
                .Where(x => x.ExpenseDate >= from && x.ExpenseDate <= to)
                .OrderByDescending(a => a.Id)
                .GroupBy(p => new
                {
                    p.ExpenseDate.Day,
                    p.ExpenseDate.Month,
                    p.ExpenseDate.Year
                })
                .Select(g => new Expense
                {
                    Amount = g.Sum(x => x.Amount),
                    ExpenseDate = new DateTime(g.Key.Year, g.Key.Month, g.Key.Day)
                })
                .ToList();

                period = "daily";
            }

            ViewBag.start = from.ToString("yyyy-MM-dd");
            ViewBag.end = to.ToString("yyyy-MM-dd");
            ViewBag.period = period;

            return View(expenses);
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
        public async Task <IActionResult> AttendanceReport(int? employee, DateTime? start, DateTime? end)
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