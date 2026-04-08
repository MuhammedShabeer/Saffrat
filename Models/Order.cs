using System;
using System.Collections.Generic;

namespace Saffrat.Models
{
    public partial class Order
    {
        public Order()
        {
            OrderDetails = new HashSet<OrderDetail>();
        }

        public int Id { get; set; }
        public int CustomerId { get; set; }
        public string TableName { get; set; }
        public string WaiterOrDriver { get; set; }
        public int? Guests { get; set; }
        public decimal SubTotal { get; set; }
        public decimal TaxTotal { get; set; }
        public decimal DiscountTotal { get; set; }
        public decimal ChargeTotal { get; set; }
        public decimal Total { get; set; }
        public string PaymentMethod { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal DueAmount { get; set; }
        public int OrderType { get; set; }
        public int Status { get; set; }
        public string Note { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public string ClosedBy { get; set; }
        public DateTime? ClosedAt { get; set; }
        public string PriceType { get; set; }
        public Guid OrderGuid { get; set; } = Guid.NewGuid();

        public virtual Customer Customer { get; set; }
        public virtual ICollection<OrderDetail> OrderDetails { get; set; }
    }
}
