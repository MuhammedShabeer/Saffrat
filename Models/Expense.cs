using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Saffrat.Models
{
    public partial class Expense
    {
        [Key]
        public int? Id { get; set; }
        [Required]
        public int AccountId { get; set; }
        [Required]
        public DateTime ExpenseDate { get; set; }
        [Required]
        public decimal Amount { get; set; }
        public string Note { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }

        [JsonIgnore]
        public virtual Account Account { get; set; }
    }
}
