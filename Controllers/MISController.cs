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
        public async Task<IActionResult> GetMISData()
        {
            var now = CurrentDateTime();
            var startOfMonth = new DateTime(now.Year, now.Month, 1);
            var startOfDay = now.Date;

            // 1. Revenue Summary
            var todayRevenue = await _dbContext.Orders.Where(x => x.CreatedAt >= startOfDay).SumAsync(x => (decimal?)x.Total) ?? 0;
            var todayOrders = await _dbContext.Orders.CountAsync(x => x.CreatedAt >= startOfDay);
            var monthRevenue = await _dbContext.Orders.Where(x => x.CreatedAt >= startOfMonth).SumAsync(x => (decimal?)x.Total) ?? 0;
            var avgOrderValue = todayOrders > 0 ? todayRevenue / todayOrders : 0;

            // 2. Daily Sales (Last 30 Days)
            var thirtyDaysAgo = now.Date.AddDays(-29);
            var dailySalesData = await _dbContext.Orders
                .Where(x => x.CreatedAt >= thirtyDaysAgo)
                .GroupBy(x => x.CreatedAt.Date)
                .Select(g => new DailySalePoint
                {
                    Date = g.Key.ToString("MMM dd"),
                    Amount = g.Sum(x => x.Total)
                })
                .ToListAsync();

            // 3. Top Items (Top 10 by Revenue)
            var topItemsData = await _dbContext.OrderDetails
                .Include(x => x.Item)
                .Where(x => x.CreatedAt >= startOfMonth)
                .GroupBy(x => x.Item.ItemName)
                .Select(g => new ItemPerformance
                {
                    ItemName = g.Key,
                    Revenue = g.Sum(x => x.Total ?? 0),
                    Quantity = g.Sum(x => x.Quantity ?? 0)
                })
                .OrderByDescending(x => x.Revenue)
                .Take(10)
                .ToListAsync();

            // 4. Category Performance
            var categorySalesData = await _dbContext.OrderDetails
                .Include(x => x.Item).ThenInclude(x => x.Group)
                .Where(x => x.CreatedAt >= startOfMonth)
                .GroupBy(x => x.Item.Group.GroupName)
                .Select(g => new CategoryPerformance
                {
                    CategoryName = g.Key ?? "Unknown",
                    Revenue = g.Sum(x => x.Total ?? 0)
                })
                .ToListAsync();

            // 5. Order Type Distribution
            var orderTypeData = await _dbContext.Orders
                .Where(x => x.CreatedAt >= startOfMonth)
                .GroupBy(x => x.OrderType)
                .Select(g => new OrderTypeDistribution
                {
                    TypeName = g.Key == 1 ? "Dine-In" : g.Key == 2 ? "Pick-Up" : "Delivery",
                    Amount = g.Sum(x => x.Total)
                })
                .ToListAsync();

            // 6. Payment Method Distribution
            var paymentMethodData = await _dbContext.Orders
                .Where(x => x.CreatedAt >= startOfMonth)
                .GroupBy(x => x.PaymentMethod)
                .Select(g => new PaymentMethodDistribution
                {
                    MethodName = string.IsNullOrEmpty(g.Key) ? "Other" : g.Key,
                    Amount = g.Sum(x => x.Total)
                })
                .ToListAsync();

            // 7. Hourly Pulse (Last 7 Days)
            var sevenDaysAgo = now.Date.AddDays(-6);
            var hourlyData = await _dbContext.Orders
                .Where(x => x.CreatedAt >= sevenDaysAgo)
                .GroupBy(x => x.CreatedAt.Hour)
                .Select(g => new HourlyPulse
                {
                    Hour = g.Key,
                    HourLabel = g.Key > 12 ? (g.Key - 12) + " PM" : (g.Key == 0 ? "12 AM" : g.Key + " AM"),
                    Amount = g.Sum(x => x.Total)
                })
                .OrderBy(x => x.Hour)
                .ToListAsync();

            var viewModel = new MISDashboardVM
            {
                DailySales = dailySalesData,
                TopItems = topItemsData,
                CategorySales = categorySalesData,
                OrderTypeSales = orderTypeData,
                PaymentMethodSales = paymentMethodData,
                HourlySales = hourlyData,
                TodayRevenue = todayRevenue,
                TodayOrders = todayOrders,
                MonthRevenue = monthRevenue,
                AvgOrderValue = avgOrderValue
            };

            return Json(new { status = "success", data = viewModel });
        }
    }
}
