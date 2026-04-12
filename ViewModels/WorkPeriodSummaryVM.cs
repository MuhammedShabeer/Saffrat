using System;
using System.Collections.Generic;

namespace Saffrat.ViewModels
{
    public class WorkPeriodSummaryVM
    {
        public int Id { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? EndAt { get; set; }
        public string StartedBy { get; set; }
        public string EndBy { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal ClosingBalance { get; set; }

        public decimal POSSalesTotal { get; set; }
        public decimal VanSalesTotal { get; set; }
        public decimal TotalSales { get; set; }
        
        public decimal DueAmountTotal { get; set; }
        public decimal POSDueAmount { get; set; }
        public decimal VanDueAmount { get; set; }
        public decimal PaidAmountTotal { get; set; }
        
        public decimal PurchasesTotal { get; set; }
        public decimal ExpensesTotal { get; set; }
        public decimal ChargesTotal { get; set; }
        
        public decimal TaxTotal { get; set; }
        public decimal DiscountTotal { get; set; }

        public Dictionary<string, decimal> PaymentMethodBreakdown { get; set; } = new Dictionary<string, decimal>();

        // New Detailed Breakdowns
        public Dictionary<string, decimal> CategorySales { get; set; } = new Dictionary<string, decimal>();
        public decimal OldDebtCollected { get; set; }
        public int VoidCount { get; set; }
        public decimal VoidAmount { get; set; }
    }
}
