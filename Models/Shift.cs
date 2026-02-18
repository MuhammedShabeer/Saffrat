using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Saffrat.Models
{
    public partial class Shift
    {
        public Shift()
        {
            Attendances = new HashSet<Attendance>();
            Employees = new HashSet<Employee>();
        }

        [Key]
        public int? Id { get; set; }
        [Required]
        public string Title { get; set; }
        [Required]
        public TimeSpan StartAt { get; set; }
        [Required]
        public TimeSpan EndAt { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }

        [JsonIgnore]
        public virtual ICollection<Attendance> Attendances { get; set; }
        [JsonIgnore]
        public virtual ICollection<Employee> Employees { get; set; }
    }
}
