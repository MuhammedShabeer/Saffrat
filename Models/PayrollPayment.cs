using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Saffrat.Models
{
    public partial class PayrollPayment
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int PayrollId { get; set; }
        
        [Required]
        public decimal Amount { get; set; }
        
        public string PaymentMethod { get; set; } // "Cash", "Bank Transfer", etc.
        
        [Required]
        public DateTime PaymentDate { get; set; }
        
        public string Notes { get; set; }
        
        public int? JournalEntryId { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        [JsonIgnore]
        public virtual Payroll Payroll { get; set; }
        
        [JsonIgnore]
        public virtual JournalEntry JournalEntry { get; set; }
    }
}
