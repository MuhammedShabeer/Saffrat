using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Saffrat.Models
{
    public partial class RunningOrderDetail
    {
        public RunningOrderDetail()
        {
            RunningOrderItemModifiers = new HashSet<RunningOrderItemModifier>();
        }

        public int Id { get; set; }
        public int? OrderId { get; set; }
        public int? ItemId { get; set; }
        public decimal? Price { get; set; }
        public decimal? ModifierTotal { get; set; }
        public int? Quantity { get; set; }
        public decimal FocQuantity { get; set; }
        public decimal? Total { get; set; }
        public DateTime CreatedAt { get; set; }

        public virtual FoodItem Item { get; set; }
        [JsonIgnore]
        public virtual RunningOrder Order { get; set; }
        public virtual ICollection<RunningOrderItemModifier> RunningOrderItemModifiers { get; set; }
    }
}
