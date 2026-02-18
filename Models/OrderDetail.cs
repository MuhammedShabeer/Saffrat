using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Saffrat.Models
{
    public partial class OrderDetail
    {
        public OrderDetail()
        {
            OrderItemModifiers = new HashSet<OrderItemModifier>();
        }

        public int Id { get; set; }
        public int? OrderId { get; set; }
        public int? ItemId { get; set; }
        public decimal? Price { get; set; }
        public decimal? ModifierTotal { get; set; }
        public int? Quantity { get; set; }
        public decimal? Total { get; set; }
        public DateTime CreatedAt { get; set; }

        public virtual FoodItem Item { get; set; }
        [JsonIgnore]
        public virtual Order Order { get; set; }
        public virtual ICollection<OrderItemModifier> OrderItemModifiers { get; set; }
    }
}
