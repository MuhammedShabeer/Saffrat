using System.ComponentModel.DataAnnotations;

namespace Saffrat.Models
{
    public partial class WorkPeriod
    {
        [Key]
        public int? Id { get; set; }
        [Required]
        public DateTime StartedAt { get; set; }
        [Required]
        public decimal OpeningBalance { get; set; }
        public decimal? ClosingBalance { get; set; }
        public DateTime? EndAt { get; set; }
        public string StartedBy { get; set; }
        public string EndBy { get; set; }
        public bool? IsEnd { get; set; }
    }
}
