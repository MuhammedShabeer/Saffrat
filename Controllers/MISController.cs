using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Saffrat.Models;
using Saffrat.Services;
using Saffrat.ViewModels;
using System.Globalization;

namespace Saffrat.Controllers
{
    [Authorize(Roles = "admin")]
    public class MISController : BaseController
    {
        private readonly RestaurantDBContext _dbContext;

        public MISController(RestaurantDBContext dbContext, ILanguageService languageService, ILocalizationService localizationService, IDateTimeService dateTimeService)
            : base(languageService, localizationService, dateTimeService)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public IActionResult Dashboard()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetMISData(string fromDate, string toDate)
        {
            DateTime now = CurrentDateTime();
            DateTime startRange;
            DateTime endRange;

            if (!string.IsNullOrEmpty(fromDate) && DateTime.TryParse(fromDate, out DateTime parsedFrom))
            {
                startRange = parsedFrom.Date;
            }
            else
            {
                startRange = now.Date; 
            }

            if (!string.IsNullOrEmpty(toDate) && DateTime.TryParse(toDate, out DateTime parsedTo))
            {
                endRange = parsedTo.Date.AddDays(1).AddTicks(-1);
            }
            else
            {
                endRange = now.Date.AddDays(1).AddTicks(-1); 
            }

            // 1. Revenue Summary (Restaurant vs VanSale) for SELECTED RANGE
            var baseOrders = _dbContext.Orders.Where(x => x.CreatedAt >= startRange && x.CreatedAt <= endRange);

            var todayRevenue = await baseOrders.Where(x => x.PriceType != "VanSale").SumAsync(x => (decimal?)x.Total) ?? 0;
            var todayDue = await baseOrders.Where(x => x.PriceType != "VanSale").SumAsync(x => (decimal?)x.DueAmount) ?? 0;
            var todayOrders = await baseOrders.CountAsync(x => x.PriceType != "VanSale");
            var avgOrderValue = todayOrders > 0 ? todayRevenue / todayOrders : 0;

            var vtodayRevenue = await baseOrders.Where(x => x.PriceType == "VanSale").SumAsync(x => (decimal?)x.Total) ?? 0;
            var vtodayDue = await baseOrders.Where(x => x.PriceType == "VanSale").SumAsync(x => (decimal?)x.DueAmount) ?? 0;
            var vtodayOrders = await baseOrders.CountAsync(x => x.PriceType == "VanSale");
            var vavgOrderValue = vtodayOrders > 0 ? vtodayRevenue / vtodayOrders : 0;
            
            var totalDiscount = await baseOrders.SumAsync(x => (decimal?)x.DiscountTotal) ?? 0;

            var startOfMonth = new DateTime(now.Year, now.Month, 1);
            var monthRevenue = await _dbContext.Orders.Where(x => x.CreatedAt >= startOfMonth && x.PriceType != "VanSale").SumAsync(x => (decimal?)x.Total) ?? 0;
            var vmonthRevenue = await _dbContext.Orders.Where(x => x.CreatedAt >= startOfMonth && x.PriceType == "VanSale").SumAsync(x => (decimal?)x.Total) ?? 0;

            // 2. Daily Sales (Trend for selected range)
            var dailyPosData = await baseOrders
                .Where(x => x.PriceType != "VanSale")
                .GroupBy(x => x.CreatedAt.Date)
                .Select(g => new DailySalePoint { Date = g.Key.ToString("MMM dd"), Amount = g.Sum(x => x.Total) })
                .ToListAsync();

            var dailyVanData = await baseOrders
                .Where(x => x.PriceType == "VanSale")
                .GroupBy(x => x.CreatedAt.Date)
                .Select(g => new DailySalePoint { Date = g.Key.ToString("MMM dd"), Amount = g.Sum(x => x.Total) })
                .ToListAsync();

            // 3. Top Items (Separated POS vs Van)
            var baseOrderDetails = _dbContext.OrderDetails.Include(x => x.Item).Where(x => x.CreatedAt >= startRange && x.CreatedAt <= endRange);

            var topPosItems = await baseOrderDetails
                .Where(x => x.Order.PriceType != "VanSale")
                .GroupBy(x => x.Item.ItemName)
                .Select(g => new ItemPerformance { ItemName = g.Key, Revenue = g.Sum(x => x.Total ?? 0), Quantity = g.Sum(x => x.Quantity ?? 0) })
                .OrderByDescending(x => x.Revenue).Take(10).ToListAsync();

            var topVanItems = await baseOrderDetails
                .Where(x => x.Order.PriceType == "VanSale")
                .GroupBy(x => x.Item.ItemName)
                .Select(g => new ItemPerformance { ItemName = g.Key, Revenue = g.Sum(x => x.Total ?? 0), Quantity = g.Sum(x => x.Quantity ?? 0) })
                .OrderByDescending(x => x.Revenue).Take(10).ToListAsync();

            // 4. Category Performance
            var categorySalesData = await baseOrderDetails
                .Include(x => x.Item).ThenInclude(x => x.Group)
                .GroupBy(x => x.Item.Group.GroupName)
                .Select(g => new CategoryPerformance { CategoryName = g.Key ?? "Unknown", Revenue = g.Sum(x => x.Total ?? 0) })
                .ToListAsync();

            // 5. Order Type & Payment Distribution
            var orderTypeData = await baseOrders
                .GroupBy(x => x.OrderType)
                .Select(g => new OrderTypeDistribution { TypeName = g.Key == 1 ? "Dine-In" : g.Key == 2 ? "Pick-Up" : g.Key == 3 ? "Delivery" : "Van Sale", Amount = g.Sum(x => x.Total) })
                .ToListAsync();

            var paymentMethodData = await baseOrders
                .GroupBy(x => x.PaymentMethod)
                .Select(g => new PaymentMethodDistribution { MethodName = string.IsNullOrEmpty(g.Key) ? "Other" : g.Key, Amount = g.Sum(x => x.Total) })
                .ToListAsync();

            // 6. Hourly Pulse
            var hourlyData = await baseOrders
                .GroupBy(x => x.CreatedAt.Hour)
                .Select(g => new HourlyPulse { 
                    Hour = g.Key, 
                    HourLabel = g.Key >= 12 ? (g.Key == 12 ? "12 PM" : (g.Key - 12) + " PM") : (g.Key == 0 ? "12 AM" : g.Key + " AM"),
                    Amount = g.Sum(x => x.Total) 
                })
                .OrderBy(x => x.Hour).ToListAsync();

            // 7. Collections (Inflow/Debt)
            var oldDebtCollected = await _dbContext.JournalEntries
                .Include(j => j.LedgerEntries)
                .Where(j => j.EntryDate >= startRange && j.EntryDate <= endRange && j.SourceDocumentType == "CustomerCollection")
                .SelectMany(j => j.LedgerEntries)
                .Where(le => _dbContext.GLAccounts.Any(ac => ac.Id == le.GLAccountId && ac.IsCash))
                .SumAsync(le => (decimal?)(le.Debit - le.Credit)) ?? 0;

            var viewModel = new MISDashboardVM
            {
                DailySales = dailyPosData,
                DailyVanSales = dailyVanData,
                TopPosItems = topPosItems,
                TopVanSaleItems = topVanItems,
                CategorySales = categorySalesData,
                OrderTypeSales = orderTypeData,
                PaymentMethodSales = paymentMethodData,
                HourlySales = hourlyData,
                
                TodayRevenue = todayRevenue,
                TodayOrders = todayOrders,
                TodayDue = todayDue,
                MonthRevenue = monthRevenue,
                AvgOrderValue = avgOrderValue,
                
                TotalDiscount = totalDiscount,
                OldDebtCollected = oldDebtCollected,
                TotalCollected = (await baseOrders.SumAsync(x => (decimal?)x.PaidAmount) ?? 0) + oldDebtCollected,

                VanSaleTodayRevenue = vtodayRevenue,
                VanSaleTodayOrders = vtodayOrders,
                VanSaleTodayDue = vtodayDue,
                VanSaleMonthRevenue = vmonthRevenue,
                VanSaleAvgOrderValue = vavgOrderValue
            };

            return Json(new { status = "success", data = viewModel });
        }
    }
}
