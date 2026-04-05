using System;
using System.Collections.Generic;

namespace Saffrat.Models
{
    public partial class RunningOrder
    {
        public RunningOrder()
        {
            RunningOrderDetails = new HashSet<RunningOrderDetail>();
        }

        public int Id { get; set; }
        public int CustomerId { get; set; }
        public string TableName { get; set; }
        public string WaiterOrDriver { get; set; }
        public int? Guests { get; set; }
        public int TaxId { get; set; }
        public int DiscountId { get; set; }
        public int ChargesId { get; set; }
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
        public string PriceType { get; set; }

        public virtual Charge Charges { get; set; }
        public virtual Customer Customer { get; set; }
        public virtual Discount Discount { get; set; }
        public virtual TaxRate Tax { get; set; }
        public virtual ICollection<RunningOrderDetail> RunningOrderDetails { get; set; }
    }
}
