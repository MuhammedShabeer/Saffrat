using System;

namespace Saffrat.Models
{
    public partial class DeletedOrder
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int CustomerId { get; set; }
        public string TableName { get; set; }
        public string WaiterOrDriver { get; set; }
        public decimal Total { get; set; }
        public int OrderType { get; set; }
        public string Note { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime DeletedAt { get; set; }
        public string DeletedBy { get; set; }
        public string DeletionReason { get; set; }
        public string DetailsJson { get; set; }
        public string PaymentMethod { get; set; }
        public string PriceType { get; set; }

        public virtual Customer Customer { get; set; }
    }
}
