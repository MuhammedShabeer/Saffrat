using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Saffrat.Models
{
    public partial class Payroll
    {
        public Payroll()
        {
            PayrollDetails = new HashSet<PayrollDetail>();
        }

        [Key]
        public int Id { get; set; }
        [Required]
        public int EmployeeId { get; set; }
        [Required]
        public string PayrollType { get; set; }
        [Required]
        public decimal Salary { get; set; }
        public decimal NetSalary { get; set; }
        [Required]
        public int Month { get; set; }
        [Required]
        public int Year { get; set; }
        [Required]
        public string PaymentStatus { get; set; }
        public DateTime? GeneratedAt { get; set; }
        public string GeneratedBy { get; set; }

        public virtual Employee Employee { get; set; }
        public virtual ICollection<PayrollDetail> PayrollDetails { get; set; }
    }
}
