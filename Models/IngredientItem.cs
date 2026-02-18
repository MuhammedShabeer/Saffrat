using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Saffrat.Models
{
    public partial class IngredientItem
    {
        public IngredientItem()
        {
            FoodItemIngredients = new HashSet<FoodItemIngredient>();
            ModifierIngredients = new HashSet<ModifierIngredient>();
            PurchaseDetails = new HashSet<PurchaseDetail>();
        }

        [Key]
        public int? Id { get; set; }
        [Required]
        public string ItemName { get; set; }
        public string Description { get; set; }
        [Required]
        public string Unit { get; set; }
        [Required]
        public decimal Price { get; set; }
        [Required]
        public decimal Quantity { get; set; }
        [Required]
        public decimal AlertQuantity { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }

        [JsonIgnore]
        public virtual ICollection<FoodItemIngredient> FoodItemIngredients { get; set; }
        [JsonIgnore]
        public virtual ICollection<ModifierIngredient> ModifierIngredients { get; set; }
        [JsonIgnore]
        public virtual ICollection<PurchaseDetail> PurchaseDetails { get; set; }
    }
}
