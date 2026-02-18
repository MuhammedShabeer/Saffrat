using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Saffrat.Models
{
    public partial class FoodItemIngredient
    {
        public int Id { get; set; }
        public int FoodItemId { get; set; }
        public int IngredientId { get; set; }
        public decimal Quantity { get; set; }

        [JsonIgnore]
        public virtual FoodItem FoodItem { get; set; }
        [JsonIgnore]
        public virtual IngredientItem Ingredient { get; set; }
    }
}
