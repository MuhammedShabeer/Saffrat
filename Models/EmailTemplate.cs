using System.ComponentModel.DataAnnotations;

namespace Saffrat.Models
{
    public partial class EmailTemplate
    {
        [Key]
        public int? Id { get; set; }
        public string Name { get; set; }
        [Required]
        public string Subject { get; set; }
        public string Description { get; set; }
        public string DefaultTemplate { get; set; }
        public string Template { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
