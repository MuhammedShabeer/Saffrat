using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Saffrat.Models
{
    public partial class FoodGroup
    {
        public FoodGroup()
        {
            FoodItems = new HashSet<FoodItem>();
        }

        [Key]
        public int? Id { get; set; }
        [Required]
        public string GroupName { get; set; }
        public string Image { get; set; }
        [Required]
        public bool Status { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }

        [JsonIgnore]
        public virtual ICollection<FoodItem> FoodItems { get; set; }
    }
}
