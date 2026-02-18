using System.Text.Json.Serialization;

namespace Saffrat.Models
{
    public partial class PayrollDetail
    {
        public int Id { get; set; }
        public int PayrollId { get; set; }
        public string Title { get; set; }
        public bool IsPercentage { get; set; }
        public string AmountType { get; set; }
        public decimal Amount { get; set; }

        [JsonIgnore]
        public virtual Payroll Payroll { get; set; }
    }
}
