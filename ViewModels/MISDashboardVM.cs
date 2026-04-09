using System;
using System.Collections.Generic;

namespace Saffrat.ViewModels
{
    public class MISDashboardVM
    {
        public List<DailySalePoint> DailySales { get; set; }
        public List<MonthlySalePoint> MonthlySales { get; set; }
        public List<ItemPerformance> TopItems { get; set; }
        public List<CategoryPerformance> CategorySales { get; set; }
        public List<OrderTypeDistribution> OrderTypeSales { get; set; }
        public List<PaymentMethodDistribution> PaymentMethodSales { get; set; }
        public List<HourlyPulse> HourlySales { get; set; }
        
        public decimal TodayRevenue { get; set; }
        public int TodayOrders { get; set; }
        public decimal MonthRevenue { get; set; }
        public decimal AvgOrderValue { get; set; }
    }

    public class DailySalePoint
    {
        public string Date { get; set; }
        public decimal Amount { get; set; }
    }

    public class MonthlySalePoint
    {
        public string Month { get; set; }
        public decimal Amount { get; set; }
    }

    public class ItemPerformance
    {
        public string ItemName { get; set; }
        public decimal Revenue { get; set; }
        public int Quantity { get; set; }
    }

    public class CategoryPerformance
    {
        public string CategoryName { get; set; }
        public decimal Revenue { get; set; }
    }

    public class OrderTypeDistribution
    {
        public string TypeName { get; set; }
        public decimal Amount { get; set; }
    }

    public class PaymentMethodDistribution
    {
        public string MethodName { get; set; }
        public decimal Amount { get; set; }
    }

    public class HourlyPulse
    {
        public int Hour { get; set; }
        public string HourLabel { get; set; }
        public decimal Amount { get; set; }
    }
}
