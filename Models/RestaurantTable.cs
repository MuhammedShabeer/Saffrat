using System.ComponentModel.DataAnnotations;

namespace Saffrat.Models
{
    public partial class RestaurantTable
    {
        [Key]
        public int? Id { get; set; }
        [Required]
        public string TableName { get; set; }
        public string Image { get; set; }
        [Required]
        public bool Status { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
