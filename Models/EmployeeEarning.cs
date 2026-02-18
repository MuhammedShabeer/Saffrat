using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Saffrat.Models
{
    public partial class EmployeeEarning
    {
        [Key]
        public int? Id { get; set; }
        [Required]
        public int EmployeeId { get; set; }
        [Required]
        public string Title { get; set; }
        [Required]
        public bool IsPercentage { get; set; }
        [Required]
        public decimal Amount { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }

        [JsonIgnore]
        public virtual Employee Employee { get; set; }
    }
}
