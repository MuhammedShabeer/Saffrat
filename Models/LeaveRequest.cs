using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Saffrat.Models
{
    public partial class LeaveRequest
    {
        [Key]
        public int? Id { get; set; }
        [Required]
        public int EmployeeId { get; set; }
        [Required]
        public string LeaveType { get; set; }
        [Required]
        public DateTime StartDate { get; set; }
        [Required]
        public DateTime EndDate { get; set; }
        public int? Days { get; set; }
        [Required]
        public string Status { get; set; }
        public string Description { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }

        [JsonIgnore]
        public virtual Employee Employee { get; set; }
    }
}
