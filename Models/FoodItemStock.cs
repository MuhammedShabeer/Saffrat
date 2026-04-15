using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Saffrat.Models
{
    public partial class FoodItemStock
    {
        public int Id { get; set; }
        public int FoodItemId { get; set; }
        public string UserId { get; set; }
        public decimal Quantity { get; set; }
        public DateTime UpdatedAt { get; set; }

        public virtual FoodItem FoodItem { get; set; }
    }
}
