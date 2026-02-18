using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Saffrat.Models
{
    public partial class Designation
    {
        public Designation()
        {
            Employees = new HashSet<Employee>();
        }

        [Key]
        public int? Id { get; set; }
        [Required]
        public string Title { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }

        [JsonIgnore]
        public virtual ICollection<Employee> Employees { get; set; }
    }
}
