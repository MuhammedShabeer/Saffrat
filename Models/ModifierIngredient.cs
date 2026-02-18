using System;
using System.Collections.Generic;

namespace Saffrat.Models
{
    public partial class ModifierIngredient
    {
        public int Id { get; set; }
        public int ModifierId { get; set; }
        public int IngredientId { get; set; }
        public decimal Quantity { get; set; }

        public virtual IngredientItem Ingredient { get; set; }
        public virtual Modifier Modifier { get; set; }
    }
}
