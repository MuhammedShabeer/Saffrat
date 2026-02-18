using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Saffrat.Models
{
    public partial class FoodItem
    {
        public FoodItem()
        {
            FoodItemIngredients = new HashSet<FoodItemIngredient>();
            OrderDetails = new HashSet<OrderDetail>();
            RunningOrderDetails = new HashSet<RunningOrderDetail>();
        }

        [Key]
        public int? Id { get; set; }
        [Required]
        public int GroupId { get; set; }
        [Required]
        public string ItemName { get; set; }
        public string Description { get; set; }
        [Required]
        public decimal Price { get; set; }
        public string Image { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }

        [JsonIgnore]
        public virtual FoodGroup Group { get; set; }
        [JsonIgnore]
        public virtual ICollection<FoodItemIngredient> FoodItemIngredients { get; set; }
        [JsonIgnore]
        public virtual ICollection<OrderDetail> OrderDetails { get; set; }
        [JsonIgnore]
        public virtual ICollection<RunningOrderDetail> RunningOrderDetails { get; set; }
    }
}
