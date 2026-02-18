using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Saffrat.Models
{
    public partial class OrderItemModifier
    {
        public int Id { get; set; }
        public int OrderDetailId { get; set; }
        public int ModifierId { get; set; }
        public decimal? Price { get; set; }
        public int? Quantity { get; set; }
        public decimal? ModifierTotal { get; set; }

        public virtual Modifier Modifier { get; set; }
        [JsonIgnore]
        public virtual OrderDetail OrderDetail { get; set; }
    }
}
