using System.ComponentModel.DataAnnotations;

namespace Saffrat.Models
{
    public partial class AccountMoneyTransfer
    {
        [Key]
        public int? Id { get; set; }
        [Required]
        public string FromAccount { get; set; }
        [Required]
        public string ToAccount { get; set; }
        [Required]
        public DateTime TransferDate { get; set; }
        [Required]
        public decimal Amount { get; set; }
        public string Note { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
