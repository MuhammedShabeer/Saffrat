using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Saffrat.Models
{
    public partial class Modifier
    {
        public Modifier()
        {
            ModifierIngredients = new HashSet<ModifierIngredient>();
            OrderItemModifiers = new HashSet<OrderItemModifier>();
            RunningOrderItemModifiers = new HashSet<RunningOrderItemModifier>();
        }

        [Key]
        public int? Id { get; set; }
        [Required]
        public string Title { get; set; }
        [Required]
        public decimal Price { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }

        [JsonIgnore]
        public virtual ICollection<ModifierIngredient> ModifierIngredients { get; set; }
        [JsonIgnore]
        public virtual ICollection<OrderItemModifier> OrderItemModifiers { get; set; }
        [JsonIgnore]
        public virtual ICollection<RunningOrderItemModifier> RunningOrderItemModifiers { get; set; }
    }
}
