using System;
using System.Collections.Generic;

namespace Saffrat.ViewModels
{
    public class PurchaseComparisonVM
    {
        public List<PurchaseComparisonItem> Items { get; set; } = new List<PurchaseComparisonItem>();
        public string Search { get; set; }
    }

    public class PurchaseComparisonItem
    {
        public int IngredientId { get; set; }
        public string IngredientName { get; set; }
        public string Unit { get; set; }

        // Latest Purchase Info (Derived from LastPurchases[0])
        public DateTime? LatestDate => LastPurchases.Count > 0 ? LastPurchases[0].Date : (DateTime?)null;
        public string LatestVendor => LastPurchases.Count > 0 ? LastPurchases[0].VendorName : "N/A";
        public decimal LatestPrice => LastPurchases.Count > 0 ? LastPurchases[0].Price : 0;

        // Drill-down: Last 5 purchases
        public List<LastPurchaseInfo> LastPurchases { get; set; } = new List<LastPurchaseInfo>();
    }

    public class LastPurchaseInfo
    {
        public DateTime Date { get; set; }
        public string VendorName { get; set; }
        public decimal Qty { get; set; }
        public decimal Price { get; set; }
        public decimal Total { get; set; }
    }
}
