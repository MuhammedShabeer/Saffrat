using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Saffrat.Models
{
    public class StockAdjustment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public DateTime EntryDate { get; set; }

        [Required]
        public int IngredientItemId { get; set; }
        public virtual IngredientItem IngredientItem { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Quantity { get; set; }

        [Required]
        [StringLength(50)]
        public string Type { get; set; } // "Addition", "Subtraction", "Wastage"

        [StringLength(500)]
        public string Reason { get; set; }

        public int? JournalEntryId { get; set; }
        public virtual JournalEntry JournalEntry { get; set; }

        [StringLength(150)]
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
