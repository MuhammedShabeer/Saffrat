using System.ComponentModel.DataAnnotations;

namespace Saffrat.Models
{
    public partial class PaymentMethod
    {
        [Key]
        public int? Id { get; set; }
        [Required]
        public string Title { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
