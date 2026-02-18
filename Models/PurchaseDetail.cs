using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Saffrat.Models
{
    public partial class PurchaseDetail
    {
        public int Id { get; set; }
        public int PurchaseId { get; set; }
        public int IngredientItemId { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal Quantity { get; set; }
        public decimal Total { get; set; }
        public DateTime? CreatedAt { get; set; }

        public virtual IngredientItem IngredientItem { get; set; }
        [JsonIgnore]
        public virtual Purchase Purchase { get; set; }
    }
}
