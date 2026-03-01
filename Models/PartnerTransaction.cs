using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Saffrat.Models
{
    public class PartnerTransaction
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PartnerId { get; set; }
        public virtual Partner Partner { get; set; }

        [Required]
        public DateTime EntryDate { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(50)]
        public string Type { get; set; } // "Investment", "Withdrawal", "ProfitDistribution"

        [StringLength(500)]
        public string Note { get; set; }

        public int? JournalEntryId { get; set; }
        public virtual JournalEntry JournalEntry { get; set; }

        [StringLength(150)]
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
