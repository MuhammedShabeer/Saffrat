using System.ComponentModel.DataAnnotations;

namespace Saffrat.Models
{
    public partial class Holiday
    {
        [Key]
        public int? Id { get; set; }
        [Required]
        public DateTime FromDate { get; set; }
        [Required]
        public DateTime ToDate { get; set; }
        public string Note { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
