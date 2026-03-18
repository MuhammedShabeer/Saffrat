using System;
using System.Collections.Generic;

namespace Saffrat.ViewModels
{
    public class PurchaseComparisonVM
    {
        public DateTime StartA { get; set; }
        public DateTime EndA { get; set; }
        public DateTime StartB { get; set; }
        public DateTime EndB { get; set; }
        public List<PurchaseComparisonItem> Items { get; set; } = new List<PurchaseComparisonItem>();
    }

    public class PurchaseComparisonItem
    {
        public int IngredientId { get; set; }
        public string IngredientName { get; set; }
        public string Unit { get; set; }

        // Period A
        public decimal QtyA { get; set; }
        public decimal AvgPriceA { get; set; }
        public decimal TotalA { get; set; }

        // Period B
        public decimal QtyB { get; set; }
        public decimal AvgPriceB { get; set; }
        public decimal TotalB { get; set; }

        // Comparison
        public decimal QtyDiff => QtyA - QtyB;
        public decimal QtyDiffPercent => QtyB == 0 ? 0 : (QtyDiff / QtyB) * 100;
        
        public decimal PriceDiff => AvgPriceA - AvgPriceB;
        public decimal PriceDiffPercent => AvgPriceB == 0 ? 0 : (PriceDiff / AvgPriceB) * 100;
        
        public decimal TotalDiff => TotalA - TotalB;
        public decimal TotalDiffPercent => TotalB == 0 ? 0 : (TotalDiff / TotalB) * 100;
    }
}
