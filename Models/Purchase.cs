using System.ComponentModel.DataAnnotations;

namespace Saffrat.Models
{
    public partial class Purchase
    {
        public Purchase()
        {
            PurchaseDetails = new HashSet<PurchaseDetail>();
        }

        [Key]
        public int? Id { get; set; }
        [Required]
        public int SupplierId { get; set; }
        [Required]
        public string InvoiceNo { get; set; }
        [Required]
        public DateTime PurchaseDate { get; set; }
        public string Description { get; set; }
        [Required]
        public string PaymentType { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal DueAmount { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public virtual Supplier Supplier { get; set; }
        public virtual ICollection<PurchaseDetail> PurchaseDetails { get; set; }
    }
}
