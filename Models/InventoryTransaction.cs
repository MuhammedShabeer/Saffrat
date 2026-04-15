using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Saffrat.Models
{
    public partial class InventoryTransaction
    {
        public int Id { get; set; }
        public int FoodItemId { get; set; }
        public string UserId { get; set; }
        public decimal QuantityChange { get; set; }
        public string Type { get; set; } // 'Load', 'Sale', 'Return', 'Adjustment'
        public DateTime EntryDate { get; set; }
        public int? ReferenceId { get; set; }
        public string CreatedBy { get; set; }

        public virtual FoodItem FoodItem { get; set; }
    }
}
